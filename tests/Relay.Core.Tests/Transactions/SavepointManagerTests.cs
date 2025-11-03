using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions
{
    public class SavepointManagerTests
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

        private class MockDbTransaction : IRelayDbTransaction
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
        public void Constructor_Should_Throw_When_Transaction_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SavepointManager(null!));
        }

        [Fact]
        public void SavepointCount_Should_Return_Zero_Initially()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var count = manager.SavepointCount;

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Create_Savepoint_With_Valid_Name()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var savepoint = await manager.CreateSavepointAsync("sp1");

            // Assert
            Assert.NotNull(savepoint);
            Assert.Equal("sp1", savepoint.Name);
            Assert.Equal(1, manager.SavepointCount);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_Name_Is_Null()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync(null!));
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_Name_Is_Empty()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync(""));
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_Name_Is_Whitespace()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync("   "));
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_Name_Contains_Invalid_Characters()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync("sp-1"));
            
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync("sp.1"));
            
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.CreateSavepointAsync("sp;1"));
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Allow_Valid_Characters()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var sp1 = await manager.CreateSavepointAsync("sp_1");
            var sp2 = await manager.CreateSavepointAsync("SavePoint123");
            var sp3 = await manager.CreateSavepointAsync("_underscore");

            // Assert
            Assert.NotNull(sp1);
            Assert.NotNull(sp2);
            Assert.NotNull(sp3);
            Assert.Equal(3, manager.SavepointCount);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Throw_When_Savepoint_Already_Exists()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SavepointAlreadyExistsException>(
                async () => await manager.CreateSavepointAsync("sp1"));
            
            Assert.Contains("sp1", exception.Message);
        }

        [Fact]
        public async Task CreateSavepointAsync_Should_Be_Case_Insensitive()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("SP1");

            // Act & Assert
            await Assert.ThrowsAsync<SavepointAlreadyExistsException>(
                async () => await manager.CreateSavepointAsync("sp1"));
        }

        [Fact]
        public async Task RollbackToSavepointAsync_Should_Rollback_To_Existing_Savepoint()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            var savepoint = await manager.CreateSavepointAsync("sp1");

            // Act
            await manager.RollbackToSavepointAsync("sp1");

            // Assert - No exception thrown
            Assert.Equal(1, manager.SavepointCount);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_Should_Throw_When_Savepoint_Not_Found()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<SavepointNotFoundException>(
                async () => await manager.RollbackToSavepointAsync("nonexistent"));
            
            Assert.Contains("nonexistent", exception.Message);
        }

        [Fact]
        public async Task RollbackToSavepointAsync_Should_Throw_When_Name_Is_Null()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await manager.RollbackToSavepointAsync(null!));
        }

        [Fact]
        public async Task GetSavepoint_Should_Return_Existing_Savepoint()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            var created = await manager.CreateSavepointAsync("sp1");

            // Act
            var retrieved = manager.GetSavepoint("sp1");

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("sp1", retrieved.Name);
        }

        [Fact]
        public void GetSavepoint_Should_Return_Null_When_Not_Found()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var savepoint = manager.GetSavepoint("nonexistent");

            // Assert
            Assert.Null(savepoint);
        }

        [Fact]
        public void GetSavepoint_Should_Return_Null_When_Name_Is_Null()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var savepoint = manager.GetSavepoint(null!);

            // Assert
            Assert.Null(savepoint);
        }

        [Fact]
        public async Task HasSavepoint_Should_Return_True_When_Savepoint_Exists()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");

            // Act
            var exists = manager.HasSavepoint("sp1");

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public void HasSavepoint_Should_Return_False_When_Savepoint_Does_Not_Exist()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var exists = manager.HasSavepoint("nonexistent");

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public void HasSavepoint_Should_Return_False_When_Name_Is_Null()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var exists = manager.HasSavepoint(null!);

            // Assert
            Assert.False(exists);
        }

        [Fact]
        public async Task ReleaseSavepointAsync_Should_Remove_Savepoint()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");

            // Act
            await manager.ReleaseSavepointAsync("sp1");

            // Assert
            Assert.Equal(0, manager.SavepointCount);
            Assert.False(manager.HasSavepoint("sp1"));
        }

        [Fact]
        public async Task ReleaseSavepointAsync_Should_Throw_When_Savepoint_Not_Found()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<SavepointNotFoundException>(
                async () => await manager.ReleaseSavepointAsync("nonexistent"));
        }

        [Fact]
        public async Task CleanupAsync_Should_Remove_All_Savepoints()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");
            await manager.CreateSavepointAsync("sp2");
            await manager.CreateSavepointAsync("sp3");

            // Act
            await manager.CleanupAsync();

            // Assert
            Assert.Equal(0, manager.SavepointCount);
        }

        [Fact]
        public async Task DisposeAsync_Should_Cleanup_All_Savepoints()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");
            await manager.CreateSavepointAsync("sp2");

            // Act
            await manager.DisposeAsync();

            // Assert
            Assert.Equal(0, manager.SavepointCount);
        }

        [Fact]
        public async Task Operations_Should_Throw_After_Disposal()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.DisposeAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await manager.CreateSavepointAsync("sp1"));
            
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await manager.RollbackToSavepointAsync("sp1"));
            
            Assert.Throws<ObjectDisposedException>(
                () => manager.GetSavepoint("sp1"));
        }

        [Fact]
        public async Task Multiple_Savepoints_Should_Be_Tracked_Independently()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);

            // Act
            var sp1 = await manager.CreateSavepointAsync("sp1");
            var sp2 = await manager.CreateSavepointAsync("sp2");
            var sp3 = await manager.CreateSavepointAsync("sp3");

            // Assert
            Assert.Equal(3, manager.SavepointCount);
            Assert.True(manager.HasSavepoint("sp1"));
            Assert.True(manager.HasSavepoint("sp2"));
            Assert.True(manager.HasSavepoint("sp3"));
        }

        [Fact]
        public async Task Savepoint_Names_Should_Be_Unique()
        {
            // Arrange
            var transaction = new MockDbTransaction();
            var manager = new SavepointManager(transaction);
            await manager.CreateSavepointAsync("sp1");

            // Act & Assert
            await Assert.ThrowsAsync<SavepointAlreadyExistsException>(
                async () => await manager.CreateSavepointAsync("sp1"));
        }
    }
}
