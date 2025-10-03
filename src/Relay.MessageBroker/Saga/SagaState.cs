namespace Relay.MessageBroker.Saga;

/// <summary>
/// Represents the state of a saga instance.
/// </summary>
public enum SagaState
{
    /// <summary>
    /// Saga has not started yet.
    /// </summary>
    NotStarted,

    /// <summary>
    /// Saga is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Saga is compensating (rolling back).
    /// </summary>
    Compensating,

    /// <summary>
    /// Saga completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Saga was compensated (rolled back).
    /// </summary>
    Compensated,

    /// <summary>
    /// Saga failed and could not be compensated.
    /// </summary>
    Failed,

    /// <summary>
    /// Saga was aborted.
    /// </summary>
    Aborted
}
