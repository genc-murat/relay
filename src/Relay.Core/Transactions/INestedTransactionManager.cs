using System;
using System.Data;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Interface for managing nested transaction scenarios by tracking transaction nesting levels
    /// and coordinating transaction reuse for nested transactional requests.
    /// </summary>
    public interface INestedTransactionManager
    {
        /// <summary>
        /// Checks if a transaction is currently active.
        /// </summary>
        /// <returns>True if a transaction is active, otherwise false.</returns>
        bool IsTransactionActive();

        /// <summary>
        /// Gets the current transaction context if any.
        /// </summary>
        /// <returns>The current transaction context or null if no transaction is active.</returns>
        ITransactionContext? GetCurrentContext();

        /// <summary>
        /// Enters a nested transaction by incrementing the nesting level of the current transaction context.
        /// </summary>
        /// <param name="requestType">The type of request entering the nested transaction.</param>
        /// <returns>The current transaction context with incremented nesting level.</returns>
        ITransactionContext EnterNestedTransaction(string requestType);

        /// <summary>
        /// Exits a nested transaction by decrementing the nesting level of the current transaction context.
        /// </summary>
        /// <param name="requestType">The type of request exiting the nested transaction.</param>
        /// <returns>True if this was the outermost transaction (nesting level reached 0); otherwise, false.</returns>
        bool ExitNestedTransaction(string requestType);

        /// <summary>
        /// Determines if the specified context should commit the transaction.
        /// </summary>
        /// <param name="context">The transaction context to check.</param>
        /// <returns>True if the transaction should be committed, otherwise false.</returns>
        bool ShouldCommitTransaction(ITransactionContext context);

        /// <summary>
        /// Determines if the specified context should roll back the transaction.
        /// </summary>
        /// <param name="context">The transaction context to check.</param>
        /// <returns>True if the transaction should be rolled back, otherwise false.</returns>
        bool ShouldRollbackTransaction(ITransactionContext context);

        /// <summary>
        /// Validates that the nested transaction configuration is compatible with the outer transaction.
        /// </summary>
        /// <param name="outerContext">The outer transaction context.</param>
        /// <param name="nestedConfiguration">The configuration for the nested transaction.</param>
        /// <param name="requestType">The type of the nested request.</param>
        void ValidateNestedTransactionConfiguration(
            ITransactionContext outerContext,
            ITransactionConfiguration nestedConfiguration,
            string requestType);
    }
}