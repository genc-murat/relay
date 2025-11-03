using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Represents a unit of work pattern for managing transactional operations with enhanced transaction management capabilities.
    /// Typically implemented by DbContext or other data access abstractions.
    /// </summary>
    /// <remarks>
    /// This interface is designed to work with Entity Framework Core's DbContext
    /// and other data access patterns that support transactional operations.
    /// 
    /// <para><strong>Breaking Change:</strong> The parameterless BeginTransactionAsync method has been removed.
    /// All transactions now require an explicit isolation level to be specified.</para>
    ///
    /// Example implementation with EF Core:
    /// <code>
    /// public class ApplicationDbContext : DbContext, IUnitOfWork
    /// {
    ///     private ITransactionContext? _currentTransactionContext;
    ///     private bool _isReadOnly;
    ///     
    ///     public ITransactionContext? CurrentTransactionContext => _currentTransactionContext;
    ///     
    ///     public bool IsReadOnly 
    ///     { 
    ///         get => _isReadOnly;
    ///         set => _isReadOnly = value;
    ///     }
    ///     
    ///     public async Task&lt;IDbTransaction&gt; BeginTransactionAsync(
    ///         IsolationLevel isolationLevel, 
    ///         CancellationToken cancellationToken = default)
    ///     {
    ///         var efTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    ///         return efTransaction.GetDbTransaction();
    ///     }
    ///     
    ///     public async Task&lt;ISavepoint&gt; CreateSavepointAsync(
    ///         string name, 
    ///         CancellationToken cancellationToken = default)
    ///     {
    ///         if (_currentTransactionContext == null)
    ///             throw new InvalidOperationException("No active transaction.");
    ///             
    ///         return await _currentTransactionContext.CreateSavepointAsync(name, cancellationToken);
    ///     }
    ///     
    ///     public async Task RollbackToSavepointAsync(
    ///         string name, 
    ///         CancellationToken cancellationToken = default)
    ///     {
    ///         if (_currentTransactionContext == null)
    ///             throw new InvalidOperationException("No active transaction.");
    ///             
    ///         await _currentTransactionContext.RollbackToSavepointAsync(name, cancellationToken);
    ///     }
    ///     
    ///     // Override SaveChangesAsync to enforce read-only transactions
    ///     public override async Task&lt;int&gt; SaveChangesAsync(CancellationToken cancellationToken = default)
    ///     {
    ///         // Enforce read-only transaction constraint
    ///         ReadOnlyTransactionEnforcer.ThrowIfReadOnly(this);
    ///         
    ///         return await base.SaveChangesAsync(cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Starts a new database transaction with the specified isolation level.
        /// </summary>
        /// <param name="isolationLevel">The isolation level for the transaction. Must not be Unspecified.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>An <see cref="IDbTransaction"/> that can be used to control the transaction.</returns>
        /// <exception cref="ArgumentException">Thrown when isolationLevel is Unspecified.</exception>
        /// <remarks>
        /// <para><strong>Breaking Change:</strong> This method now requires an explicit isolation level parameter.
        /// The parameterless overload has been removed to enforce explicit isolation level specification.</para>
        /// 
        /// <para>The isolation level determines the locking behavior and consistency guarantees:
        /// <list type="bullet">
        /// <item><description><see cref="IsolationLevel.ReadUncommitted"/>: Allows dirty reads, non-repeatable reads, and phantom reads.</description></item>
        /// <item><description><see cref="IsolationLevel.ReadCommitted"/>: Prevents dirty reads but allows non-repeatable reads and phantom reads.</description></item>
        /// <item><description><see cref="IsolationLevel.RepeatableRead"/>: Prevents dirty reads and non-repeatable reads but allows phantom reads.</description></item>
        /// <item><description><see cref="IsolationLevel.Serializable"/>: Prevents all anomalies but has the highest locking overhead.</description></item>
        /// <item><description><see cref="IsolationLevel.Snapshot"/>: Uses row versioning to provide consistent reads without blocking.</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves all changes made in this unit of work to the database.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <exception cref="ReadOnlyTransactionViolationException">Thrown when attempting to save changes in a read-only transaction.</exception>
        /// <remarks>
        /// If the unit of work is in read-only mode (<see cref="IsReadOnly"/> is true),
        /// this method will throw a <see cref="ReadOnlyTransactionViolationException"/>.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

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
        /// Savepoints allow partial rollback within a transaction without rolling back the entire transaction.
        /// This is useful for implementing complex business logic with multiple steps where some steps
        /// may need to be retried or undone without affecting the entire transaction.
        /// 
        /// <para>Savepoint names must be unique within the transaction. Creating a savepoint with a duplicate name
        /// will throw an exception.</para>
        /// 
        /// <para>Example usage:
        /// <code>
        /// var savepoint = await unitOfWork.CreateSavepointAsync("BeforeRiskyOperation");
        /// try
        /// {
        ///     await PerformRiskyOperation();
        /// }
        /// catch (Exception)
        /// {
        ///     await unitOfWork.RollbackToSavepointAsync("BeforeRiskyOperation");
        ///     // Transaction continues, but risky operation is undone
        /// }
        /// finally
        /// {
        ///     await savepoint.DisposeAsync();
        /// }
        /// </code>
        /// </para>
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

        /// <summary>
        /// Gets the current transaction context, if a transaction is active.
        /// </summary>
        /// <remarks>
        /// The transaction context provides access to transaction metadata such as transaction ID,
        /// isolation level, nesting level, and savepoint management operations.
        /// 
        /// <para>Returns null if no transaction is currently active.</para>
        /// </remarks>
        ITransactionContext? CurrentTransactionContext { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this unit of work is in read-only mode.
        /// </summary>
        /// <remarks>
        /// When set to true, any attempt to save changes will throw a <see cref="ReadOnlyTransactionViolationException"/>.
        /// This is useful for query-only operations where you want to ensure no modifications are made.
        /// 
        /// <para>Read-only transactions can be optimized by the database engine for better performance.</para>
        /// </remarks>
        bool IsReadOnly { get; set; }
    }
}
