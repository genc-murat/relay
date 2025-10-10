using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Collects and aggregates saga execution metrics for monitoring and analysis.
/// </summary>
public interface ISagaMetricsCollector
{
    /// <summary>
    /// Records the start of a saga execution.
    /// </summary>
    void RecordSagaStarted(string sagaType, Guid sagaId, string correlationId);

    /// <summary>
    /// Records successful saga completion.
    /// </summary>
    void RecordSagaCompleted(string sagaType, Guid sagaId, TimeSpan duration);

    /// <summary>
    /// Records saga failure.
    /// </summary>
    void RecordSagaFailed(string sagaType, Guid sagaId, string failedStep, TimeSpan duration);

    /// <summary>
    /// Records saga compensation.
    /// </summary>
    void RecordSagaCompensated(string sagaType, Guid sagaId, int stepsCompensated, TimeSpan duration);

    /// <summary>
    /// Records saga timeout.
    /// </summary>
    void RecordSagaTimedOut(string sagaType, Guid sagaId, SagaState state);

    /// <summary>
    /// Records a saga step execution.
    /// </summary>
    void RecordStepExecuted(string sagaType, string stepName, TimeSpan duration, bool success);

    /// <summary>
    /// Gets aggregated metrics for a specific saga type.
    /// </summary>
    SagaMetrics GetMetrics(string sagaType);

    /// <summary>
    /// Gets aggregated metrics for all saga types.
    /// </summary>
    Dictionary<string, SagaMetrics> GetAllMetrics();

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    void Reset();
}

/// <summary>
/// In-memory implementation of saga metrics collector.
/// </summary>
public class InMemorySagaMetricsCollector : ISagaMetricsCollector
{
    private readonly ConcurrentDictionary<string, SagaTypeMetrics> _metricsPerType = new();
    private readonly ConcurrentDictionary<Guid, SagaExecutionTracking> _activeSagas = new();
    private readonly ILogger<InMemorySagaMetricsCollector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySagaMetricsCollector"/> class.
    /// </summary>
    public InMemorySagaMetricsCollector(ILogger<InMemorySagaMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void RecordSagaStarted(string sagaType, Guid sagaId, string correlationId)
    {
        var tracking = new SagaExecutionTracking
        {
            SagaType = sagaType,
            SagaId = sagaId,
            CorrelationId = correlationId,
            StartTime = DateTimeOffset.UtcNow,
            Stopwatch = Stopwatch.StartNew()
        };

        _activeSagas[sagaId] = tracking;

        var metrics = GetOrCreateMetrics(sagaType);
        metrics.IncrementStarted();

        _logger.LogDebug(
            "Saga started: {SagaType} (Id: {SagaId}, CorrelationId: {CorrelationId})",
            sagaType,
            sagaId,
            correlationId);
    }

    /// <inheritdoc/>
    public void RecordSagaCompleted(string sagaType, Guid sagaId, TimeSpan duration)
    {
        var metrics = GetOrCreateMetrics(sagaType);
        metrics.IncrementCompleted(duration);

        _activeSagas.TryRemove(sagaId, out _);

        _logger.LogInformation(
            "Saga completed: {SagaType} (Id: {SagaId}, Duration: {Duration}ms)",
            sagaType,
            sagaId,
            duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void RecordSagaFailed(string sagaType, Guid sagaId, string failedStep, TimeSpan duration)
    {
        var metrics = GetOrCreateMetrics(sagaType);
        metrics.IncrementFailed(failedStep, duration);

        _activeSagas.TryRemove(sagaId, out _);

        _logger.LogWarning(
            "Saga failed: {SagaType} (Id: {SagaId}, FailedStep: {FailedStep}, Duration: {Duration}ms)",
            sagaType,
            sagaId,
            failedStep,
            duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void RecordSagaCompensated(string sagaType, Guid sagaId, int stepsCompensated, TimeSpan duration)
    {
        var metrics = GetOrCreateMetrics(sagaType);
        metrics.IncrementCompensated(stepsCompensated, duration);

        _activeSagas.TryRemove(sagaId, out _);

        _logger.LogInformation(
            "Saga compensated: {SagaType} (Id: {SagaId}, StepsCompensated: {StepsCompensated}, Duration: {Duration}ms)",
            sagaType,
            sagaId,
            stepsCompensated,
            duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void RecordSagaTimedOut(string sagaType, Guid sagaId, SagaState state)
    {
        var metrics = GetOrCreateMetrics(sagaType);
        metrics.IncrementTimedOut();

        if (_activeSagas.TryRemove(sagaId, out var tracking))
        {
            tracking.Stopwatch?.Stop();
        }

        _logger.LogWarning(
            "Saga timed out: {SagaType} (Id: {SagaId}, State: {State})",
            sagaType,
            sagaId,
            state);
    }

    /// <inheritdoc/>
    public void RecordStepExecuted(string sagaType, string stepName, TimeSpan duration, bool success)
    {
        var metrics = GetOrCreateMetrics(sagaType);
        metrics.RecordStepExecution(stepName, duration, success);

        var logLevel = success ? LogLevel.Debug : LogLevel.Warning;
        _logger.Log(logLevel,
            "Saga step executed: {SagaType}.{StepName} (Duration: {Duration}ms, Success: {Success})",
            sagaType,
            stepName,
            duration.TotalMilliseconds,
            success);
    }

    /// <inheritdoc/>
    public SagaMetrics GetMetrics(string sagaType)
    {
        if (_metricsPerType.TryGetValue(sagaType, out var metricsData))
        {
            return metricsData.ToSagaMetrics();
        }

        return new SagaMetrics { SagaType = sagaType };
    }

    /// <inheritdoc/>
    public Dictionary<string, SagaMetrics> GetAllMetrics()
    {
        return _metricsPerType.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToSagaMetrics());
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _metricsPerType.Clear();
        _activeSagas.Clear();
        _logger.LogInformation("Saga metrics reset");
    }

    private SagaTypeMetrics GetOrCreateMetrics(string sagaType)
    {
        return _metricsPerType.GetOrAdd(sagaType, _ => new SagaTypeMetrics(sagaType));
    }

    /// <summary>
    /// Internal tracking for saga execution.
    /// </summary>
    private class SagaExecutionTracking
    {
        public string SagaType { get; init; } = string.Empty;
        public Guid SagaId { get; init; }
        public string CorrelationId { get; init; } = string.Empty;
        public DateTimeOffset StartTime { get; init; }
        public Stopwatch? Stopwatch { get; init; }
    }

    /// <summary>
    /// Thread-safe metrics aggregation for a saga type.
    /// </summary>
    private class SagaTypeMetrics
    {
        private readonly string _sagaType;
        private long _started;
        private long _completed;
        private long _failed;
        private long _compensated;
        private long _timedOut;
        private long _totalDurationMs;
        private long _totalCompensationDurationMs;
        private long _totalStepsCompensated;
        private readonly ConcurrentDictionary<string, long> _failuresByStep = new();
        private readonly ConcurrentDictionary<string, StepMetrics> _stepMetrics = new();
        private readonly ConcurrentBag<double> _durations = new();

        public SagaTypeMetrics(string sagaType)
        {
            _sagaType = sagaType;
        }

        public void IncrementStarted()
        {
            Interlocked.Increment(ref _started);
        }

        public void IncrementCompleted(TimeSpan duration)
        {
            Interlocked.Increment(ref _completed);
            Interlocked.Add(ref _totalDurationMs, (long)duration.TotalMilliseconds);
            _durations.Add(duration.TotalMilliseconds);
        }

        public void IncrementFailed(string failedStep, TimeSpan duration)
        {
            Interlocked.Increment(ref _failed);
            Interlocked.Add(ref _totalDurationMs, (long)duration.TotalMilliseconds);
            _failuresByStep.AddOrUpdate(failedStep, 1, (_, count) => count + 1);
            _durations.Add(duration.TotalMilliseconds);
        }

        public void IncrementCompensated(int stepsCompensated, TimeSpan duration)
        {
            Interlocked.Increment(ref _compensated);
            Interlocked.Add(ref _totalCompensationDurationMs, (long)duration.TotalMilliseconds);
            Interlocked.Add(ref _totalStepsCompensated, stepsCompensated);
        }

        public void IncrementTimedOut()
        {
            Interlocked.Increment(ref _timedOut);
        }

        public void RecordStepExecution(string stepName, TimeSpan duration, bool success)
        {
            var metrics = _stepMetrics.GetOrAdd(stepName, _ => new StepMetrics());
            metrics.RecordExecution(duration, success);
        }

        public SagaMetrics ToSagaMetrics()
        {
            var started = Interlocked.Read(ref _started);
            var completed = Interlocked.Read(ref _completed);
            var failed = Interlocked.Read(ref _failed);
            var compensated = Interlocked.Read(ref _compensated);
            var timedOut = Interlocked.Read(ref _timedOut);
            var totalDurationMs = Interlocked.Read(ref _totalDurationMs);
            var totalCompensationDurationMs = Interlocked.Read(ref _totalCompensationDurationMs);
            var totalStepsCompensated = Interlocked.Read(ref _totalStepsCompensated);

            var totalFinished = completed + failed + compensated;
            var avgDurationMs = totalFinished > 0 ? totalDurationMs / (double)totalFinished : 0;
            var avgCompensationDurationMs = compensated > 0 ? totalCompensationDurationMs / (double)compensated : 0;
            var avgStepsCompensated = compensated > 0 ? totalStepsCompensated / (double)compensated : 0;
            var successRate = totalFinished > 0 ? (completed / (double)totalFinished) * 100 : 0;

            // Calculate percentiles
            var sortedDurations = _durations.OrderBy(d => d).ToArray();
            var p50 = GetPercentile(sortedDurations, 50);
            var p95 = GetPercentile(sortedDurations, 95);
            var p99 = GetPercentile(sortedDurations, 99);

            return new SagaMetrics
            {
                SagaType = _sagaType,
                TotalStarted = started,
                TotalCompleted = completed,
                TotalFailed = failed,
                TotalCompensated = compensated,
                TotalTimedOut = timedOut,
                AverageDurationMs = avgDurationMs,
                P50DurationMs = p50,
                P95DurationMs = p95,
                P99DurationMs = p99,
                AverageCompensationDurationMs = avgCompensationDurationMs,
                AverageStepsCompensated = avgStepsCompensated,
                SuccessRate = successRate,
                FailuresByStep = new Dictionary<string, long>(_failuresByStep),
                StepMetrics = _stepMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToStepMetricsData())
            };
        }

        private static double GetPercentile(double[] sortedValues, int percentile)
        {
            if (sortedValues.Length == 0)
                return 0;

            var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Length) - 1;
            return sortedValues[Math.Max(0, Math.Min(index, sortedValues.Length - 1))];
        }

        private class StepMetrics
        {
            private long _executions;
            private long _successes;
            private long _failures;
            private long _totalDurationMs;

            public void RecordExecution(TimeSpan duration, bool success)
            {
                Interlocked.Increment(ref _executions);
                if (success)
                    Interlocked.Increment(ref _successes);
                else
                    Interlocked.Increment(ref _failures);

                Interlocked.Add(ref _totalDurationMs, (long)duration.TotalMilliseconds);
            }

            public StepMetricsData ToStepMetricsData()
            {
                var executions = Interlocked.Read(ref _executions);
                var successes = Interlocked.Read(ref _successes);
                var failures = Interlocked.Read(ref _failures);
                var totalDurationMs = Interlocked.Read(ref _totalDurationMs);

                return new StepMetricsData
                {
                    TotalExecutions = executions,
                    Successes = successes,
                    Failures = failures,
                    AverageDurationMs = executions > 0 ? totalDurationMs / (double)executions : 0,
                    SuccessRate = executions > 0 ? (successes / (double)executions) * 100 : 0
                };
            }
        }
    }
}

/// <summary>
/// Aggregated metrics for a saga type.
/// </summary>
public readonly record struct SagaMetrics
{
    public string SagaType { get; init; }
    public long TotalStarted { get; init; }
    public long TotalCompleted { get; init; }
    public long TotalFailed { get; init; }
    public long TotalCompensated { get; init; }
    public long TotalTimedOut { get; init; }
    public double AverageDurationMs { get; init; }
    public double P50DurationMs { get; init; }
    public double P95DurationMs { get; init; }
    public double P99DurationMs { get; init; }
    public double AverageCompensationDurationMs { get; init; }
    public double AverageStepsCompensated { get; init; }
    public double SuccessRate { get; init; }
    public Dictionary<string, long> FailuresByStep { get; init; }
    public Dictionary<string, StepMetricsData> StepMetrics { get; init; }

    public override string ToString()
    {
        return $"{SagaType}: Started={TotalStarted}, Completed={TotalCompleted}, " +
               $"Failed={TotalFailed}, Compensated={TotalCompensated}, TimedOut={TotalTimedOut}, " +
               $"SuccessRate={SuccessRate:F1}%, AvgDuration={AverageDurationMs:F0}ms";
    }
}

/// <summary>
/// Metrics for a specific saga step.
/// </summary>
public readonly record struct StepMetricsData
{
    public long TotalExecutions { get; init; }
    public long Successes { get; init; }
    public long Failures { get; init; }
    public double AverageDurationMs { get; init; }
    public double SuccessRate { get; init; }

    public override string ToString()
    {
        return $"Executions={TotalExecutions}, Successes={Successes}, Failures={Failures}, " +
               $"SuccessRate={SuccessRate:F1}%, AvgDuration={AverageDurationMs:F0}ms";
    }
}
