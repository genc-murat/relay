using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

namespace Relay.MessageBroker.Batch;

/// <summary>
/// Implements batch processing for messages with automatic flushing and compression.
/// </summary>
/// <typeparam name="TMessage">The type of messages to batch.</typeparam>
public sealed class BatchProcessor<TMessage> : IBatchProcessor<TMessage>
{
    private readonly IMessageBroker _broker;
    private readonly BatchOptions _options;
    private readonly ILogger<BatchProcessor<TMessage>> _logger;
    private readonly List<BatchItem<TMessage>> _batch;
    private readonly SemaphoreSlim _lock;
    private readonly Timer _flushTimer;
    private readonly ConcurrentQueue<double> _processingTimes;
    private readonly ConcurrentQueue<int> _batchSizes;

    private Task? _currentFlushTask;
    private long _totalBatchesProcessed;
    private long _totalMessagesProcessed;
    private long _totalFailedMessages;
    private long _totalOriginalBytes;
    private long _totalCompressedBytes;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessor{TMessage}"/> class.
    /// </summary>
    /// <param name="broker">The message broker to publish batches to.</param>
    /// <param name="options">The batch processing options.</param>
    /// <param name="logger">The logger.</param>
    public BatchProcessor(
        IMessageBroker broker,
        IOptions<BatchOptions> options,
        ILogger<BatchProcessor<TMessage>> logger)
    {
        _broker = broker ?? throw new ArgumentNullException(nameof(broker));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _batch = new List<BatchItem<TMessage>>(_options.MaxBatchSize);
        _lock = new SemaphoreSlim(1, 1);
        _processingTimes = new ConcurrentQueue<double>();
        _batchSizes = new ConcurrentQueue<int>();

        // Start the flush timer
        _flushTimer = new Timer(
            _ => {
                // Fire and forget the async operation to avoid blocking the timer thread
                // but track it so we can await it during disposal
                _currentFlushTask = Task.Run(async () =>
                {
                    try
                    {
                        await FlushTimerCallbackAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during timer-based flush");
                    }
                });
            },
            null,
            _options.FlushInterval,
            _options.FlushInterval);

        _logger.LogInformation(
            "BatchProcessor initialized with MaxBatchSize={MaxBatchSize}, FlushInterval={FlushInterval}ms",
            _options.MaxBatchSize,
            _options.FlushInterval.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public async ValueTask AddAsync(
        TMessage message,
        PublishOptions? options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            _batch.Add(new BatchItem<TMessage>
            {
                Message = message,
                Options = options
            });

            _logger.LogTrace(
                "Message added to batch. Current size: {CurrentSize}/{MaxSize}",
                _batch.Count,
                _options.MaxBatchSize);

            // Flush if batch size is reached
            if (_batch.Count >= _options.MaxBatchSize)
            {
                _logger.LogDebug("Batch size limit reached, flushing batch");
                await FlushInternalAsync(cancellationToken);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            await FlushInternalAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc/>
    public BatchProcessorMetrics GetMetrics()
    {
        var processingTimesArray = _processingTimes.ToArray();
        var batchSizesArray = _batchSizes.ToArray();

        var totalMessages = Interlocked.Read(ref _totalMessagesProcessed);
        var failedMessages = Interlocked.Read(ref _totalFailedMessages);
        var successfulMessages = totalMessages - failedMessages;

        return new BatchProcessorMetrics
        {
            CurrentBatchSize = _batch.Count,
            AverageBatchSize = batchSizesArray.Length > 0 ? batchSizesArray.Average() : 0,
            TotalBatchesProcessed = Interlocked.Read(ref _totalBatchesProcessed),
            TotalMessagesProcessed = totalMessages,
            AverageProcessingTimeMs = processingTimesArray.Length > 0 ? processingTimesArray.Average() : 0,
            SuccessRate = totalMessages > 0 ? (double)successfulMessages / totalMessages : 1.0,
            TotalFailedMessages = failedMessages,
            CompressionRatio = CalculateCompressionRatio(),
            LastFlushAt = _totalBatchesProcessed > 0 ? DateTimeOffset.UtcNow : null
        };
    }

    /// <summary>
    /// Internal flush implementation (must be called with lock held).
    /// </summary>
    private async ValueTask FlushInternalAsync(CancellationToken cancellationToken)
    {
        if (_batch.Count == 0)
        {
            return;
        }

        var batchToProcess = _batch.ToList();
        _batch.Clear();

        _logger.LogDebug("Flushing batch with {Count} messages", batchToProcess.Count);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await PublishBatchAsync(batchToProcess, cancellationToken);

            stopwatch.Stop();
            RecordMetrics(batchToProcess.Count, stopwatch.Elapsed.TotalMilliseconds, 0);

            _logger.LogInformation(
                "Successfully flushed batch with {Count} messages in {Duration}ms",
                batchToProcess.Count,
                stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to flush batch with {Count} messages", batchToProcess.Count);

            if (_options.PartialRetry)
            {
                await RetryFailedMessagesAsync(batchToProcess, cancellationToken);
            }
            else
            {
                RecordMetrics(batchToProcess.Count, stopwatch.Elapsed.TotalMilliseconds, batchToProcess.Count);
                throw;
            }
        }
    }

    /// <summary>
    /// Publishes a batch of messages.
    /// </summary>
    private async ValueTask PublishBatchAsync(
        List<BatchItem<TMessage>> batch,
        CancellationToken cancellationToken)
    {
        if (_options.EnableCompression)
        {
            await PublishCompressedBatchAsync(batch, cancellationToken);
        }
        else
        {
            await PublishUncompressedBatchAsync(batch, cancellationToken);
        }
    }

    /// <summary>
    /// Publishes a batch with compression.
    /// </summary>
    private async ValueTask PublishCompressedBatchAsync(
        List<BatchItem<TMessage>> batch,
        CancellationToken cancellationToken)
    {
        // Serialize all messages
        var serializedMessages = batch.Select(item => new
        {
            Message = JsonSerializer.SerializeToUtf8Bytes(item.Message),
            item.Options
        }).ToList();

        // Calculate original size
        var originalSize = serializedMessages.Sum(m => m.Message.Length);

        // Create a batch payload
        var batchPayload = new
        {
            Messages = serializedMessages.Select(m => new
            {
                Data = Convert.ToBase64String(m.Message),
                m.Options
            }).ToList(),
            BatchSize = batch.Count,
            Timestamp = DateTimeOffset.UtcNow
        };

        var batchJson = JsonSerializer.SerializeToUtf8Bytes(batchPayload);

        // Compress the batch
        var compressedData = await CompressAsync(batchJson, cancellationToken);

        // Track compression metrics
        Interlocked.Add(ref _totalOriginalBytes, originalSize);
        Interlocked.Add(ref _totalCompressedBytes, compressedData.Length);

        var compressionRatio = (double)originalSize / compressedData.Length;
        _logger.LogDebug(
            "Compressed batch from {OriginalSize} to {CompressedSize} bytes (ratio: {Ratio:F2}x)",
            originalSize,
            compressedData.Length,
            compressionRatio);

        // Publish the compressed batch
        var batchOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["X-Batch-Size"] = batch.Count,
                ["X-Batch-Compressed"] = true,
                ["X-Batch-Original-Size"] = originalSize
            }
        };

        await _broker.PublishAsync(compressedData, batchOptions, cancellationToken);
    }

    /// <summary>
    /// Publishes a batch without compression.
    /// </summary>
    private async ValueTask PublishUncompressedBatchAsync(
        List<BatchItem<TMessage>> batch,
        CancellationToken cancellationToken)
    {
        // Publish each message individually
        foreach (var item in batch)
        {
            await _broker.PublishAsync(item.Message, item.Options, cancellationToken);
        }
    }

    /// <summary>
    /// Retries failed messages individually.
    /// </summary>
    private async ValueTask RetryFailedMessagesAsync(
        List<BatchItem<TMessage>> batch,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrying {Count} messages individually", batch.Count);

        var failedCount = 0;
        foreach (var item in batch)
        {
            try
            {
                await _broker.PublishAsync(item.Message, item.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                failedCount++;
                _logger.LogError(ex, "Failed to publish message during retry");
            }
        }

        RecordMetrics(batch.Count, 0, failedCount);

        _logger.LogInformation(
            "Retry completed: {Successful}/{Total} messages published successfully",
            batch.Count - failedCount,
            batch.Count);
    }

    /// <summary>
    /// Compresses data using GZip.
    /// </summary>
    private async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken)
    {
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            await gzipStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    /// <summary>
    /// Records batch processing metrics.
    /// </summary>
    private void RecordMetrics(int batchSize, double processingTimeMs, int failedCount)
    {
        Interlocked.Increment(ref _totalBatchesProcessed);
        Interlocked.Add(ref _totalMessagesProcessed, batchSize);
        Interlocked.Add(ref _totalFailedMessages, failedCount);

        _processingTimes.Enqueue(processingTimeMs);
        _batchSizes.Enqueue(batchSize);

        // Keep only last 100 measurements
        while (_processingTimes.Count > 100)
        {
            _processingTimes.TryDequeue(out _);
        }
        while (_batchSizes.Count > 100)
        {
            _batchSizes.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Calculates the compression ratio.
    /// </summary>
    private double CalculateCompressionRatio()
    {
        var originalBytes = Interlocked.Read(ref _totalOriginalBytes);
        var compressedBytes = Interlocked.Read(ref _totalCompressedBytes);

        if (compressedBytes == 0)
        {
            return 1.0;
        }

        return (double)originalBytes / compressedBytes;
    }

    /// <summary>
    /// Timer callback for periodic flushing.
    /// </summary>
    private async Task FlushTimerCallbackAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during timer-based flush");
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        // Stop the timer
        await _flushTimer.DisposeAsync();

        // Wait for any in-flight timer flush to complete
        var flushTask = _currentFlushTask;
        if (flushTask != null)
        {
            try
            {
                await flushTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error waiting for in-flight timer flush during disposal");
            }
        }

        // Flush any remaining messages before marking as disposed
        try
        {
            await FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flushing remaining messages during disposal");
        }

        _disposed = true;

        _lock.Dispose();

        _logger.LogInformation("BatchProcessor disposed");
    }
}
