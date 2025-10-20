namespace Relay.MessageBroker.Saga.Interfaces;

/// <summary>
/// Interface for saga orchestration.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public interface ISaga<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Gets the unique identifier for the saga definition.
    /// </summary>
    string SagaId { get; }

    /// <summary>
    /// Gets the steps in the saga.
    /// </summary>
    IReadOnlyList<ISagaStep<TSagaData>> Steps { get; }

    /// <summary>
    /// Executes the saga.
    /// </summary>
    /// <param name="data">The saga data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask<SagaExecutionResult<TSagaData>> ExecuteAsync(TSagaData data, CancellationToken cancellationToken = default);
}
