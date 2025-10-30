using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Relay.MessageBroker.Backpressure;

/// <summary>
/// Implementation of backpressure controller that monitors processing metrics and applies throttling when needed.
/// </summary>
public sealed class BackpressureController : IBackpressureController
{
    private readonly BackpressureOptions _options;
    private readonly ILogger<BackpressureController> _logger;
    private readonly ConcurrentQueue<ProcessingRecord> _processingRecords;
    private readonly object _stateLock = new();

    private bool _isThrottling;
    private long _totalProcessingRecords;
    private long _backpressureActivations;
    private DateTimeOffset? _lastBackpressureActivation;
    private DateTimeOffset? _lastBackpressureDeactivation;
    private int _currentQueueDepth;

    /// <summary>
    /// Event raised when backpressure state changes.
    /// </summary>
    public event EventHandler<BackpressureEvent>? BackpressureStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackpressureController"/> class.
    /// </summary>
    /// <param name="options">The backpressure options.</param>
    /// <param name="logger">The logger.</param>
    public BackpressureController(
        IOptions<BackpressureOptions> options,
        ILogger<BackpressureController> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options.Validate();

        _processingRecords = new ConcurrentQueue<ProcessingRecord>();

        _logger.LogInformation(
            "BackpressureController initialized. LatencyThreshold: {LatencyThreshold}ms, QueueDepthThreshold: {QueueDepthThreshold}, RecoveryLatencyThreshold: {RecoveryLatencyThreshold}ms",
            _options.LatencyThreshold.TotalMilliseconds,
            _options.QueueDepthThreshold,
            _options.RecoveryLatencyThreshold.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public ValueTask<bool> ShouldThrottleAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ValueTask.FromResult(false);
        }

        var metrics = CalculateMetrics();

        // Check if we should activate backpressure
        if (!_isThrottling)
        {
            if (metrics.AverageLatency > _options.LatencyThreshold)
            {
                ActivateBackpressure(metrics, "Average latency exceeded threshold");
                return ValueTask.FromResult(true);
            }

            if (metrics.QueueDepth > _options.QueueDepthThreshold)
            {
                ActivateBackpressure(metrics, "Queue depth exceeded threshold");
                return ValueTask.FromResult(true);
            }
        }
        // Check if we should deactivate backpressure
        else
        {
            if (metrics.AverageLatency < _options.RecoveryLatencyThreshold &&
                metrics.QueueDepth < _options.QueueDepthThreshold)
            {
                DeactivateBackpressure(metrics, "Conditions improved");
                return ValueTask.FromResult(false);
            }
        }

        return ValueTask.FromResult(_isThrottling);
    }

    /// <inheritdoc/>
    public ValueTask RecordProcessingAsync(TimeSpan duration, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return ValueTask.CompletedTask;
        }

        var record = new ProcessingRecord
        {
            Duration = duration,
            Timestamp = DateTimeOffset.UtcNow
        };

        _processingRecords.Enqueue(record);
        Interlocked.Increment(ref _totalProcessingRecords);

        // Keep only the last N records based on sliding window size
        while (_processingRecords.Count > _options.SlidingWindowSize)
        {
            _processingRecords.TryDequeue(out _);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public BackpressureMetrics GetMetrics()
    {
        return CalculateMetrics();
    }

    /// <summary>
    /// Updates the current queue depth for monitoring.
    /// </summary>
    /// <param name="queueDepth">The current queue depth.</param>
    public void UpdateQueueDepth(int queueDepth)
    {
        Interlocked.Exchange(ref _currentQueueDepth, queueDepth);
    }

    /// <summary>
    /// Calculates current backpressure metrics.
    /// </summary>
    /// <returns>The calculated metrics.</returns>
    private BackpressureMetrics CalculateMetrics()
    {
        var records = _processingRecords.ToArray();
        
        var avgLatency = TimeSpan.Zero;
        var minLatency = TimeSpan.MaxValue;
        var maxLatency = TimeSpan.Zero;

        if (records.Length > 0)
        {
            var totalTicks = 0L;
            foreach (var record in records)
            {
                totalTicks += record.Duration.Ticks;
                if (record.Duration < minLatency)
                {
                    minLatency = record.Duration;
                }
                if (record.Duration > maxLatency)
                {
                    maxLatency = record.Duration;
                }
            }
            avgLatency = TimeSpan.FromTicks(totalTicks / records.Length);
        }
        else
        {
            minLatency = TimeSpan.Zero;
        }

        return new BackpressureMetrics
        {
            AverageLatency = avgLatency,
            QueueDepth = Interlocked.CompareExchange(ref _currentQueueDepth, 0, 0),
            IsThrottling = _isThrottling,
            TotalProcessingRecords = Interlocked.Read(ref _totalProcessingRecords),
            BackpressureActivations = Interlocked.Read(ref _backpressureActivations),
            LastBackpressureActivation = _lastBackpressureActivation,
            LastBackpressureDeactivation = _lastBackpressureDeactivation,
            MinLatency = minLatency,
            MaxLatency = maxLatency
        };
    }

    /// <summary>
    /// Activates backpressure throttling.
    /// </summary>
    /// <param name="metrics">The current metrics.</param>
    /// <param name="reason">The reason for activation.</param>
    private void ActivateBackpressure(BackpressureMetrics metrics, string reason)
    {
        lock (_stateLock)
        {
            if (_isThrottling)
            {
                return;
            }

            _isThrottling = true;
            _lastBackpressureActivation = DateTimeOffset.UtcNow;
            Interlocked.Increment(ref _backpressureActivations);

            _logger.LogWarning(
                "Backpressure ACTIVATED. Reason: {Reason}, AvgLatency: {AvgLatency}ms, QueueDepth: {QueueDepth}, ThrottleFactor: {ThrottleFactor}",
                reason,
                metrics.AverageLatency.TotalMilliseconds,
                metrics.QueueDepth,
                _options.ThrottleFactor);

            RaiseBackpressureEvent(new BackpressureEvent
            {
                EventType = BackpressureEventType.Activated,
                Timestamp = DateTimeOffset.UtcNow,
                AverageLatency = metrics.AverageLatency,
                QueueDepth = metrics.QueueDepth,
                Reason = reason
            });
        }
    }

    /// <summary>
    /// Deactivates backpressure throttling.
    /// </summary>
    /// <param name="metrics">The current metrics.</param>
    /// <param name="reason">The reason for deactivation.</param>
    private void DeactivateBackpressure(BackpressureMetrics metrics, string reason)
    {
        lock (_stateLock)
        {
            if (!_isThrottling)
            {
                return;
            }

            _isThrottling = false;
            _lastBackpressureDeactivation = DateTimeOffset.UtcNow;

            _logger.LogInformation(
                "Backpressure DEACTIVATED. Reason: {Reason}, AvgLatency: {AvgLatency}ms, QueueDepth: {QueueDepth}",
                reason,
                metrics.AverageLatency.TotalMilliseconds,
                metrics.QueueDepth);

            RaiseBackpressureEvent(new BackpressureEvent
            {
                EventType = BackpressureEventType.Deactivated,
                Timestamp = DateTimeOffset.UtcNow,
                AverageLatency = metrics.AverageLatency,
                QueueDepth = metrics.QueueDepth,
                Reason = reason
            });
        }
    }

    /// <summary>
    /// Raises a backpressure state change event.
    /// </summary>
    /// <param name="backpressureEvent">The event to raise.</param>
    private void RaiseBackpressureEvent(BackpressureEvent backpressureEvent)
    {
        try
        {
            BackpressureStateChanged?.Invoke(this, backpressureEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising backpressure event");
        }
    }

    /// <summary>
    /// Internal class to track processing records.
    /// </summary>
    private sealed class ProcessingRecord
    {
        public TimeSpan Duration { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
