using System;
using System.Threading;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Provides access to the current transaction context using AsyncLocal storage.
    /// </summary>
    /// <remarks>
    /// This class uses AsyncLocal to store the transaction context, which ensures that
    /// the context flows through async operations and is isolated between concurrent requests.
    /// 
    /// <para>The transaction context is automatically available to all code executing within
    /// the same async context, without needing to explicitly pass it through method parameters.</para>
    /// 
    /// <para>Example usage:
    /// <code>
    /// // Set the current context (typically done by TransactionBehavior)
    /// TransactionContextAccessor.Current = new TransactionContext(...);
    /// 
    /// // Access the current context from anywhere in the async call chain
    /// var context = TransactionContextAccessor.Current;
    /// if (context != null)
    /// {
    ///     Console.WriteLine($"Transaction ID: {context.TransactionId}");
    /// }
    /// 
    /// // Clear the context when done
    /// TransactionContextAccessor.Current = null;
    /// </code>
    /// </para>
    /// </remarks>
    public static class TransactionContextAccessor
    {
        private static readonly AsyncLocal<TransactionContextHolder> _currentContext = new AsyncLocal<TransactionContextHolder>();

        /// <summary>
        /// Gets or sets the current transaction context.
        /// </summary>
        /// <remarks>
        /// The context is stored in AsyncLocal storage, which means it flows through async operations
        /// and is isolated between concurrent requests. Setting this to null clears the current context.
        /// 
        /// <para>When a transaction context is disposed, it should be cleared from the accessor
        /// to prevent accessing disposed contexts.</para>
        /// </remarks>
        public static ITransactionContext? Current
        {
            get => _currentContext.Value?.Context;
            set
            {
                var holder = _currentContext.Value;
                if (holder != null)
                {
                    // Clear the existing holder
                    holder.Context = null;
                }

                if (value != null)
                {
                    // Create a new holder for the new context
                    _currentContext.Value = new TransactionContextHolder { Context = value };
                }
                else
                {
                    // Clear the holder
                    _currentContext.Value = null;
                }
            }
        }

        /// <summary>
        /// Gets the internal TransactionContext implementation if available.
        /// </summary>
        /// <remarks>
        /// This is used internally by the transaction system to access the concrete implementation
        /// for operations like incrementing/decrementing nesting level.
        /// </remarks>
        internal static TransactionContext? CurrentInternal
        {
            get => _currentContext.Value?.Context as TransactionContext;
        }

        /// <summary>
        /// Checks if a transaction context is currently active.
        /// </summary>
        /// <returns>True if a transaction context is active; otherwise, false.</returns>
        public static bool HasActiveContext()
        {
            return Current != null;
        }

        /// <summary>
        /// Clears the current transaction context.
        /// </summary>
        /// <remarks>
        /// This is equivalent to setting <see cref="Current"/> to null.
        /// It should be called when a transaction completes to ensure the context doesn't leak.
        /// </remarks>
        public static void Clear()
        {
            Current = null;
        }

        /// <summary>
        /// Holder class for the transaction context to enable proper AsyncLocal behavior.
        /// </summary>
        /// <remarks>
        /// AsyncLocal requires a reference type to properly track changes across async boundaries.
        /// This holder class wraps the context to ensure proper flow semantics.
        /// </remarks>
        private sealed class TransactionContextHolder
        {
            public ITransactionContext? Context { get; set; }
        }
    }
}
