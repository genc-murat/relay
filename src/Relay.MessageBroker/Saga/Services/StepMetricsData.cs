namespace Relay.MessageBroker.Saga.Services;

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
