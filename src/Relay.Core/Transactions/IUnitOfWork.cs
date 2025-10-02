using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents a unit of work pattern for managing transactional operations.
    /// Typically implemented by DbContext or other data access abstractions.
    /// </summary>
    /// <remarks>
    /// This interface is designed to work with Entity Framework Core's DbContext
    /// and other data access patterns that support transactional operations.
    ///
    /// Example implementation with EF Core:
    /// <code>
    /// public class ApplicationDbContext : DbContext, IUnitOfWork
    /// {
    ///     public Task&lt;int&gt; SaveChangesAsync(CancellationToken cancellationToken = default)
    ///     {
    ///         return base.SaveChangesAsync(cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
