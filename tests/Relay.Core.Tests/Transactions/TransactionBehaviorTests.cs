using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Contracts.Infrastructure; // Corrected
using Relay.Core.Contracts.Pipeline;       // Corrected
using Relay.Core.Contracts.Requests;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionBehaviorTests
    {
        #region Test Models & Mocks

        private record TransactionalCommand : IRequest, ITransactionalRequest;

        private record NonTransactionalQuery : IRequest;

        private class MockDbTransaction : IDbTransaction
        {
            private readonly List<string> _callLog;

            public MockDbTransaction(List<string> callLog)
            {
                _callLog = callLog;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                _callLog.Add(nameof(CommitAsync));
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _callLog.Add(nameof(RollbackAsync));
                return Task.CompletedTask;
            }
        }

        private class MockUnitOfWork : IUnitOfWork
        {
            public readonly List<string> CallLog = new();
            public bool ShouldThrowOnSave { get; set; }

            public Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            {
                CallLog.Add(nameof(BeginTransactionAsync));
                return Task.FromResult<IDbTransaction>(new MockDbTransaction(CallLog));
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                CallLog.Add(nameof(SaveChangesAsync));
                if (ShouldThrowOnSave)
                {
                    throw new InvalidOperationException("Save failed");
                }
                return Task.FromResult(1);
            }
        }

        #endregion

        [Fact]
        public async Task Handle_Should_CallNext_And_DoNothing_When_Request_Is_Not_Transactional()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var logger = new NullLogger<TransactionBehavior<NonTransactionalQuery, Unit>>();
            var behavior = new TransactionBehavior<NonTransactionalQuery, Unit>(unitOfWork, logger);
            var request = new NonTransactionalQuery();
            var handlerCalled = false;
            RequestHandlerDelegate<Unit> next = () =>
            {
                handlerCalled = true;
                return ValueTask.FromResult(Unit.Value);
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(handlerCalled);
            Assert.Empty(unitOfWork.CallLog);
        }

        [Fact]
        public async Task Handle_Should_Execute_Full_Transaction_Lifecycle_On_Success()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var logger = new NullLogger<TransactionBehavior<TransactionalCommand, Unit>>();
            var behavior = new TransactionBehavior<TransactionalCommand, Unit>(unitOfWork, logger);
            var request = new TransactionalCommand();
            var handlerCalled = false;
            RequestHandlerDelegate<Unit> next = () =>
            {
                handlerCalled = true;
                unitOfWork.CallLog.Add("Handler");
                return ValueTask.FromResult(Unit.Value);
            };

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(handlerCalled);
            Assert.Equal(new[]
            {
                nameof(IUnitOfWork.BeginTransactionAsync),
                "Handler",
                nameof(IUnitOfWork.SaveChangesAsync),
                nameof(IDbTransaction.CommitAsync)
            }, unitOfWork.CallLog);
        }

        [Fact]
        public async Task Handle_Should_Rollback_Transaction_On_Handler_Failure()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var logger = new NullLogger<TransactionBehavior<TransactionalCommand, Unit>>();
            var behavior = new TransactionBehavior<TransactionalCommand, Unit>(unitOfWork, logger);
            var request = new TransactionalCommand();
            RequestHandlerDelegate<Unit> next = () => throw new InvalidOperationException("Handler failed");

            // Act
            var act = async () => await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal(new[]
            {
                nameof(IUnitOfWork.BeginTransactionAsync),
                nameof(IDbTransaction.RollbackAsync)
            }, unitOfWork.CallLog);
        }

        [Fact]
        public async Task Handle_Should_Rollback_Transaction_On_SaveChanges_Failure()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork { ShouldThrowOnSave = true };
            var logger = new NullLogger<TransactionBehavior<TransactionalCommand, Unit>>();
            var behavior = new TransactionBehavior<TransactionalCommand, Unit>(unitOfWork, logger);
            var request = new TransactionalCommand();
            RequestHandlerDelegate<Unit> next = () =>
            {
                unitOfWork.CallLog.Add("Handler");
                return ValueTask.FromResult(Unit.Value);
            };

            // Act
            var act = async () => await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(act);
            Assert.Equal(new[]
            {
                nameof(IUnitOfWork.BeginTransactionAsync),
                "Handler",
                nameof(IUnitOfWork.SaveChangesAsync),
                nameof(IDbTransaction.RollbackAsync)
            }, unitOfWork.CallLog);
        }
    }
}
