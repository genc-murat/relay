using System;
using System.Threading;
using System.Threading.Tasks;
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
    public class UnitOfWorkBehaviorTests
    {
        #region Test Models

        public record CreateUserCommand(string Name, string Email)
            : IRequest<User>, ITransactionalRequest<User>;

        public record GetUserQuery(int Id) : IRequest<User>;

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        // Mock Unit of Work
        public class MockUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCallCount { get; private set; }
            public int LastEntriesWritten { get; private set; } = 5;
            public bool ShouldThrowOnSave { get; set; }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                SaveChangesCallCount++;

                if (ShouldThrowOnSave)
                    throw new InvalidOperationException("Save failed");

                return Task.FromResult(LastEntriesWritten);
            }
        }

        // Test handler delegate
        private class TestHandlerDelegate
        {
            public bool WasCalled { get; private set; }
            public bool ShouldThrow { get; set; }

            public async ValueTask<User> Execute()
            {
                WasCalled = true;

                if (ShouldThrow)
                    throw new InvalidOperationException("Handler failed");

                await Task.CompletedTask;
                return new User { Id = 1, Name = "John", Email = "john@example.com" };
            }
        }

        #endregion

        #region UnitOfWorkBehavior Tests

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Save_Changes_After_Handler_Success()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var behavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork);
            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            var result = await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            unitOfWork.SaveChangesCallCount.Should().Be(1);
            result.Should().NotBeNull();
            result.Name.Should().Be("John");
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Not_Save_On_Handler_Exception()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var behavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork);
            var request = new CreateUserCommand("John", "john@example.com");
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
            unitOfWork.SaveChangesCallCount.Should().Be(0, "SaveChanges should not be called when handler throws");
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Skip_If_UnitOfWork_Is_Null()
        {
            // Arrange
            var behavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork: null);
            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            var result = await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Propagate_SaveChanges_Exception()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork { ShouldThrowOnSave = true };
            var behavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork);
            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await behavior.HandleAsync(
                    request,
                    handlerDelegate.Execute,
                    CancellationToken.None);
            });

            handlerDelegate.WasCalled.Should().BeTrue();
            unitOfWork.SaveChangesCallCount.Should().Be(1);
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Save_For_All_Requests_By_Default()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var behavior = new UnitOfWorkBehavior<GetUserQuery, User>(
                unitOfWork,
                options: null, // Default: save for all requests
                logger: null);

            var request = new GetUserQuery(1);
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            unitOfWork.SaveChangesCallCount.Should().Be(1, "Should save for all requests by default");
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Skip_Non_Transactional_When_Configured()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var options = Options.Create(new UnitOfWorkOptions
            {
                SaveOnlyForTransactionalRequests = true
            });

            var behavior = new UnitOfWorkBehavior<GetUserQuery, User>(unitOfWork, options);
            var request = new GetUserQuery(1);
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            unitOfWork.SaveChangesCallCount.Should().Be(0, "Should skip non-transactional requests when configured");
        }

        [Fact]
        public async Task UnitOfWorkBehavior_Should_Save_Transactional_When_Configured()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var options = Options.Create(new UnitOfWorkOptions
            {
                SaveOnlyForTransactionalRequests = true
            });

            var behavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork, options);
            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate();

            // Act
            await behavior.HandleAsync(
                request,
                handlerDelegate.Execute,
                CancellationToken.None);

            // Assert
            unitOfWork.SaveChangesCallCount.Should().Be(1, "Should save transactional requests when configured");
        }

        #endregion

        #region DI Registration Tests

        [Fact]
        public void AddRelayUnitOfWork_Should_Register_UnitOfWorkBehavior()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayUnitOfWork();
            var provider = services.BuildServiceProvider();

            // Assert
            var behaviors = provider.GetServices<IPipelineBehavior<CreateUserCommand, User>>();
            services.Should().NotBeNull();
        }

        [Fact]
        public void AddRelayUnitOfWork_Should_Configure_Options()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayUnitOfWork(saveOnlyForTransactionalRequests: true);
            var provider = services.BuildServiceProvider();
            var options = provider.GetService<IOptions<UnitOfWorkOptions>>();

            // Assert
            options.Should().NotBeNull();
            options!.Value.SaveOnlyForTransactionalRequests.Should().BeTrue();
        }

        [Fact]
        public void AddRelayUnitOfWork_Should_Use_Default_Options()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddRelayUnitOfWork();
            var provider = services.BuildServiceProvider();
            var options = provider.GetService<IOptions<UnitOfWorkOptions>>();

            // Assert
            options.Should().NotBeNull();
            options!.Value.SaveOnlyForTransactionalRequests.Should().BeFalse();
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task Integration_Transaction_And_UnitOfWork_Should_Work_Together()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var transactionBehavior = new TransactionBehavior<CreateUserCommand, User>();
            var unitOfWorkBehavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork);

            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate();

            // Act - UnitOfWork wraps Transaction (inner to outer: handler -> transaction -> unitofwork)
            var result = await unitOfWorkBehavior.HandleAsync(
                request,
                async () => await transactionBehavior.HandleAsync(
                    request,
                    handlerDelegate.Execute,
                    CancellationToken.None),
                CancellationToken.None);

            // Assert
            handlerDelegate.WasCalled.Should().BeTrue();
            unitOfWork.SaveChangesCallCount.Should().Be(1);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task Integration_Transaction_Rollback_Should_Prevent_SaveChanges()
        {
            // Arrange
            var unitOfWork = new MockUnitOfWork();
            var transactionBehavior = new TransactionBehavior<CreateUserCommand, User>();
            var unitOfWorkBehavior = new UnitOfWorkBehavior<CreateUserCommand, User>(unitOfWork);

            var request = new CreateUserCommand("John", "john@example.com");
            var handlerDelegate = new TestHandlerDelegate { ShouldThrow = true };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await unitOfWorkBehavior.HandleAsync(
                    request,
                    async () => await transactionBehavior.HandleAsync(
                        request,
                        handlerDelegate.Execute,
                        CancellationToken.None),
                    CancellationToken.None);
            });

            // Transaction should rollback, so SaveChanges should not be called
            unitOfWork.SaveChangesCallCount.Should().Be(0);
        }

        #endregion
    }
}
