namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// In-memory database context implementation for testing.
/// </summary>
public sealed class InMemorySagaDbContext : ISagaDbContext
{
    private readonly List<SagaEntityBase> _sagas = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public IQueryable<SagaEntityBase> Sagas
    {
        get
        {
            lock (_lock)
            {
                return _sagas.AsQueryable();
            }
        }
    }

    /// <inheritdoc/>
    public void Add(SagaEntityBase entity)
    {
        lock (_lock)
        {
            _sagas.Add(entity);
        }
    }

    /// <inheritdoc/>
    public void Update(SagaEntityBase entity)
    {
        lock (_lock)
        {
            var existing = _sagas.FirstOrDefault(s => s.SagaId == entity.SagaId);
            if (existing != null)
            {
                _sagas.Remove(existing);
                _sagas.Add(entity);
            }
        }
    }

    /// <inheritdoc/>
    public void Remove(SagaEntityBase entity)
    {
        lock (_lock)
        {
            _sagas.Remove(entity);
        }
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(1);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to dispose
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Clears all saga data (for testing).
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _sagas.Clear();
        }
    }
}
