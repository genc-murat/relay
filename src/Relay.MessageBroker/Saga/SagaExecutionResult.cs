namespace Relay.MessageBroker.Saga;

/// <summary>
/// Result of saga execution.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class SagaExecutionResult<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Gets or sets the saga data after execution.
    /// </summary>
    public TSagaData Data { get; init; } = default!;

    /// <summary>
    /// Gets or sets whether the saga executed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets or sets the name of the step that failed (if any).
    /// </summary>
    public string? FailedStep { get; init; }

    /// <summary>
    /// Gets or sets the exception that occurred (if any).
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Gets or sets whether compensation was successful (if it was needed).
    /// </summary>
    public bool CompensationSucceeded { get; init; }
}
