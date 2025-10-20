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
