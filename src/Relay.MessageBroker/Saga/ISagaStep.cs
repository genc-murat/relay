namespace Relay.MessageBroker.Saga;

/// <summary>
/// Represents a step in a saga orchestration.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public interface ISagaStep<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Gets the name of the step.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the step.
    /// </summary>
    /// <param name="data">The saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask ExecuteAsync(TSagaData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compensates (rolls back) the step.
    /// </summary>
    /// <param name="data">The saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask CompensateAsync(TSagaData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Base class for saga steps.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public abstract class SagaStep<TSagaData> : ISagaStep<TSagaData> where TSagaData : ISagaData
{
    /// <inheritdoc/>
    public virtual string Name => GetType().Name;

    /// <inheritdoc/>
    public abstract ValueTask ExecuteAsync(TSagaData data, CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract ValueTask CompensateAsync(TSagaData data, CancellationToken cancellationToken = default);
}
