using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents an async-capable database transaction with enhanced transaction management capabilities.
    /// </summary>
    /// <remarks>
    /// This interface extends the standard System.Data.IDbTransaction with async methods
    /// to support modern async/await patterns in transaction management.
    /// 
    /// Implementations should wrap the underlying database transaction (e.g., DbTransaction from EF Core)
    /// and provide async commit/rollback operations.
    /// </remarks>
    public interface IRelayDbTransaction : IAsyncDisposable
    {
        /// <summary>
        /// Gets the database connection associated with this transaction.
        /// </summary>
        IDbConnection? Connection { get; }

        /// <summary>
        /// Gets the isolation level for this transaction.
        /// </summary>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Commits the database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous commit operation.</returns>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the database transaction asynchronously.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous rollback operation.</returns>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the database transaction synchronously.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the database transaction synchronously.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Disposes the transaction synchronously.
        /// </summary>
        void Dispose();
    }
}
