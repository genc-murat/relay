using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Implementation of <see cref="ITransactionContext"/> that provides transaction metadata
    /// and savepoint management operations.
    /// </summary>
    /// <remarks>
    /// This class is used internally by the transaction system to track transaction state
    /// and provide access to transaction operations. It is stored in AsyncLocal storage
    /// to flow through async operations.
    /// </remarks>
    internal sealed class TransactionContext : ITransactionContext
    {
        private readonly SavepointManager _savepointManager;
        private IRelayDbTransaction? _currentTransaction;
        private bool _isDisposed;

        /// <inheritdoc />
        public string TransactionId { get; }

        /// <inheritdoc />
        public int NestingLevel { get; internal set; }

        /// <inheritdoc />
        public IsolationLevel IsolationLevel { get; }

        /// <inheritdoc />
        public bool IsReadOnly { get; }

        /// <inheritdoc />
        public DateTime StartedAt { get; }

        /// <inheritdoc />
        public IRelayDbTransaction? CurrentTransaction
        {
            get => _currentTransaction;
            internal set => _currentTransaction = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionContext"/> class.
        /// </summary>
        /// <param name="transaction">The database transaction.</param>
        /// <param name="isolationLevel">The isolation level of the transaction.</param>
        /// <param name="isReadOnly">Whether the transaction is read-only.</param>
        /// <param name="nestingLevel">The nesting level of the transaction (0 for outermost).</param>
        /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
        /// <exception cref="ArgumentException">Thrown when isolationLevel is Unspecified.</exception>
        public TransactionContext(
            IRelayDbTransaction transaction,
            IsolationLevel isolationLevel,
            bool isReadOnly = false,
            int nestingLevel = 0)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            if (isolationLevel == IsolationLevel.Unspecified)
                throw new ArgumentException("Isolation level cannot be Unspecified.", nameof(isolationLevel));

            if (nestingLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(nestingLevel), "Nesting level cannot be negative.");

            _currentTransaction = transaction;
            _savepointManager = new SavepointManager(transaction);

            TransactionId = Guid.NewGuid().ToString("N");
            IsolationLevel = isolationLevel;
            IsReadOnly = isReadOnly;
            NestingLevel = nestingLevel;
            StartedAt = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_currentTransaction == null)
                throw new InvalidOperationException("No active transaction.");

            return await _savepointManager.CreateSavepointAsync(name, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_currentTransaction == null)
                throw new InvalidOperationException("No active transaction.");

            await _savepointManager.RollbackToSavepointAsync(name, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Increments the nesting level when a nested transaction is detected.
        /// </summary>
        internal void IncrementNestingLevel()
        {
            NestingLevel++;
        }

        /// <summary>
        /// Decrements the nesting level when a nested transaction completes.
        /// </summary>
        internal void DecrementNestingLevel()
        {
            if (NestingLevel > 0)
            {
                NestingLevel--;
            }
        }

        /// <summary>
        /// Cleans up savepoints when the transaction completes.
        /// </summary>
        internal async Task CleanupAsync()
        {
            if (!_isDisposed)
            {
                await _savepointManager.CleanupAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the transaction context and cleans up resources.
        /// </summary>
        internal async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            await _savepointManager.DisposeAsync().ConfigureAwait(false);
            _currentTransaction = null;
            _isDisposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TransactionContext));
            }
        }
    }
}
