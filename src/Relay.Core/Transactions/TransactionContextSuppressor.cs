using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides a mechanism to suppress transaction context propagation within a specific scope.
    /// </summary>
    /// <remarks>
    /// This class is useful when you need to execute code that should not participate in the
    /// current transaction context. For example, logging operations or audit trail writes that
    /// should succeed even if the main transaction is rolled back.
    /// 
    /// <para>The suppression is scoped - when the suppressor is disposed, the original transaction
    /// context is restored.</para>
    /// 
    /// <para>Example usage:
    /// <code>
    /// // Within a transaction
    /// var context = TransactionContextAccessor.Current;
    /// Console.WriteLine($"Transaction ID: {context?.TransactionId}"); // Has value
    /// 
    /// using (var suppressor = new TransactionContextSuppressor())
    /// {
    ///     // Transaction context is suppressed here
    ///     var suppressedContext = TransactionContextAccessor.Current;
    ///     Console.WriteLine($"Suppressed: {suppressedContext == null}"); // True
    ///     
    ///     // Perform operations that should not be part of the transaction
    ///     await LogAuditTrailAsync();
    /// }
    /// 
    /// // Transaction context is restored
    /// var restoredContext = TransactionContextAccessor.Current;
    /// Console.WriteLine($"Restored: {restoredContext?.TransactionId}"); // Same as before
    /// </code>
    /// </para>
    /// </remarks>
    public sealed class TransactionContextSuppressor : IDisposable
    {
        private readonly ITransactionContext? _suppressedContext;
        private bool _isDisposed;

        /// <summary>
        /// Gets a value indicating whether the transaction context is currently suppressed.
        /// </summary>
        public bool IsSuppressed { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContextSuppressor"/> class
        /// and suppresses the current transaction context.
        /// </summary>
        /// <remarks>
        /// The current transaction context is saved and will be restored when this instance is disposed.
        /// If no transaction context is active, this has no effect.
        /// </remarks>
        public TransactionContextSuppressor()
        {
            _suppressedContext = TransactionContextAccessor.Current;
            
            if (_suppressedContext != null)
            {
                TransactionContextAccessor.Current = null;
                IsSuppressed = true;
            }
        }

        /// <summary>
        /// Restores the suppressed transaction context.
        /// </summary>
        /// <remarks>
        /// This method is called automatically when the suppressor is disposed.
        /// It can be called manually to restore the context before disposal if needed.
        /// </remarks>
        public void Restore()
        {
            if (_isDisposed)
                return;

            if (IsSuppressed)
            {
                TransactionContextAccessor.Current = _suppressedContext;
                IsSuppressed = false;
            }
        }

        /// <summary>
        /// Disposes the suppressor and restores the original transaction context.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Restore();
            _isDisposed = true;
        }
    }
}
