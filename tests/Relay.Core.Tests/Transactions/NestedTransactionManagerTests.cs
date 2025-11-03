using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Transactions;
using Xunit;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions
{
    public class NestedTransactionManagerTests
    {
        private class MockDbTransaction : IRelayDbTransaction
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

        [Fact]
        public void IsTransactionActive_Should_Return_False_When_No_Transaction()
        {
            TransactionContextAccessor.Clear();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.IsTransactionActive();

            Assert.False(result);
        }

        [Fact]
        public void IsTransactionActive_Should_Return_True_When_Transaction_Exists()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            TransactionContextAccessor.Current = context;

            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.IsTransactionActive();

            Assert.True(result);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void GetCurrentContext_Should_Return_Null_When_No_Transaction()
        {
            TransactionContextAccessor.Clear();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.GetCurrentContext();

            Assert.Null(result);
        }

        [Fact]
        public void GetCurrentContext_Should_Return_Context_When_Transaction_Exists()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            TransactionContextAccessor.Current = context;

            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.GetCurrentContext();

            Assert.NotNull(result);
            Assert.Same(context, result);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void EnterNestedTransaction_Should_Increment_Nesting_Level()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            TransactionContextAccessor.Current = context;

            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.EnterNestedTransaction("TestRequest");

            Assert.Equal(1, result.NestingLevel);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void EnterNestedTransaction_Should_Throw_When_No_Transaction()
        {
            TransactionContextAccessor.Clear();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<InvalidOperationException>(() => 
                manager.EnterNestedTransaction("TestRequest"));
        }

        [Fact]
        public void ExitNestedTransaction_Should_Decrement_Nesting_Level()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            TransactionContextAccessor.Current = context;

            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());
            manager.EnterNestedTransaction("TestRequest");
            manager.EnterNestedTransaction("TestRequest");

            var result = manager.ExitNestedTransaction("TestRequest");

            Assert.False(result); // Not outermost yet
            Assert.Equal(1, context.NestingLevel);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void ExitNestedTransaction_Should_Return_True_When_Reaching_Outermost()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            TransactionContextAccessor.Current = context;

            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());
            manager.EnterNestedTransaction("TestRequest");

            var result = manager.ExitNestedTransaction("TestRequest");

            Assert.True(result); // Reached outermost
            Assert.Equal(0, context.NestingLevel);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void ExitNestedTransaction_Should_Throw_When_No_Transaction()
        {
            TransactionContextAccessor.Clear();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<InvalidOperationException>(() => 
                manager.ExitNestedTransaction("TestRequest"));
        }

        [Fact]
        public void ShouldCommitTransaction_Should_Return_True_For_Outermost()
        {
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.ShouldCommitTransaction(context);

            Assert.True(result);
        }

        [Fact]
        public void ShouldCommitTransaction_Should_Return_False_For_Nested()
        {
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            context.IncrementNestingLevel();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.ShouldCommitTransaction(context);

            Assert.False(result);
        }

        [Fact]
        public void ShouldRollbackTransaction_Should_Return_True_For_Outermost()
        {
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.ShouldRollbackTransaction(context);

            Assert.True(result);
        }

        [Fact]
        public void ShouldRollbackTransaction_Should_Return_False_For_Nested()
        {
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            context.IncrementNestingLevel();
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            var result = manager.ShouldRollbackTransaction(context);

            Assert.False(result);
        }

        [Fact]
        public void ValidateNestedTransactionConfiguration_Should_Throw_On_Isolation_Level_Mismatch()
        {
            var transaction = new MockDbTransaction();
            var outerContext = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var nestedConfig = new TransactionConfiguration(
                IsolationLevel.Serializable,
                TimeSpan.FromSeconds(30),
                isReadOnly: false);
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<NestedTransactionException>(() => 
                manager.ValidateNestedTransactionConfiguration(outerContext, nestedConfig, "TestRequest"));
        }

        [Fact]
        public void ValidateNestedTransactionConfiguration_Should_Throw_On_ReadOnly_Mismatch()
        {
            var transaction = new MockDbTransaction();
            var outerContext = new TransactionContext(transaction, IsolationLevel.ReadCommitted, isReadOnly: true);
            var nestedConfig = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromSeconds(30),
                isReadOnly: false); // Trying to write in read-only transaction
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<NestedTransactionException>(() => 
                manager.ValidateNestedTransactionConfiguration(outerContext, nestedConfig, "TestRequest"));
        }

        [Fact]
        public void ValidateNestedTransactionConfiguration_Should_Allow_ReadOnly_Nested_In_ReadWrite()
        {
            var transaction = new MockDbTransaction();
            var outerContext = new TransactionContext(transaction, IsolationLevel.ReadCommitted, isReadOnly: false);
            var nestedConfig = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromSeconds(30),
                isReadOnly: true); // Read-only nested in read-write is OK
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            // Should not throw
            manager.ValidateNestedTransactionConfiguration(outerContext, nestedConfig, "TestRequest");
        }

        [Fact]
        public void ValidateNestedTransactionConfiguration_Should_Allow_Matching_Configuration()
        {
            var transaction = new MockDbTransaction();
            var outerContext = new TransactionContext(transaction, IsolationLevel.ReadCommitted);
            var nestedConfig = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromSeconds(30),
                isReadOnly: false);
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            // Should not throw
            manager.ValidateNestedTransactionConfiguration(outerContext, nestedConfig, "TestRequest");
        }

        [Fact]
        public void ShouldCommitTransaction_Should_Throw_ArgumentNullException_For_Null_Context()
        {
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<ArgumentNullException>(() => 
                manager.ShouldCommitTransaction(null!));
        }

        [Fact]
        public void ShouldRollbackTransaction_Should_Throw_ArgumentNullException_For_Null_Context()
        {
            var manager = new NestedTransactionManager(new NullLogger<NestedTransactionManager>());

            Assert.Throws<ArgumentNullException>(() => 
                manager.ShouldRollbackTransaction(null!));
        }
    }
}
