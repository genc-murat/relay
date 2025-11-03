using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Manages nested transaction scenarios by tracking transaction nesting levels
    /// and coordinating transaction reuse for nested transactional requests.
    /// </summary>
    /// <remarks>
    /// The NestedTransactionManager is responsible for:
    /// - Detecting when a transactional request is executed within an existing transaction
    /// - Reusing the existing transaction instead of creating a new one
    /// - Tracking and managing the transaction nesting level
    /// - Ensuring only the outermost transaction commits or rolls back
    /// 
    /// <para>When a nested transactional request is detected, the manager increments the nesting level
    /// and returns the existing transaction context. When the nested request completes, the nesting level
    /// is decremented. Only when the nesting level reaches zero (outermost transaction) is the transaction
    /// actually committed or rolled back.</para>
    /// 
    /// <para>Example scenario:
    /// <code>
    /// // Outermost request (nesting level 0)
    /// [Transaction(IsolationLevel.ReadCommitted)]
    /// public record CreateOrderCommand : ITransactionalRequest;
    /// 
    /// // Handler calls another transactional request
    /// public class CreateOrderHandler : IRequestHandler&lt;CreateOrderCommand, OrderResult&gt;
    /// {
    ///     public async Task&lt;OrderResult&gt; HandleAsync(CreateOrderCommand request, CancellationToken ct)
    ///     {
    ///         // This will reuse the existing transaction (nesting level 1)
    ///         await _relay.SendAsync(new UpdateInventoryCommand(), ct);
    ///         return new OrderResult();
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public sealed class NestedTransactionManager : INestedTransactionManager
    {
        private readonly ILogger<NestedTransactionManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="NestedTransactionManager"/> class.
        /// </summary>
        /// <param name="logger">The logger for diagnostic output.</param>
        public NestedTransactionManager(ILogger<NestedTransactionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Checks if a transaction is currently active in the current async context.
        /// </summary>
        /// <returns>True if a transaction context is active; otherwise, false.</returns>
        public bool IsTransactionActive()
        {
            return TransactionContextAccessor.HasActiveContext();
        }

        /// <summary>
        /// Gets the current transaction context if one is active.
        /// </summary>
        /// <returns>The current transaction context, or null if no transaction is active.</returns>
        public ITransactionContext? GetCurrentContext()
        {
            return TransactionContextAccessor.Current;
        }

        /// <summary>
        /// Enters a nested transaction by incrementing the nesting level of the current transaction context.
        /// </summary>
        /// <param name="requestType">The type of request entering the nested transaction.</param>
        /// <returns>The current transaction context with incremented nesting level.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction context is active.</exception>
        /// <remarks>
        /// This method should be called when a nested transactional request is detected.
        /// It increments the nesting level to track how deep the transaction nesting is.
        /// The caller should ensure that <see cref="ExitNestedTransaction"/> is called when the nested request completes.
        /// </remarks>
        public ITransactionContext EnterNestedTransaction(string requestType)
        {
            var context = TransactionContextAccessor.CurrentInternal;
            
            if (context == null)
            {
                throw new InvalidOperationException(
                    $"Cannot enter nested transaction for {requestType}: No active transaction context found.");
            }

            var previousLevel = context.NestingLevel;
            context.IncrementNestingLevel();

            _logger.LogDebug(
                "Entering nested transaction for {RequestType}. Transaction {TransactionId} nesting level: {PreviousLevel} -> {CurrentLevel}",
                requestType,
                context.TransactionId,
                previousLevel,
                context.NestingLevel);

            return context;
        }

        /// <summary>
        /// Exits a nested transaction by decrementing the nesting level of the current transaction context.
        /// </summary>
        /// <param name="requestType">The type of request exiting the nested transaction.</param>
        /// <returns>True if this was the outermost transaction (nesting level reached 0); otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no transaction context is active.</exception>
        /// <remarks>
        /// This method should be called when a nested transactional request completes.
        /// It decrements the nesting level and returns true if the outermost transaction has completed,
        /// indicating that the transaction should be committed or rolled back.
        /// </remarks>
        public bool ExitNestedTransaction(string requestType)
        {
            var context = TransactionContextAccessor.CurrentInternal;
            
            if (context == null)
            {
                throw new InvalidOperationException(
                    $"Cannot exit nested transaction for {requestType}: No active transaction context found.");
            }

            var previousLevel = context.NestingLevel;
            context.DecrementNestingLevel();

            _logger.LogDebug(
                "Exiting nested transaction for {RequestType}. Transaction {TransactionId} nesting level: {PreviousLevel} -> {CurrentLevel}",
                requestType,
                context.TransactionId,
                previousLevel,
                context.NestingLevel);

            // Return true if we've reached the outermost transaction (level 0)
            return context.NestingLevel == 0;
        }

        /// <summary>
        /// Determines whether the transaction should be committed based on the nesting level.
        /// </summary>
        /// <param name="context">The transaction context to check.</param>
        /// <returns>True if the transaction should be committed (outermost transaction); otherwise, false.</returns>
        /// <remarks>
        /// Only the outermost transaction (nesting level 0) should actually commit the transaction.
        /// Nested transactions should not commit, as they are reusing the outer transaction.
        /// </remarks>
        public bool ShouldCommitTransaction(ITransactionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var shouldCommit = context.NestingLevel == 0;

            if (!shouldCommit)
            {
                _logger.LogDebug(
                    "Skipping commit for nested transaction {TransactionId} at nesting level {NestingLevel}",
                    context.TransactionId,
                    context.NestingLevel);
            }

            return shouldCommit;
        }

        /// <summary>
        /// Determines whether the transaction should be rolled back based on the nesting level.
        /// </summary>
        /// <param name="context">The transaction context to check.</param>
        /// <returns>True if the transaction should be rolled back (outermost transaction); otherwise, false.</returns>
        /// <remarks>
        /// Only the outermost transaction (nesting level 0) should actually roll back the transaction.
        /// When a nested transaction fails, the exception propagates to the outer transaction,
        /// which will handle the rollback.
        /// </remarks>
        public bool ShouldRollbackTransaction(ITransactionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var shouldRollback = context.NestingLevel == 0;

            if (!shouldRollback)
            {
                _logger.LogDebug(
                    "Skipping rollback for nested transaction {TransactionId} at nesting level {NestingLevel}. Exception will propagate to outer transaction.",
                    context.TransactionId,
                    context.NestingLevel);
            }

            return shouldRollback;
        }

        /// <summary>
        /// Validates that the nested transaction configuration is compatible with the outer transaction.
        /// </summary>
        /// <param name="outerContext">The outer transaction context.</param>
        /// <param name="nestedConfiguration">The configuration for the nested transaction.</param>
        /// <param name="requestType">The type of the nested request.</param>
        /// <exception cref="NestedTransactionException">Thrown when the nested transaction configuration is incompatible.</exception>
        /// <remarks>
        /// This method validates that:
        /// - The isolation level of the nested transaction matches the outer transaction
        /// - Read-only constraints are compatible (nested can be read-only if outer is, but not vice versa)
        /// 
        /// If the configurations are incompatible, a NestedTransactionException is thrown with details
        /// about the incompatibility.
        /// </remarks>
        public void ValidateNestedTransactionConfiguration(
            ITransactionContext outerContext,
            ITransactionConfiguration nestedConfiguration,
            string requestType)
        {
            if (outerContext == null)
                throw new ArgumentNullException(nameof(outerContext));
            if (nestedConfiguration == null)
                throw new ArgumentNullException(nameof(nestedConfiguration));

            // Validate isolation level compatibility
            if (nestedConfiguration.IsolationLevel != outerContext.IsolationLevel)
            {
                var message = $"Nested transaction for {requestType} has incompatible isolation level. " +
                    $"Outer transaction: {outerContext.IsolationLevel}, Nested transaction: {nestedConfiguration.IsolationLevel}. " +
                    "Nested transactions must use the same isolation level as the outer transaction.";

                _logger.LogError(
                    "Nested transaction configuration validation failed for {RequestType}. {ErrorMessage}",
                    requestType,
                    message);

                throw new NestedTransactionException(
                    message,
                    outerContext.TransactionId,
                    outerContext.NestingLevel,
                    requestType);
            }

            // Validate read-only compatibility
            // A nested transaction can be read-only if the outer is read-only or read-write
            // But a nested read-write transaction cannot be inside a read-only outer transaction
            if (!nestedConfiguration.IsReadOnly && outerContext.IsReadOnly)
            {
                var message = $"Nested transaction for {requestType} cannot be read-write when outer transaction is read-only. " +
                    "A read-only transaction cannot contain write operations.";

                _logger.LogError(
                    "Nested transaction configuration validation failed for {RequestType}. {ErrorMessage}",
                    requestType,
                    message);

                throw new NestedTransactionException(
                    message,
                    outerContext.TransactionId,
                    outerContext.NestingLevel,
                    requestType);
            }

            _logger.LogDebug(
                "Nested transaction configuration validated successfully for {RequestType} in transaction {TransactionId}",
                requestType,
                outerContext.TransactionId);
        }
    }
}
