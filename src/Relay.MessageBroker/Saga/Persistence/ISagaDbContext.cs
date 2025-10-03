namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Interface for saga database context.
/// </summary>
public interface ISagaDbContext : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the sagas DbSet.
    /// </summary>
    IQueryable<SagaEntityBase> Sagas { get; }

    /// <summary>
    /// Adds a saga entity.
    /// </summary>
    void Add(SagaEntityBase entity);

    /// <summary>
    /// Updates a saga entity.
    /// </summary>
    void Update(SagaEntityBase entity);

    /// <summary>
    /// Removes a saga entity.
    /// </summary>
    void Remove(SagaEntityBase entity);

    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
