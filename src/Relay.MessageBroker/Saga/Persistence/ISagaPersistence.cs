using System.Runtime.CompilerServices;
using Relay.MessageBroker.Saga.Interfaces;

namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Interface for saga persistence.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public interface ISagaPersistence<TSagaData> where TSagaData : ISagaData
{
    /// <summary>
    /// Saves or updates saga data.
    /// </summary>
    /// <param name="data">The saga data to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask SaveAsync(TSagaData data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga data by saga ID.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga data, or null if not found.</returns>
    ValueTask<TSagaData?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets saga data by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saga data, or null if not found.</returns>
    ValueTask<TSagaData?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes saga data.
    /// </summary>
    /// <param name="sagaId">The saga ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask DeleteAsync(Guid sagaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active sagas (not completed, compensated, or failed).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of active saga data.</returns>
    IAsyncEnumerable<TSagaData> GetActiveSagasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sagas by state.
    /// </summary>
    /// <param name="state">The saga state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of saga data in the specified state.</returns>
    IAsyncEnumerable<TSagaData> GetByStateAsync(SagaState state, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory saga persistence implementation (for development/testing).
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class InMemorySagaPersistence<TSagaData> : ISagaPersistence<TSagaData> where TSagaData : ISagaData
{
    private readonly Dictionary<Guid, TSagaData> _sagasById = new();
    private readonly Dictionary<string, TSagaData> _sagasByCorrelationId = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public ValueTask SaveAsync(TSagaData data, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _sagasById[data.SagaId] = data;
            
            if (!string.IsNullOrEmpty(data.CorrelationId))
            {
                _sagasByCorrelationId[data.CorrelationId] = data;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask<TSagaData?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _sagasById.TryGetValue(sagaId, out var data) 
                ? ValueTask.FromResult<TSagaData?>(data)
                : ValueTask.FromResult<TSagaData?>(default);
        }
    }

    /// <inheritdoc/>
    public ValueTask<TSagaData?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return _sagasByCorrelationId.TryGetValue(correlationId, out var data)
                ? ValueTask.FromResult<TSagaData?>(data)
                : ValueTask.FromResult<TSagaData?>(default);
        }
    }

    /// <inheritdoc/>
    public ValueTask DeleteAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_sagasById.TryGetValue(sagaId, out var data))
            {
                _sagasById.Remove(sagaId);
                
                if (!string.IsNullOrEmpty(data.CorrelationId))
                {
                    _sagasByCorrelationId.Remove(data.CorrelationId);
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TSagaData> GetActiveSagasAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        TSagaData[] sagas;
        
        lock (_lock)
        {
            sagas = _sagasById.Values
                .Where(s => s.State == SagaState.Running || s.State == SagaState.Compensating)
                .ToArray();
        }

        foreach (var saga in sagas)
        {
            yield return saga;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TSagaData> GetByStateAsync(SagaState state, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        TSagaData[] sagas;
        
        lock (_lock)
        {
            sagas = _sagasById.Values.Where(s => s.State == state).ToArray();
        }

        foreach (var saga in sagas)
        {
            yield return saga;
        }

        await Task.CompletedTask;
    }
}
