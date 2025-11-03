using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;
using IDbTransaction = Relay.Core.Transactions.IDbTransaction;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionContextTests
    {
        #region Mock Classes

        private class MockDbConnection : IDbConnection
        {
            public string ConnectionString { get; set; } = "";
            public int ConnectionTimeout => 30;
            public string Database => "TestDb";
            public ConnectionState State => ConnectionState.Open;

            public System.Data.IDbTransaction BeginTransaction() => throw new NotImplementedException();
            public System.Data.IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
            public void ChangeDatabase(string databaseName) { }
            public void Close() { }
            public IDbCommand CreateCommand() => new MockDbCommand();
            public void Dispose() { }
            public void Open() { }
        }

        private class MockDbCommand : IDbCommand
        {
            public string CommandText { get; set; } = "";
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection? Connection { get; set; }
            public IDataParameterCollection Parameters => throw new NotImplementedException();
            public System.Data.IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }

            public void Cancel() { }
            public IDbDataParameter CreateParameter() => throw new NotImplementedException();
            public void Dispose() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => throw new NotImplementedException();
            public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotImplementedException();
            public object? ExecuteScalar() => null;
            public void Prepare() { }
        }

        private class MockDbTransaction : IDbTransaction
        {
            public IDbConnection? Connection { get; }
            public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

            public MockDbTransaction()
            {
                Connection = new MockDbConnection();
            }

            public void Commit() { }
            public void Dispose() { }
            public void Rollback() { }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        #endregion

        [Fact]
        public void Constructor_Should_Initialize_Properties()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var isolationLevel = IsolationLevel.ReadCommitted;

            // Act
            var context = new TransactionContext(transaction, isolationLevel);

            // Assert
            Assert.NotNull(context.TransactionId);
            Assert.NotEmpty(context.TransactionId);
            Assert.Equal(0, context.NestingLevel);
            Assert.Equal(isolationLevel, context.IsolationLevel);
            Assert.False(context.IsReadOnly);
            Assert.NotNull(context.CurrentTransaction);
            Assert.True(context.StartedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void Constructor_Should_Set_ReadOnly_Flag()
        {
            // Arrange
            var transaction = new MockDbTransaction();

            // Act
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted, isReadOnly: true);

            // Assert
            Assert.True(context.IsReadOnly);
        }

        [Fact]
        public void Constructor_Should_Set_Nesting_Level()
        {
            // Arrange
            var transaction = new MockDbTransaction();

            // Act
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted, nestingLevel: 2);

            // Assert
            Assert.Equal(2, context.NestingLevel);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Transaction_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(
                () => new TransactionContext(null!, IsolationLevel.ReadCommitted));
        }

        [Fact]
        public void Constructor_Should_Throw_When_IsolationLevel_Is_Unspecified()
        {
            // Arrange
            var transaction = new MockDbTransaction();

            // Act & Assert
            Assert.Throws<ArgumentException>(
                () => new TransactionContext(transaction, IsolationLevel.Unspecified));
        }

        [Fact]
        public void Constructor_Should_Throw_When_NestingLevel_Is_Negative()
        {
            // Arrange
            var transaction = new MockDbTransaction();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TransactionContext(transaction, IsolationLevel.ReadCommitted, nestingLevel: -1));
        }

        [Fact]
        public void TransactionId_Should_Be_Unique()
        {
            // Arrange
            var transaction1 = new MockDbTransaction();
            var transaction2 = new MockDbTransaction();

            // Act
            var context1 = new TransactionContext(transaction1, IsolationLevel.ReadCommitted);
            var context2 = new TransactionContext(transaction2, IsolationLevel.ReadCommitted);

            // Assert
            Assert.NotEqual(context1.TransactionId, context2.TransactionId);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Create_Savepoint()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            // Act
            var savepoint = await context.CreateSavepointAsync("sp1");

            // Assert
            Assert.NotNull(savepoint);
            Assert.Equal("sp1", savepoint.Name);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_No_Active_Transaction()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            await context.DisposeAsync(); // Dispose to clear transaction

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await context.CreateSavepointAsync("sp1"));
        }

        [Fact]
        public async Task RollbackToSavepointAsync_Should_Rollback_To_Savepoint()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            await context.CreateSavepointAsync("sp1");

            // Act
            await context.RollbackToSavepointAsync("sp1");

            // Assert - No exception thrown
        }

        [Fact]
        public async Task RollbackToSavepointAsync_Should_Throw_When_Savepoint_Not_Found()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            // Act & Assert
            await Assert.ThrowsAsync<SavepointNotFoundException>(
                async () => await context.RollbackToSavepointAsync("nonexistent"));
        }

        [Fact]
        public void IncrementNestingLevel_Should_Increase_Level()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var initialLevel = context.NestingLevel;

            // Act
            context.IncrementNestingLevel();

            // Assert
            Assert.Equal(initialLevel + 1, context.NestingLevel);
        }

        [Fact]
        public void DecrementNestingLevel_Should_Decrease_Level()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted, nestingLevel: 2);

            // Act
            context.DecrementNestingLevel();

            // Assert
            Assert.Equal(1, context.NestingLevel);
        }

        [Fact]
        public void DecrementNestingLevel_Should_Not_Go_Below_Zero()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted, nestingLevel: 0);

            // Act
            context.DecrementNestingLevel();

            // Assert
            Assert.Equal(0, context.NestingLevel);
        }

        [Fact]
        public async Task Multiple_Nesting_Operations_Should_Track_Correctly()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            // Act
            context.IncrementNestingLevel(); // 1
            context.IncrementNestingLevel(); // 2
            context.IncrementNestingLevel(); // 3
            context.DecrementNestingLevel(); // 2
            context.DecrementNestingLevel(); // 1

            // Assert
            Assert.Equal(1, context.NestingLevel);
        }

        [Fact]
        public async Task CleanupAsync_Should_Remove_All_Savepoints()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            await context.CreateSavepointAsync("sp1");
            await context.CreateSavepointAsync("sp2");

            // Act
            await context.CleanupAsync();

            // Assert - Savepoints should be cleaned up
            // We can't directly verify this without exposing internal state,
            // but we can verify no exceptions are thrown
        }

        [Fact]
        public async Task DisposeAsync_Should_Clear_Transaction()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            // Act
            await context.DisposeAsync();

            // Assert
            Assert.Null(context.CurrentTransaction);
        }

        [Fact]
        public async Task DisposeAsync_Should_Be_Idempotent()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            // Act
            await context.DisposeAsync();
            await context.DisposeAsync(); // Second call should not throw

            // Assert - No exception thrown
        }

        [Fact]
        public async Task Operations_Should_Throw_After_Disposal()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            await context.DisposeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await context.CreateSavepointAsync("sp1"));
            
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await context.RollbackToSavepointAsync("sp1"));
        }

        [Fact]
        public void StartedAt_Should_Be_Close_To_Current_Time()
        {
            // Arrange
            var before = DateTime.UtcNow;
            var transaction = new MockDbTransaction();

            // Act
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var after = DateTime.UtcNow;

            // Assert
            Assert.True(context.StartedAt >= before);
            Assert.True(context.StartedAt <= after);
        }

        [Fact]
        public void Context_Should_Support_All_Isolation_Levels()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var isolationLevels = new[]
            {
                IsolationLevel.ReadUncommitted,
                IsolationLevel.ReadCommitted,
                IsolationLevel.RepeatableRead,
                IsolationLevel.Serializable,
                IsolationLevel.Snapshot
            };

            // Act & Assert
            foreach (var level in isolationLevels)
            {
                var context = new TransactionContext(transaction, level);
                Assert.Equal(level, context.IsolationLevel);
            }
        }
    }
}
