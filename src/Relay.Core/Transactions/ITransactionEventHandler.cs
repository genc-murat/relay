using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Defines methods for handling transaction lifecycle events.
    /// </summary>
    /// <remarks>
    /// Implement this interface to execute custom logic at specific points in the transaction lifecycle.
    /// Event handlers are invoked by the transaction system and can be used for cross-cutting concerns
    /// such as auditing, notifications, cache invalidation, or custom business logic.
    /// 
    /// <para><strong>Event Execution Order:</strong></para>
    /// <list type="number">
    /// <item><description>OnBeforeBeginAsync - Before transaction starts</description></item>
    /// <item><description>OnAfterBeginAsync - After transaction starts</description></item>
    /// <item><description>... request handler executes ...</description></item>
    /// <item><description>OnBeforeCommitAsync - Before transaction commits (can prevent commit by throwing)</description></item>
    /// <item><description>OnAfterCommitAsync - After transaction commits (errors logged but don't fail)</description></item>
    /// </list>
    /// 
    /// <para>Or in case of rollback:</para>
    /// <list type="number">
    /// <item><description>OnBeforeRollbackAsync - Before transaction rolls back</description></item>
    /// <item><description>OnAfterRollbackAsync - After transaction rolls back (errors logged but don't fail)</description></item>
    /// </list>
    /// 
    /// <para><strong>Error Handling:</strong></para>
    /// <list type="bullet">
    /// <item><description>Exceptions in OnBeforeCommitAsync will cause the transaction to roll back</description></item>
    /// <item><description>Exceptions in OnAfterCommitAsync and OnAfterRollbackAsync are logged but don't affect the transaction</description></item>
    /// <item><description>Exceptions in other events are propagated and may affect transaction outcome</description></item>
    /// </list>
    /// 
    /// <para><strong>Example Implementation:</strong></para>
    /// <code>
    /// public class AuditEventHandler : ITransactionEventHandler
    /// {
    ///     private readonly IAuditLog _auditLog;
    ///     
    ///     public AuditEventHandler(IAuditLog auditLog)
    ///     {
    ///         _auditLog = auditLog;
    ///     }
    ///     
    ///     public async Task OnAfterCommitAsync(TransactionEventContext context, CancellationToken cancellationToken)
    ///     {
    ///         await _auditLog.WriteAsync(
    ///             $"Transaction {context.TransactionId} for {context.RequestType} committed",
    ///             cancellationToken);
    ///     }
    ///     
    ///     // Implement other methods as needed, or use default implementations
    ///     public Task OnBeforeBeginAsync(TransactionEventContext context, CancellationToken cancellationToken) 
    ///         => Task.CompletedTask;
    ///     
    ///     public Task OnAfterBeginAsync(TransactionEventContext context, CancellationToken cancellationToken) 
    ///         => Task.CompletedTask;
    ///     
    ///     public Task OnBeforeCommitAsync(TransactionEventContext context, CancellationToken cancellationToken) 
    ///         => Task.CompletedTask;
    ///     
    ///     public Task OnBeforeRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken) 
    ///         => Task.CompletedTask;
    ///     
    ///     public Task OnAfterRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken) 
    ///         => Task.CompletedTask;
    /// }
    /// </code>
    /// 
    /// <para><strong>Registration:</strong></para>
    /// <code>
    /// services.AddTransient&lt;ITransactionEventHandler, AuditEventHandler&gt;();
    /// services.AddTransient&lt;ITransactionEventHandler, CacheInvalidationHandler&gt;();
    /// </code>
    /// </remarks>
    public interface ITransactionEventHandler
    {
        /// <summary>
        /// Called before a transaction begins.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised before the database transaction is created. It can be used to
        /// perform setup operations or validation before the transaction starts.
        /// 
        /// If this method throws an exception, the transaction will not be started and the
        /// exception will be propagated to the caller.
        /// </remarks>
        Task OnBeforeBeginAsync(TransactionEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called after a transaction has begun.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised after the database transaction has been successfully created.
        /// It can be used to perform initialization operations that require an active transaction.
        /// 
        /// If this method throws an exception, the transaction will be rolled back and the
        /// exception will be propagated to the caller.
        /// </remarks>
        Task OnAfterBeginAsync(TransactionEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called before a transaction is committed.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised before the database transaction is committed. It can be used to
        /// perform validation or pre-commit operations.
        /// 
        /// <strong>IMPORTANT:</strong> If this method throws an exception, the transaction will be
        /// rolled back instead of committed. This allows event handlers to prevent a commit if
        /// validation fails or other issues are detected.
        /// 
        /// Example use cases:
        /// <list type="bullet">
        /// <item><description>Final validation before commit</description></item>
        /// <item><description>Checking business rules that span multiple entities</description></item>
        /// <item><description>Preparing data for post-commit operations</description></item>
        /// </list>
        /// </remarks>
        Task OnBeforeCommitAsync(TransactionEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called after a transaction has been committed.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised after the database transaction has been successfully committed.
        /// It can be used to perform post-commit operations such as cache invalidation, sending
        /// notifications, or triggering background jobs.
        /// 
        /// <strong>IMPORTANT:</strong> Exceptions thrown by this method are logged but do not affect
        /// the transaction outcome. The transaction has already been committed and cannot be rolled back.
        /// 
        /// Example use cases:
        /// <list type="bullet">
        /// <item><description>Cache invalidation</description></item>
        /// <item><description>Sending notifications or events</description></item>
        /// <item><description>Triggering background jobs</description></item>
        /// <item><description>Updating search indexes</description></item>
        /// </list>
        /// </remarks>
        Task OnAfterCommitAsync(TransactionEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called before a transaction is rolled back.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised before the database transaction is rolled back. It can be used to
        /// perform cleanup operations or logging before the rollback occurs.
        /// 
        /// If this method throws an exception, the rollback will still proceed, but the exception
        /// will be logged and may be propagated depending on the context.
        /// </remarks>
        Task OnBeforeRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Called after a transaction has been rolled back.
        /// </summary>
        /// <param name="context">The transaction event context containing transaction metadata.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// This event is raised after the database transaction has been rolled back. It can be used
        /// to perform cleanup operations or logging after the rollback.
        /// 
        /// <strong>IMPORTANT:</strong> Exceptions thrown by this method are logged but do not affect
        /// the transaction outcome. The transaction has already been rolled back.
        /// 
        /// Example use cases:
        /// <list type="bullet">
        /// <item><description>Logging rollback events</description></item>
        /// <item><description>Cleaning up temporary resources</description></item>
        /// <item><description>Sending failure notifications</description></item>
        /// </list>
        /// </remarks>
        Task OnAfterRollbackAsync(TransactionEventContext context, CancellationToken cancellationToken = default);
    }
}
