using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Manages savepoint creation, tracking, and lifecycle within a transaction.
    /// </summary>
    internal sealed class SavepointManager : IAsyncDisposable
    {
        private readonly IDbTransaction _transaction;
        private readonly Dictionary<string, ISavepoint> _savepoints;
        private readonly object _lock = new object();
        private bool _isDisposed;

        /// <summary>
        /// Gets the count of active savepoints.
        /// </summary>
        public int SavepointCount
        {
            get
            {
                lock (_lock)
                {
                    return _savepoints.Count;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavepointManager"/> class.
        /// </summary>
        /// <param name="transaction">The database transaction to manage savepoints for.</param>
        /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
        public SavepointManager(IDbTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _savepoints = new Dictionary<string, ISavepoint>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a new savepoint with the specified name.
        /// </summary>
        /// <param name="name">The unique name for the savepoint.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>The created savepoint.</returns>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        /// <exception cref="SavepointAlreadyExistsException">Thrown when a savepoint with the same name already exists.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateSavepointName(name);

            lock (_lock)
            {
                if (_savepoints.ContainsKey(name))
                {
                    throw new SavepointAlreadyExistsException(name);
                }
            }

            try
            {
                // Create the savepoint in the database
                var connection = _transaction.Connection;
                if (connection == null)
                    throw new InvalidOperationException("Transaction connection is not available.");
                    
                var command = connection.CreateCommand();

                // Cast to System.Data.IDbTransaction for command assignment
                if (_transaction is System.Data.IDbTransaction sysTransaction)
                {
                    command.Transaction = sysTransaction;
                }
                
                command.CommandText = $"SAVEPOINT {name}";

                await Task.Run(() => command.ExecuteNonQuery(), cancellationToken).ConfigureAwait(false);
                command.Dispose();

                // Create the savepoint object
                var savepoint = new Savepoint(name, _transaction);

                lock (_lock)
                {
                    _savepoints[name] = savepoint;
                }

                return savepoint;
            }
            catch (Exception ex) when (ex is not SavepointException)
            {
                throw new SavepointException($"Failed to create savepoint '{name}'.", ex);
            }
        }

        /// <summary>
        /// Rolls back the transaction to the specified savepoint.
        /// </summary>
        /// <param name="name">The name of the savepoint to roll back to.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="ArgumentException">Thrown when name is null, empty, or whitespace.</exception>
        /// <exception cref="SavepointNotFoundException">Thrown when the specified savepoint does not exist.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the manager has been disposed.</exception>
        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateSavepointName(name);

            ISavepoint savepoint;
            lock (_lock)
            {
                if (!_savepoints.TryGetValue(name, out savepoint!))
                {
                    throw new SavepointNotFoundException(name);
                }
            }

            await savepoint.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a savepoint by name.
        /// </summary>
        /// <param name="name">The name of the savepoint.</param>
        /// <returns>The savepoint if found; otherwise, null.</returns>
        public ISavepoint? GetSavepoint(string name)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(name))
                return null;

            lock (_lock)
            {
                return _savepoints.TryGetValue(name, out var savepoint) ? savepoint : null;
            }
        }

        /// <summary>
        /// Checks if a savepoint with the specified name exists.
        /// </summary>
        /// <param name="name">The name of the savepoint.</param>
        /// <returns>True if the savepoint exists; otherwise, false.</returns>
        public bool HasSavepoint(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            lock (_lock)
            {
                return _savepoints.ContainsKey(name);
            }
        }

        /// <summary>
        /// Releases a savepoint by name.
        /// </summary>
        /// <param name="name">The name of the savepoint to release.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <exception cref="SavepointNotFoundException">Thrown when the specified savepoint does not exist.</exception>
        public async Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ValidateSavepointName(name);

            ISavepoint savepoint;
            lock (_lock)
            {
                if (!_savepoints.TryGetValue(name, out savepoint!))
                {
                    throw new SavepointNotFoundException(name);
                }
            }

            await savepoint.ReleaseAsync(cancellationToken).ConfigureAwait(false);

            lock (_lock)
            {
                _savepoints.Remove(name);
            }
        }

        /// <summary>
        /// Cleans up all savepoints when the transaction completes.
        /// </summary>
        public async Task CleanupAsync()
        {
            if (_isDisposed)
                return;

            List<ISavepoint> savepointsToDispose;
            lock (_lock)
            {
                savepointsToDispose = _savepoints.Values.ToList();
                _savepoints.Clear();
            }

            foreach (var savepoint in savepointsToDispose)
            {
                try
                {
                    await savepoint.DisposeAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Suppress exceptions during cleanup
                }
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_isDisposed)
                return;

            await CleanupAsync().ConfigureAwait(false);
            _isDisposed = true;
        }

        private void ValidateSavepointName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Savepoint name cannot be null, empty, or whitespace.", nameof(name));
            }

            // Additional validation for SQL injection prevention
            if (name.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                throw new ArgumentException(
                    "Savepoint name can only contain letters, digits, and underscores.",
                    nameof(name));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(SavepointManager));
            }
        }
    }
}
