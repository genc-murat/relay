using System;
using System.Data;
using Relay.Core.Transactions;
using Xunit;
using IDbTransaction = Relay.Core.Transactions.IDbTransaction;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionContextAccessorTests
    {
        private class MockDbTransaction : IDbTransaction
        {
            public IDbConnection? Connection => null;
            public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

            public System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return System.Threading.Tasks.Task.CompletedTask;
            }

            public void Commit() { }
            public void Rollback() { }
            public void Dispose() { }

            public System.Threading.Tasks.ValueTask DisposeAsync()
            {
                return System.Threading.Tasks.ValueTask.CompletedTask;
            }
        }

        [Fact]
        public void Current_Should_Start_With_Null()
        {
            TransactionContextAccessor.Clear();

            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void Current_Should_Store_Context()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            TransactionContextAccessor.Current = context;

            Assert.NotNull(TransactionContextAccessor.Current);
            Assert.Same(context, TransactionContextAccessor.Current);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void Current_Should_Allow_Context_Replacement()
        {
            TransactionContextAccessor.Clear();
            var transaction1 = new MockDbTransaction();
            var context1 = new TransactionContext(transaction1, IsolationLevel.ReadCommitted);
            var transaction2 = new MockDbTransaction();
            var context2 = new TransactionContext(transaction2, IsolationLevel.Serializable);

            TransactionContextAccessor.Current = context1;
            TransactionContextAccessor.Current = context2;

            Assert.Same(context2, TransactionContextAccessor.Current);
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void Clear_Should_Remove_Context()
        {
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            TransactionContextAccessor.Current = context;
            TransactionContextAccessor.Clear();

            Assert.Null(TransactionContextAccessor.Current);
        }

        [Fact]
        public void HasActiveContext_Should_Return_True_When_Context_Exists()
        {
            TransactionContextAccessor.Clear();
            var transaction = new MockDbTransaction();
            var context = new TransactionContext(transaction, IsolationLevel.ReadCommitted);

            TransactionContextAccessor.Current = context;

            Assert.True(TransactionContextAccessor.HasActiveContext());
            
            TransactionContextAccessor.Clear();
        }

        [Fact]
        public void HasActiveContext_Should_Return_False_When_No_Context()
        {
            TransactionContextAccessor.Clear();

            Assert.False(TransactionContextAccessor.HasActiveContext());
        }
    }
}
