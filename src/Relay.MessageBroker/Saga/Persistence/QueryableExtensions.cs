namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Extension methods for IQueryable to support async operations.
/// </summary>
internal static class QueryableExtensions
{
    public static async Task<T?> FirstOrDefaultAsync<T>(this IQueryable<T> queryable, System.Linq.Expressions.Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        // For in-memory collections
        return queryable.Where(predicate).FirstOrDefault();
    }

    public static async Task<List<T>> ToListAsync<T>(this IQueryable<T> queryable, CancellationToken cancellationToken = default)
    {
        // For in-memory collections
        return queryable.ToList();
    }
}
