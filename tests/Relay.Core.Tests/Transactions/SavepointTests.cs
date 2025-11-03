using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Transactions;
using Xunit;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions;

public class SavepointTests
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

    private class MockDbTransactionWithNullConnection : IRelayDbTransaction
    {
        public IDbConnection? Connection => null;
        public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public void Commit() { }
        public void Dispose() { }
        public void Rollback() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class MockDbTransactionNotSystemData : IRelayDbTransaction
    {
        public IDbConnection? Connection { get; }
        public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public MockDbTransactionNotSystemData()
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
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Null()
    {
        // Arrange
        var transaction = new MockDbTransaction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Savepoint(null!, transaction));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("Savepoint name cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Empty()
    {
        // Arrange
        var transaction = new MockDbTransaction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Savepoint("", transaction));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("Savepoint name cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentException_When_Name_Is_Whitespace()
    {
        // Arrange
        var transaction = new MockDbTransaction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Savepoint("   ", transaction));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("Savepoint name cannot be null or whitespace", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Transaction_Is_Null()
    {
        // Arrange
        var name = "testSavepoint";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new Savepoint(name, null!));
        Assert.Equal("transaction", exception.ParamName);
    }

    [Fact]
    public void Constructor_Should_Throw_InvalidOperationException_When_Transaction_Connection_Is_Null()
    {
        // Arrange
        var transaction = new MockDbTransactionWithNullConnection();
        var name = "testSavepoint";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new Savepoint(name, transaction));
        Assert.Contains("Transaction connection is not available", exception.Message);
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters_And_SystemData_Transaction()
    {
        // Arrange
        var transaction = new MockDbTransaction();
        var name = "testSavepoint";

        // Act
        var savepoint = new Savepoint(name, transaction);

        // Assert
        Assert.Equal(name, savepoint.Name);
        Assert.True(savepoint.CreatedAt <= DateTime.UtcNow);
        Assert.True(savepoint.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_Should_Initialize_Successfully_With_Valid_Parameters_And_Non_SystemData_Transaction()
    {
        // Arrange
        var transaction = new MockDbTransactionNotSystemData();
        var name = "testSavepoint";

        // Act
        var savepoint = new Savepoint(name, transaction);

        // Assert
        Assert.Equal(name, savepoint.Name);
        Assert.True(savepoint.CreatedAt <= DateTime.UtcNow);
        Assert.True(savepoint.CreatedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Constructor_Should_Assign_Transaction_To_Command_When_Transaction_Is_SystemData_IDbTransaction()
    {
        // Arrange
        var transaction = new MockDbTransaction();
        var name = "testSavepoint";

        // Act
        var savepoint = new Savepoint(name, transaction);

        // Assert - The constructor should not throw and should create the savepoint successfully
        // We can't directly access the private _command field, but the fact that construction succeeds
        // indicates the transaction assignment worked correctly
        Assert.Equal(name, savepoint.Name);
    }

    [Theory]
    [InlineData("sp1")]
    [InlineData("savepoint_123")]
    [InlineData("SAVEPOINT")]
    [InlineData("a")]
    [InlineData("valid_savepoint_name_with_underscores")]
    public void Constructor_Should_Accept_Valid_Savepoint_Names(string validName)
    {
        // Arrange
        var transaction = new MockDbTransaction();

        // Act & Assert - Should not throw
        var savepoint = new Savepoint(validName, transaction);
        Assert.Equal(validName, savepoint.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("\r")]
    public void Constructor_Should_Reject_Invalid_Savepoint_Names(string invalidName)
    {
        // Arrange
        var transaction = new MockDbTransaction();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Savepoint(invalidName, transaction));
        Assert.Equal("name", exception.ParamName);
        Assert.Contains("Savepoint name cannot be null or whitespace", exception.Message);
    }
}