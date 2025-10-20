namespace Relay.MessageBroker.Saga;

/// <summary>
/// Extended interface for saga data that tracks compensation progress.
/// Implement this interface on your saga data to enable detailed compensation tracking.
/// </summary>
public interface ISagaDataWithCompensationTracking : ISagaData
{
    /// <summary>
    /// Records a successfully compensated step.
    /// </summary>
    void RecordCompensatedStep(string stepName, DateTimeOffset timestamp);

    /// <summary>
    /// Records a failed compensation attempt.
    /// </summary>
    void RecordFailedCompensation(string stepName, Exception exception, int retryCount, DateTimeOffset timestamp);

    /// <summary>
    /// Records overall compensation summary.
    /// </summary>
    void RecordCompensationSummary(int totalSteps, int successfulCount, int failedCount, int totalAttempts, TimeSpan duration);
}
