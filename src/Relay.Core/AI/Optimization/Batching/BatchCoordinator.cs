using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Optimization.Batching
{
    /// <summary>
    /// Coordinates batch processing of requests.
    /// </summary>
    internal sealed class BatchCoordinator<TRequest, TResponse> : IBatchCoordinator, IDisposable
        where TRequest : IRequest<TResponse>
    {
        private readonly int _batchSize;
        private readonly TimeSpan _batchWindow;
        private readonly TimeSpan _maxWaitTime;
        private readonly BatchingStrategy _strategy;
        private readonly ILogger _logger;
        private readonly System.Collections.Concurrent.ConcurrentQueue<BatchItem<TRequest, TResponse>> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);
        private readonly System.Threading.Timer _timer;
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _disposeCts = new();
        private int _queuedCount = 0;
        private DateTime _batchStartTime = DateTime.UtcNow;
        private bool _disposed = false;

        public BatchCoordinatorMetadata? Metadata { get; set; }

        public BatchCoordinator(
            int batchSize,
            TimeSpan batchWindow,
            TimeSpan maxWaitTime,
            BatchingStrategy strategy,
            ILogger logger)
        {
            _batchSize = batchSize;
            _batchWindow = batchWindow;
            _maxWaitTime = maxWaitTime;
            _strategy = strategy;
            _logger = logger;

            // Start batch processing timer
            _timer = new System.Threading.Timer(
                _ => TriggerBatchProcessing(),
                null,
                batchWindow,
                batchWindow);

            // Start background processing task
            _processingTask = Task.Run(ProcessingLoopAsync);
        }

        public BatchCoordinatorMetadata? GetMetadata() => Metadata;

        public async ValueTask<BatchExecutionResult<TResponse>> EnqueueAndWaitAsync(
            BatchItem<TRequest, TResponse> item,
            CancellationToken cancellationToken)
        {
            _queue.Enqueue(item);
            var count = System.Threading.Interlocked.Increment(ref _queuedCount);

            // Trigger batch if size reached
            if (count >= _batchSize)
            {
                _signal.Release();
            }

            // Wait for batch execution with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_maxWaitTime);

            try
            {
                return await item.CompletionSource.Task.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Timeout or cancellation - execute individually as fallback
                var response = await item.Handler();
                return new BatchExecutionResult<TResponse>
                {
                    Response = response,
                    BatchSize = 1,
                    WaitTime = DateTime.UtcNow - item.EnqueueTime,
                    ExecutionTime = TimeSpan.Zero,
                    Success = true,
                    Strategy = _strategy,
                    Efficiency = 0.0
                };
            }
        }

        private void TriggerBatchProcessing()
        {
            if (_queuedCount > 0)
            {
                _signal.Release();
            }
        }

        private async Task ProcessingLoopAsync()
        {
            try
            {
                while (!_disposeCts.Token.IsCancellationRequested)
                {
                    // Wait for signal or cancellation
                    await _signal.WaitAsync(_disposeCts.Token);
                    
                    // Process the batch
                    await ProcessBatchAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch processing loop failed");
            }
        }

        private async Task ProcessBatchAsync()
        {
            var items = new List<BatchItem<TRequest, TResponse>>();
            var batchStartTime = DateTime.UtcNow;

            // Dequeue items up to batch size
            var itemsToDequeue = Math.Min(_batchSize, _queuedCount);
            for (int i = 0; i < itemsToDequeue; i++)
            {
                if (_queue.TryDequeue(out var item))
                {
                    items.Add(item);
                    System.Threading.Interlocked.Decrement(ref _queuedCount);
                }
            }

            if (items.Count == 0)
                return;

            _logger.LogDebug("Processing batch of {Count} items", items.Count);

            // Process all items in parallel
            var executionStartTime = DateTime.UtcNow;
            var tasks = items.Select(async item =>
            {
                try
                {
                    var response = await item.Handler();
                    var waitTime = executionStartTime - item.EnqueueTime;
                    var executionTime = DateTime.UtcNow - executionStartTime;

                    var result = new BatchExecutionResult<TResponse>
                    {
                        Response = response,
                        BatchSize = items.Count,
                        WaitTime = waitTime,
                        ExecutionTime = executionTime,
                        Success = true,
                        Strategy = _strategy,
                        Efficiency = CalculateEfficiency(items.Count, waitTime, executionTime)
                    };

                    item.CompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Batch item execution failed");
                    item.CompletionSource.SetException(ex);
                }
            });

            await Task.WhenAll(tasks);
        }

        private double CalculateEfficiency(int batchSize, TimeSpan waitTime, TimeSpan executionTime)
        {
            // Efficiency = (batch size benefit) / (wait time cost)
            // Higher batch size increases efficiency, longer wait time decreases it
            var sizeFactor = Math.Log(batchSize + 1) / Math.Log(_batchSize + 1);
            var waitPenalty = Math.Min(1.0, waitTime.TotalMilliseconds / _maxWaitTime.TotalMilliseconds);
            return Math.Clamp(sizeFactor * (1.0 - waitPenalty * 0.5), 0.0, 1.0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposeCts?.Cancel();
                _timer?.Dispose();
                
                try
                {
                    _processingTask?.Wait(TimeSpan.FromSeconds(1));
                }
                catch (AggregateException)
                {
                    // Ignore exceptions during disposal
                }
                
                _signal?.Dispose();
                _disposeCts?.Dispose();
                _disposed = true;
            }
        }
    }
}
