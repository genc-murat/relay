using Relay.Core.Transactions;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IDbTransaction = Relay.Core.Transactions.IDbTransaction;

namespace Relay.Core.Tests.Transactions;

public class ReadOnlyTransactionEnforcerTests
{
    private class MockDbTransaction : IDbTransaction
    {
        public IDbConnection? Connection => null;
        public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Commit() { }
        public void Rollback() { }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class MockUnitOfWork : IUnitOfWork
    {
        public bool IsReadOnly { get; set; }
        public ITransactionContext? CurrentTransactionContext { get; set; }

        public Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void ThrowIfReadOnly_Should_Throw_When_ReadOnly()
    {
        var unitOfWork = new MockUnitOfWork 
        { 
            IsReadOnly = true,
            CurrentTransactionContext = new TransactionContext(new MockDbTransaction(), IsolationLevel.ReadCommitted)
        };

        Assert.Throws<ReadOnlyTransactionViolationException>(() => 
            ReadOnlyTransactionEnforcer.ThrowIfReadOnly(unitOfWork));
    }

    [Fact]
    public void ThrowIfReadOnly_Should_Not_Throw_When_Not_ReadOnly()
    {
        var unitOfWork = new MockUnitOfWork { IsReadOnly = false };

        ReadOnlyTransactionEnforcer.ThrowIfReadOnly(unitOfWork);
    }

    [Fact]
    public void ThrowIfReadOnly_With_RequestType_Should_Throw_When_ReadOnly()
    {
        var unitOfWork = new MockUnitOfWork 
        { 
            IsReadOnly = true,
            CurrentTransactionContext = new TransactionContext(new MockDbTransaction(), IsolationLevel.ReadCommitted)
        };

        Assert.Throws<ReadOnlyTransactionViolationException>(() => 
            ReadOnlyTransactionEnforcer.ThrowIfReadOnly(unitOfWork, "TestRequest"));
    }

    [Fact]
    public void ThrowIfReadOnly_With_RequestType_Should_Throw_ArgumentNullException_When_UnitOfWork_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => 
            ReadOnlyTransactionEnforcer.ThrowIfReadOnly(null!, "TestRequest"));
    }

    [Fact]
    public void IsReadOnly_Should_Return_True_When_ReadOnly()
    {
        var unitOfWork = new MockUnitOfWork { IsReadOnly = true };

        Assert.True(ReadOnlyTransactionEnforcer.IsReadOnly(unitOfWork));
    }

    [Fact]
    public void IsReadOnly_Should_Return_False_When_Not_ReadOnly()
    {
        var unitOfWork = new MockUnitOfWork { IsReadOnly = false };

        Assert.False(ReadOnlyTransactionEnforcer.IsReadOnly(unitOfWork));
    }

    [Fact]
    public void IsReadOnly_Should_Throw_ArgumentNullException_When_UnitOfWork_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => 
            ReadOnlyTransactionEnforcer.IsReadOnly(null!));
    }

    [Fact]
    public void ConfigureReadOnlyTransaction_Should_Not_Throw()
    {
        var transaction = new MockDbTransaction();

        ReadOnlyTransactionEnforcer.ConfigureReadOnlyTransaction(transaction);
    }

    [Fact]
    public void ConfigureReadOnlyTransaction_Should_Throw_ArgumentNullException_When_Transaction_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => 
            ReadOnlyTransactionEnforcer.ConfigureReadOnlyTransaction(null!));
    }

    [Fact]
    public void ThrowIfReadOnly_Should_Throw_ArgumentNullException_When_UnitOfWork_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => 
            ReadOnlyTransactionEnforcer.ThrowIfReadOnly(null!));
    }
}
