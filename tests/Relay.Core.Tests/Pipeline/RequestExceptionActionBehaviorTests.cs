using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for RequestExceptionActionBehavior pipeline behavior.
    /// </summary>
    public class RequestExceptionActionBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RequestExceptionActionBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Return_Result_When_No_Exception_Occurs()
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            var loggerMock = new Mock<ILogger<RequestExceptionActionBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
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

            // Verify no exception actions were executed
            serviceProviderMock.Verify(x => x.GetService(It.IsAny<Type>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Execute_Actions_For_Exception_Type_And_Rethrow()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            var actionMock = new Mock<IRequestExceptionAction<TestRequest, InvalidOperationException>>();
            var actions = new[] { actionMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, InvalidOperationException>>)))
                .Returns(actions);
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionActionBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);

            actionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Exception occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Executed 1 exception action")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Execute_Actions_For_Base_Exception_Types()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            var specificActionMock = new Mock<IRequestExceptionAction<TestRequest, InvalidOperationException>>();
            var baseActionMock = new Mock<IRequestExceptionAction<TestRequest, Exception>>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, InvalidOperationException>>)))
                .Returns(new[] { specificActionMock.Object });
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, Exception>>)))
                .Returns(new[] { baseActionMock.Object });

            var loggerMock = new Mock<ILogger<RequestExceptionActionBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            specificActionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);
            baseActionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Continue_Executing_Actions_When_One_Throws_Exception()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            var failingActionMock = new Mock<IRequestExceptionAction<TestRequest, InvalidOperationException>>();
            failingActionMock
                .Setup(v => v.ExecuteAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Action failed"));

            var succeedingActionMock = new Mock<IRequestExceptionAction<TestRequest, InvalidOperationException>>();

            var actions = new[] { failingActionMock.Object, succeedingActionMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, InvalidOperationException>>)))
                .Returns(actions);
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionActionBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            failingActionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);
            succeedingActionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Exception action")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Work_Without_Logger()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            var actionMock = new Mock<IRequestExceptionAction<TestRequest, InvalidOperationException>>();
            var actions = new[] { actionMock.Object };

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, InvalidOperationException>>)))
                .Returns(actions);
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionAction<TestRequest, Exception>>)))
                .Returns(Array.Empty<object>());

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            actionMock.Verify(v => v.ExecuteAsync(request, expectedException, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Empty_Actions_Collection()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Test exception");

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionActionBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionActionBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => behavior.HandleAsync(request, next, cancellationToken).AsTask());

            Assert.Equal(expectedException, exception);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Executed 0 exception action")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        // Test request class
        public class TestRequest : IRequest<string> { }
    }
}
