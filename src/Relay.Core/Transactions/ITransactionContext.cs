using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents the context of an active transaction, providing access to transaction metadata
    /// and savepoint management operations.
    /// </summary>
    /// <remarks>
    /// The transaction context is maintained in AsyncLocal storage and flows through async operations.
    /// It provides access to transaction metadata such as transaction ID, nesting level, and isolation level,
    /// as well as methods for managing savepoints within the transaction.
    /// 
    /// Example usage:
    /// <code>
    /// var context = unitOfWork.CurrentTransactionContext;
    /// if (context != null)
    /// {
    ///     Console.WriteLine($"Transaction ID: {context.TransactionId}");
    ///     Console.WriteLine($"Isolation Level: {context.IsolationLevel}");
    ///     
    ///     var savepoint = await context.CreateSavepointAsync("checkpoint1");
    ///     // ... perform operations ...
    ///     await context.RollbackToSavepointAsync("checkpoint1");
    /// }
    /// </code>
    /// </remarks>
    public interface ITransactionContext
    {
        /// <summary>
        /// Gets the unique identifier for this transaction.
        /// </summary>
        /// <remarks>
        /// The transaction ID is used for correlation in logs, traces, and metrics.
        /// It remains constant throughout the transaction lifecycle, even across nested transactions.
        /// </remarks>
        string TransactionId { get; }

        /// <summary>
        /// Gets the nesting level of this transaction.
        /// </summary>
        /// <remarks>
        /// The nesting level indicates how many nested transactional requests are currently active.
        /// - Level 0: Outermost transaction
        /// - Level 1+: Nested transaction (reusing the outer transaction)
        /// 
        /// This is useful for diagnostics and understanding the transaction call stack.
        /// </remarks>
        int NestingLevel { get; }

        /// <summary>
        /// Gets the isolation level of this transaction.
        /// </summary>
        /// <remarks>
        /// The isolation level determines the locking behavior and consistency guarantees.
        /// This value is set when the transaction begins and cannot be changed during the transaction.
        /// </remarks>
        IsolationLevel IsolationLevel { get; }

        /// <summary>
        /// Gets a value indicating whether this transaction is read-only.
        /// </summary>
        /// <remarks>
        /// Read-only transactions prevent data modifications and can be optimized by the database engine.
        /// Any attempt to save changes in a read-only transaction will throw a ReadOnlyTransactionViolationException.
        /// </remarks>
        bool IsReadOnly { get; }

        /// <summary>
        /// Gets the timestamp when this transaction was started.
        /// </summary>
        /// <remarks>
        /// This is useful for calculating transaction duration and detecting long-running transactions.
        /// </remarks>
        DateTime StartedAt { get; }

        /// <summary>
        /// Gets the current database transaction object.
        /// </summary>
        /// <remarks>
        /// This provides access to the underlying database transaction for advanced scenarios.
        /// Returns null if the transaction has not been started or has been disposed.
        /// </remarks>
        IDbTransaction? CurrentTransaction { get; }

        /// <summary>
        /// Creates a named savepoint within the current transaction.
        /// </summary>
        /// <param name="name">The unique name for the savepoint.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A savepoint that can be used for rollback operations.</returns>
        /// <exception cref="ArgumentException">Thrown when the savepoint name is null, empty, or already exists.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <exception cref="SavepointException">Thrown when savepoint creation fails.</exception>
        /// <remarks>
        /// Savepoint names must be unique within the transaction. Creating a savepoint with a duplicate name
        /// will throw an exception. Savepoints allow partial rollback without affecting the entire transaction.
        /// </remarks>
        Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction to a previously created savepoint.
        /// </summary>
        /// <param name="name">The name of the savepoint to roll back to.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous rollback operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the savepoint name is null or empty.</exception>
        /// <exception cref="SavepointNotFoundException">Thrown when the specified savepoint does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no transaction is active.</exception>
        /// <remarks>
        /// Rolling back to a savepoint undoes all changes made after the savepoint was created,
        /// but the transaction continues and can still be committed or rolled back.
        /// </remarks>
        Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default);
    }
}
