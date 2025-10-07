using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents a database transaction that can be committed or rolled back.
    /// This is typically a wrapper around a provider-specific transaction object (e.g., IDbContextTransaction).
    /// </summary>
    public interface IDbTransaction : IAsyncDisposable
    {
        /// <summary>
        /// Commits all changes made to the database in this transaction.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back all changes made to the database in this transaction.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
