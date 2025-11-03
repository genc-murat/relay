using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions.TestUtilities
{
    /// <summary>
    /// In-memory implementation of IUnitOfWork for testing purposes.
    /// Simulates transaction operations without requiring a real database.
    /// </summary>
    public class InMemoryUnitOfWork : IUnitOfWork
    {
        private readonly List<string> _changeLog = new();
        private InMemoryTransactionContext? _currentContext;
        private bool _isReadOnly;
        private int _changeCounter;

        /// <summary>
        /// Gets the log of all operations performed on this unit of work.
        /// </summary>
        public IReadOnlyList<string> OperationLog => _changeLog.AsReadOnly();

        /// <summary>
        /// Gets or sets whether SaveChangesAsync should throw an exception.
        /// </summary>
        public bool ShouldThrowOnSave { get; set; }

        /// <summary>
        /// Gets or sets the exception to throw when ShouldThrowOnSave is true.
        /// </summary>
        public Exception? SaveException { get; set; }

        /// <summary>
        /// Gets or sets whether BeginTransactionAsync should throw an exception.
        /// </summary>
        public bool ShouldThrowOnBeginTransaction { get; set; }

        /// <summary>
        /// Gets or sets the exception to throw when ShouldThrowOnBeginTransaction is true.
        /// </summary>
        public Exception? BeginTransactionException { get; set; }

        /// <summary>
        /// Gets the isolation level of the last transaction that was started.
        /// </summary>
        public IsolationLevel? LastIsolationLevel { get; private set; }

        /// <summary>
        /// Gets the number of times SaveChangesAsync was called.
        /// </summary>
        public int SaveChangesCallCount { get; private set; }

        /// <summary>
        /// Gets the number of times BeginTransactionAsync was called.
        /// </summary>
        public int BeginTransactionCallCount { get; private set; }

        /// <summary>
        /// Gets the current transaction context.
        /// </summary>
        public ITransactionContext? CurrentTransactionContext => _currentContext;

        /// <summary>
        /// Gets or sets whether this unit of work is in read-only mode.
        /// </summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;
                _changeLog.Add($"IsReadOnly set to {value}");
            }
        }

        /// <summary>
        /// Begins a new transaction with the specified isolation level.
        /// </summary>
        public async Task<IRelayDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            BeginTransactionCallCount++;
            LastIsolationLevel = isolationLevel;
            _changeLog.Add($"BeginTransaction({isolationLevel})");

            if (ShouldThrowOnBeginTransaction)
            {
                throw BeginTransactionException ?? new InvalidOperationException("BeginTransaction failed");
            }

            await Task.Yield(); // Simulate async operation

            var transaction = new InMemoryDbTransaction(_changeLog);
            _currentContext = new InMemoryTransactionContext(isolationLevel, _isReadOnly, transaction, this);

            return transaction;
        }

        /// <summary>
        /// Saves all changes made in this unit of work.
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            _changeLog.Add("SaveChanges");

            if (_isReadOnly)
            {
                throw new ReadOnlyTransactionViolationException("Cannot save changes in a read-only transaction");
            }

            if (ShouldThrowOnSave)
            {
                throw SaveException ?? new InvalidOperationException("SaveChanges failed");
            }

            await Task.Yield(); // Simulate async operation

            return ++_changeCounter;
        }

        /// <summary>
        /// Creates a named savepoint within the current transaction.
        /// </summary>
        public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_currentContext == null)
            {
                throw new InvalidOperationException("No active transaction");
            }

            _changeLog.Add($"CreateSavepoint({name})");
            return await _currentContext.CreateSavepointAsync(name, cancellationToken);
        }

        /// <summary>
        /// Rolls back the transaction to a previously created savepoint.
        /// </summary>
        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (_currentContext == null)
            {
                throw new InvalidOperationException("No active transaction");
            }

            _changeLog.Add($"RollbackToSavepoint({name})");
            await _currentContext.RollbackToSavepointAsync(name, cancellationToken);
        }

        /// <summary>
        /// Clears the operation log.
        /// </summary>
        public void ClearLog()
        {
            _changeLog.Clear();
        }

        /// <summary>
        /// Resets the unit of work to its initial state.
        /// </summary>
        public void Reset()
        {
            _changeLog.Clear();
            _currentContext = null;
            _isReadOnly = false;
            _changeCounter = 0;
            SaveChangesCallCount = 0;
            BeginTransactionCallCount = 0;
            LastIsolationLevel = null;
            ShouldThrowOnSave = false;
            SaveException = null;
            ShouldThrowOnBeginTransaction = false;
            BeginTransactionException = null;
        }

        /// <summary>
        /// Simulates completing the transaction (for testing purposes).
        /// </summary>
        internal void CompleteTransaction()
        {
            _currentContext = null;
        }
    }

    /// <summary>
    /// In-memory implementation of IDbTransaction for testing.
    /// </summary>
    internal class InMemoryDbTransaction : IRelayDbTransaction
    {
        private readonly List<string> _changeLog;
        private bool _isDisposed;

        public InMemoryDbTransaction(List<string> changeLog)
        {
            _changeLog = changeLog;
        }

        public IDbConnection? Connection => null;
        public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                _changeLog.Add("Transaction.Dispose");
                _isDisposed = true;
            }
            await Task.CompletedTask;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemoryDbTransaction));
            }

            _changeLog.Add("Transaction.Commit");
            await Task.Yield();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemoryDbTransaction));
            }

            _changeLog.Add("Transaction.Rollback");
            await Task.Yield();
        }

        public void Commit()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemoryDbTransaction));
            }
            _changeLog.Add("Transaction.Commit");
        }

        public void Rollback()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemoryDbTransaction));
            }
            _changeLog.Add("Transaction.Rollback");
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _changeLog.Add("Transaction.Dispose");
                _isDisposed = true;
            }
        }
    }

    /// <summary>
    /// In-memory implementation of ITransactionContext for testing.
    /// </summary>
    internal class InMemoryTransactionContext : ITransactionContext
    {
        private readonly Dictionary<string, InMemorySavepoint> _savepoints = new();
        private readonly InMemoryUnitOfWork _unitOfWork;

        public InMemoryTransactionContext(
            IsolationLevel isolationLevel,
            bool isReadOnly,
            IRelayDbTransaction transaction,
            InMemoryUnitOfWork unitOfWork)
        {
            TransactionId = Guid.NewGuid().ToString();
            IsolationLevel = isolationLevel;
            IsReadOnly = isReadOnly;
            CurrentTransaction = transaction;
            StartedAt = DateTime.UtcNow;
            _unitOfWork = unitOfWork;
        }

        public string TransactionId { get; }
        public int NestingLevel { get; internal set; }
        public IsolationLevel IsolationLevel { get; }
        public bool IsReadOnly { get; }
        public DateTime StartedAt { get; }
        public IRelayDbTransaction? CurrentTransaction { get; }

        public async Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Savepoint name cannot be null or empty", nameof(name));
            }

            if (_savepoints.ContainsKey(name))
            {
                throw new SavepointAlreadyExistsException(name);
            }

            await Task.Yield();

            var savepoint = new InMemorySavepoint(name, this);
            _savepoints[name] = savepoint;

            return savepoint;
        }

        public async Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Savepoint name cannot be null or empty", nameof(name));
            }

            if (!_savepoints.ContainsKey(name))
            {
                throw new SavepointNotFoundException(name);
            }

            await Task.Yield();

            // Remove all savepoints created after this one
            var savepointsToRemove = _savepoints
                .Where(kvp => kvp.Value.CreatedAt > _savepoints[name].CreatedAt)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sp in savepointsToRemove)
            {
                _savepoints.Remove(sp);
            }
        }

        internal void RemoveSavepoint(string name)
        {
            _savepoints.Remove(name);
        }
    }

    /// <summary>
    /// In-memory implementation of ISavepoint for testing.
    /// </summary>
    internal class InMemorySavepoint : ISavepoint
    {
        private readonly InMemoryTransactionContext _context;
        private bool _isDisposed;

        public InMemorySavepoint(string name, InMemoryTransactionContext context)
        {
            Name = name;
            CreatedAt = DateTime.UtcNow;
            _context = context;
        }

        public string Name { get; }
        public DateTime CreatedAt { get; }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemorySavepoint));
            }

            await _context.RollbackToSavepointAsync(Name, cancellationToken);
        }

        public async Task ReleaseAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(InMemorySavepoint));
            }

            await Task.Yield();
            _context.RemoveSavepoint(Name);
        }

        public async ValueTask DisposeAsync()
        {
            if (!_isDisposed)
            {
                await ReleaseAsync();
                _isDisposed = true;
            }
        }
    }
}
