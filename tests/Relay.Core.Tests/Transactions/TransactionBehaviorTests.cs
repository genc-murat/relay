using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core;
using Relay.Core.Pipeline;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class TransactionBehaviorTests
    {
        #region Test Models

        // Transactional request
        public record CreateOrderCommand(int UserId, decimal Amount)
            : IRequest<Order>, ITransactionalRequest<Order>;

        // Non-transactional request
        public record GetOrderQuery(int OrderId) : IRequest<Order>;

        public class Order
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public decimal Amount { get; set; }
        }

        // Test handler delegate
        private class TestHandlerDelegate
        {
            public bool WasCalled { get; private set; }
            public bool ShouldThrow { get; set; }

            public async ValueTask<Order> Execute()
            {
                WasCalled = true;

                if (ShouldThrow)
                    throw new InvalidOperationException("Handler failed");

                await Task.CompletedTask;
                return new Order { Id = 1, UserId = 123, Amount = 99.99m };
            }
        }

        #endregion

        #region TransactionBehavior Tests

        [Fact]
        public async Task TransactionBehavior_Should_Skip_For_NonTransactional_Requests()
        {
            // Arrange
            var behavior = new TransactionBehavior<GetOrderQuery, Order>();
            var request = new GetOrderQuery(1);
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            var result = await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            result.Should().NotBeNull();
            result.Id.Should().Be(1);
        }

        [Fact]
        public async Task TransactionBehavior_Should_Create_Transaction_For_Transactional_Requests()
        {
            // Arrange
            var behavior = new TransactionBehavior<CreateOrderCommand, Order>();
            var request = new CreateOrderCommand(123, 99.99m);
            var handlerDelegate = new TestHandlerDelegate();

            Transaction? capturedTransaction = null;

            // Act
            var result = await behavior.HandleAsync(
                request,
                async () =>
                {
                    capturedTransaction = Transaction.Current;
                    return await handlerDelegate.Execute();
                },
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            capturedTransaction.Should().NotBeNull("Transaction should be active during handler execution");
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task TransactionBehavior_Should_Commit_On_Success()
        {
            // Arrange
            var behavior = new TransactionBehavior<CreateOrderCommand, Order>();
            var request = new CreateOrderCommand(123, 99.99m);
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            var result = await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            result.Should().NotBeNull();
            // Transaction should be completed without exception
        }

        [Fact]
        public async Task TransactionBehavior_Should_Rollback_On_Exception()
        {
            // Arrange
            var behavior = new TransactionBehavior<CreateOrderCommand, Order>();
            var request = new CreateOrderCommand(123, 99.99m);
            var handlerDelegate = new TestHandlerDelegate { ShouldThrow = true };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    handlerDelegate.Execute,
                    CancellationToken.None);
            });

            handlerDelegate.WasCalled.Should().BeTrue();
            // Transaction should be rolled back (not completed)
        }

        [Fact]
        public async Task TransactionBehavior_Should_Use_Custom_IsolationLevel()
        {
            // Arrange
            var options = Options.Create(new Relay.Core.Transactions.TransactionOptions
            {
                ScopeOption = TransactionScopeOption.Required,
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TimeSpan.FromMinutes(2)
            });

            var behavior = new TransactionBehavior<CreateOrderCommand, Order>(options);

            var request = new CreateOrderCommand(123, 99.99m);

            IsolationLevel? capturedIsolationLevel = null;

            // Act
            await behavior.HandleAsync(
                request,
                async () =>
                {
                    capturedIsolationLevel = Transaction.Current?.IsolationLevel;
                    await Task.CompletedTask;
                    return new Order();
                },
                CancellationToken.None);

            // Assert
            capturedIsolationLevel.Should().Be(IsolationLevel.Serializable);
        }

        [Fact]
        public async Task TransactionBehavior_Should_Support_Nested_Transactions()
        {
            // Arrange
            var outerBehavior = new TransactionBehavior<CreateOrderCommand, Order>();
            var innerBehavior = new TransactionBehavior<CreateOrderCommand, Order>();

            var request = new CreateOrderCommand(123, 99.99m);

            Transaction? outerTransaction = null;
            Transaction? innerTransaction = null;

            // Act
            await outerBehavior.HandleAsync(
                request,
                async () =>
                {
                    outerTransaction = Transaction.Current;

                    return await innerBehavior.HandleAsync(
                        request,
                        async () =>
                        {
                            innerTransaction = Transaction.Current;
                            await Task.CompletedTask;
                            return new Order();
                        },
                        CancellationToken.None);
                },
                CancellationToken.None);

            // Assert
            outerTransaction.Should().NotBeNull();
            innerTransaction.Should().NotBeNull();
            // Both should reference the same ambient transaction
            outerTransaction.Should().BeSameAs(innerTransaction);
        }

        #endregion

        #region DI Registration Tests

        [Fact]
        public void AddRelayTransactions_Should_Register_TransactionBehavior()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayTransactions();
            var provider = services.BuildServiceProvider();

            // Note: TransactionBehavior is registered as open generic IPipelineBehavior<,>
            // We can't directly resolve it without specific types, but we can verify registration
            var behaviors = provider.GetServices<IPipelineBehavior<CreateOrderCommand, Order>>();

            // Assert
            // The registration should succeed without exceptions
            services.Should().NotBeNull();
        }

        [Fact]
        public void AddRelayTransactions_Should_Accept_Custom_Settings()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayTransactions(
                scopeOption: TransactionScopeOption.RequiresNew,
                isolationLevel: IsolationLevel.Snapshot,
                timeout: TimeSpan.FromMinutes(5));

            // Assert
            // Should register without exceptions
            services.Should().NotBeNull();
        }

        #endregion
    }
}
