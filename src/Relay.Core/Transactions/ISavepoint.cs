using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents a savepoint within a transaction that can be rolled back to or released.
    /// </summary>
    /// <remarks>
    /// Savepoints allow partial rollback within a transaction without rolling back the entire transaction.
    /// This is useful for implementing complex business logic with multiple steps where some steps
    /// may need to be retried or undone without affecting the entire transaction.
    /// 
    /// Example usage:
    /// <code>
    /// var savepoint = await transaction.CreateSavepointAsync("BeforeRiskyOperation");
    /// try
    /// {
    ///     await PerformRiskyOperation();
    /// }
    /// catch (Exception)
    /// {
    ///     await savepoint.RollbackAsync();
    ///     // Transaction continues, but risky operation is undone
    /// }
    /// finally
    /// {
    ///     await savepoint.DisposeAsync();
    /// }
    /// </code>
    /// </remarks>
    public interface ISavepoint : IAsyncDisposable
    {
        /// <summary>
        /// Gets the name of the savepoint.
        /// </summary>
        /// <remarks>
        /// Savepoint names must be unique within a transaction and are used to identify
        /// the savepoint for rollback operations.
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Gets the timestamp when the savepoint was created.
        /// </summary>
        /// <remarks>
        /// This is useful for diagnostics and tracking the lifecycle of savepoints.
        /// </remarks>
        DateTime CreatedAt { get; }

        /// <summary>
        /// Rolls back the transaction to this savepoint, undoing all changes made after the savepoint was created.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous rollback operation.</returns>
        /// <exception cref="SavepointException">Thrown when the rollback operation fails.</exception>
        /// <remarks>
        /// After rolling back to a savepoint, the transaction continues and can still be committed or rolled back.
        /// The savepoint remains valid after rollback and can be rolled back to again if needed.
        /// </remarks>
        Task RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases the savepoint, freeing any resources associated with it.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous release operation.</returns>
        /// <remarks>
        /// Once released, the savepoint can no longer be used for rollback operations.
        /// Releasing savepoints that are no longer needed can improve performance by reducing
        /// the overhead of maintaining savepoint state.
        /// </remarks>
        Task ReleaseAsync(CancellationToken cancellationToken = default);
    }
}
