using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Behaviors;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for ServiceFactoryLoggingBehavior pipeline behavior.
    /// </summary>
    public class ServiceFactoryLoggingBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceFactory_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ServiceFactoryLoggingBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Log_Request_Handling_When_Logger_Is_Available()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>>();

            // Create a service factory that returns the logger when requested
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>))
                    return loggerMock.Object;
                return null;
            };

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            // Verify logging calls
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Handling request of type TestRequest")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Successfully handled request of type TestRequest")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Log_When_Logger_Is_Not_Available()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>>();

            // Create a service factory that returns null for logger
            ServiceFactory serviceFactory = type => null;

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal("response", result);

            // Verify no logging calls
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Log_Error_When_Exception_Occurs_And_Logger_Is_Available()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>>();
            var expectedException = new InvalidOperationException("Test exception");

            // Create a service factory that returns the logger when requested
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>))
                    return loggerMock.Object;
                return null;
            };

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                throw expectedException;
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);

            // Verify error logging
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error handling request of type TestRequest")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Log_Error_When_Exception_Occurs_And_Logger_Is_Not_Available()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>>();
            var expectedException = new InvalidOperationException("Test exception");

            // Create a service factory that returns null for logger
            ServiceFactory serviceFactory = type => null;

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                throw expectedException;
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);

            // Verify no logging calls
            loggerMock.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Always_Call_Next_Delegate()
        {
            // Arrange
            // Create a service factory that returns null for all services
            ServiceFactory serviceFactory = type => null;

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var nextCalled = false;
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                nextCalled = true;
                return new ValueTask<string>("response");
            };

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.True(nextCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Pass_CancellationToken_To_Next_Delegate()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>>();

            // Create a service factory that returns the logger when requested
            ServiceFactory serviceFactory = type =>
            {
                if (type == typeof(ILogger<ServiceFactoryLoggingBehavior<TestRequest, string>>))
                    return loggerMock.Object;
                return null;
            };

            var behavior = new ServiceFactoryLoggingBehavior<TestRequest, string>(serviceFactory);
            var request = new TestRequest();
            var cancellationToken = new CancellationToken(true);
            var receivedToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () =>
            {
                receivedToken = cancellationToken;
                return new ValueTask<string>("response");
            };

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal(cancellationToken, receivedToken);
        }

        // Test request class
        public class TestRequest : IRequest<string> { }
    }
}
