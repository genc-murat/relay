namespace Relay.MessageBroker.Saga.Services;

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
