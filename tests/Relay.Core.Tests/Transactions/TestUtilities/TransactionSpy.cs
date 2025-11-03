using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions.TestUtilities
{
    /// <summary>
    /// A spy implementation of IUnitOfWork that records all transaction operations for verification in tests.
    /// </summary>
    public class TransactionSpy : IUnitOfWork
    {
        private readonly List<TransactionOperation> _operations = new();
        private InMemoryTransactionContext? _currentContext;
        private bool _isReadOnly;

        /// <summary>
        /// Gets all recorded operations.
        /// </summary>
        public IReadOnlyList<TransactionOperation> Operations => _operations.AsReadOnly();

        /// <summary>
        /// Gets or sets whether SaveChangesAsync should throw an exception.
        /// </summary>
        public bool ShouldThrowOnSave { get; set; }

        /// <summary>
        /// Gets or sets the exception to throw when ShouldThrowOnSave is true.
        /// </summary>
        public Exception? SaveException { get; set; }

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
                RecordOperation(TransactionOperationType.SetReadOnly, new { IsReadOnly = value });
            }
        }

        /// <summary>
        /// Begins a new transaction with the specified isolation level.
        /// </summary>
        public async Task<IRelayDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            RecordOperation(TransactionOperationType.BeginTransaction, new { IsolationLevel = isolationLevel });

            await Task.Yield();

            var transaction = new SpyDbTransaction(this);
            _currentContext = new InMemoryTransactionContext(isolationLevel, _isReadOnly, transaction, null!);

            return transaction;
        }

        /// <summary>
        /// Saves all changes made in this unit of work.
        /// </summary>
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            RecordOperation(TransactionOperationType.SaveChanges);

            if (_isReadOnly)
            {
                throw new ReadOnlyTransactionViolationException("Cannot save changes in a read-only transaction");
            }

            if (ShouldThrowOnSave)
            {
                throw SaveException ?? new InvalidOperationException("SaveChanges failed");
            }

            await Task.Yield();
            return 1;
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

            RecordOperation(TransactionOperationType.CreateSavepoint, new { Name = name });
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

            RecordOperation(TransactionOperationType.RollbackToSavepoint, new { Name = name });
            await _currentContext.RollbackToSavepointAsync(name, cancellationToken);
        }

        #region Verification Methods

        /// <summary>
        /// Verifies that a transaction was begun with the specified isolation level.
        /// </summary>
        public void VerifyTransactionBegan(IsolationLevel? expectedLevel = null)
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.BeginTransaction);
            Assert.NotNull(operation);

            if (expectedLevel.HasValue)
            {
                var actualLevel = GetOperationData<IsolationLevel>(operation, "IsolationLevel");
                Assert.Equal(expectedLevel.Value, actualLevel);
            }
        }

        /// <summary>
        /// Verifies that a transaction was not begun.
        /// </summary>
        public void VerifyTransactionNotBegan()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.BeginTransaction);
            Assert.Null(operation);
        }

        /// <summary>
        /// Verifies that the transaction was committed.
        /// </summary>
        public void VerifyTransactionCommitted()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.Commit);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies that the transaction was not committed.
        /// </summary>
        public void VerifyTransactionNotCommitted()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.Commit);
            Assert.Null(operation);
        }

        /// <summary>
        /// Verifies that the transaction was rolled back.
        /// </summary>
        public void VerifyTransactionRolledBack()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.Rollback);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies that the transaction was not rolled back.
        /// </summary>
        public void VerifyTransactionNotRolledBack()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.Rollback);
            Assert.Null(operation);
        }

        /// <summary>
        /// Verifies that SaveChanges was called.
        /// </summary>
        public void VerifySaveChangesCalled()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.SaveChanges);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies that SaveChanges was not called.
        /// </summary>
        public void VerifySaveChangesNotCalled()
        {
            var operation = _operations.FirstOrDefault(o => o.Type == TransactionOperationType.SaveChanges);
            Assert.Null(operation);
        }

        /// <summary>
        /// Verifies that a savepoint was created with the specified name.
        /// </summary>
        public void VerifySavepointCreated(string name)
        {
            var operation = _operations.FirstOrDefault(o =>
                o.Type == TransactionOperationType.CreateSavepoint &&
                GetOperationData<string>(o, "Name") == name);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies that a savepoint was not created.
        /// </summary>
        public void VerifySavepointNotCreated(string name)
        {
            var operation = _operations.FirstOrDefault(o =>
                o.Type == TransactionOperationType.CreateSavepoint &&
                GetOperationData<string>(o, "Name") == name);
            Assert.Null(operation);
        }

        /// <summary>
        /// Verifies that a rollback to savepoint was performed.
        /// </summary>
        public void VerifyRollbackToSavepoint(string name)
        {
            var operation = _operations.FirstOrDefault(o =>
                o.Type == TransactionOperationType.RollbackToSavepoint &&
                GetOperationData<string>(o, "Name") == name);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies that IsReadOnly was set to the specified value.
        /// </summary>
        public void VerifyReadOnlySet(bool expectedValue)
        {
            var operation = _operations.FirstOrDefault(o =>
                o.Type == TransactionOperationType.SetReadOnly &&
                GetOperationData<bool>(o, "IsReadOnly") == expectedValue);
            Assert.NotNull(operation);
        }

        /// <summary>
        /// Verifies the order of operations.
        /// </summary>
        public void VerifyOperationOrder(params TransactionOperationType[] expectedOrder)
        {
            var actualOrder = _operations.Select(o => o.Type).ToArray();
            Assert.Equal(expectedOrder, actualOrder);
        }

        /// <summary>
        /// Verifies that a specific number of operations of a given type were recorded.
        /// </summary>
        public void VerifyOperationCount(TransactionOperationType type, int expectedCount)
        {
            var actualCount = _operations.Count(o => o.Type == type);
            Assert.Equal(expectedCount, actualCount);
        }

        /// <summary>
        /// Gets the number of operations of a specific type.
        /// </summary>
        public int GetOperationCount(TransactionOperationType type)
        {
            return _operations.Count(o => o.Type == type);
        }

        /// <summary>
        /// Clears all recorded operations.
        /// </summary>
        public void ClearOperations()
        {
            _operations.Clear();
        }

        /// <summary>
        /// Resets the spy to its initial state.
        /// </summary>
        public void Reset()
        {
            _operations.Clear();
            _currentContext = null;
            _isReadOnly = false;
            ShouldThrowOnSave = false;
            SaveException = null;
        }

        #endregion

        #region Helper Methods

        private void RecordOperation(TransactionOperationType type, object? data = null)
        {
            _operations.Add(new TransactionOperation
            {
                Type = type,
                Timestamp = DateTime.UtcNow,
                Data = data
            });
        }

        private T GetOperationData<T>(TransactionOperation operation, string propertyName)
        {
            if (operation.Data == null)
            {
                throw new InvalidOperationException($"Operation data is null");
            }

            var property = operation.Data.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property '{propertyName}' not found in operation data");
            }

            var value = property.GetValue(operation.Data);
            if (value is T typedValue)
            {
                return typedValue;
            }

            throw new InvalidOperationException($"Property '{propertyName}' is not of type {typeof(T).Name}");
        }

        #endregion

        /// <summary>
        /// Spy implementation of IDbTransaction.
        /// </summary>
        private class SpyDbTransaction : IRelayDbTransaction
        {
            private readonly TransactionSpy _spy;
            private bool _isDisposed;

            public SpyDbTransaction(TransactionSpy spy)
            {
                _spy = spy;
            }

            public async ValueTask DisposeAsync()
            {
                if (!_isDisposed)
                {
                    _spy.RecordOperation(TransactionOperationType.Dispose);
                    _isDisposed = true;
                }
                await Task.CompletedTask;
            }

            public async Task CommitAsync(CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SpyDbTransaction));
                }

                _spy.RecordOperation(TransactionOperationType.Commit);
                await Task.Yield();
            }

            public async Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SpyDbTransaction));
                }

                _spy.RecordOperation(TransactionOperationType.Rollback);
                await Task.Yield();
            }

            public void Commit()
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SpyDbTransaction));
                }
                _spy.RecordOperation(TransactionOperationType.Commit);
            }

            public void Rollback()
            {
                if (_isDisposed)
                {
                    throw new ObjectDisposedException(nameof(SpyDbTransaction));
                }
                _spy.RecordOperation(TransactionOperationType.Rollback);
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _spy.RecordOperation(TransactionOperationType.Dispose);
                    _isDisposed = true;
                }
            }

            public IDbConnection? Connection => null;
            public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
        }
    }

    /// <summary>
    /// Represents a recorded transaction operation.
    /// </summary>
    public class TransactionOperation
    {
        /// <summary>
        /// Gets or sets the type of operation.
        /// </summary>
        public TransactionOperationType Type { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the operation was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets additional data associated with the operation.
        /// </summary>
        public object? Data { get; set; }
    }

    /// <summary>
    /// Defines the types of transaction operations that can be recorded.
    /// </summary>
    public enum TransactionOperationType
    {
        BeginTransaction,
        Commit,
        Rollback,
        SaveChanges,
        CreateSavepoint,
        RollbackToSavepoint,
        ReleaseSavepoint,
        SetReadOnly,
        Dispose
    }
}
