using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Pipeline
{
    /// <summary>
    /// Tests for RequestExceptionHandlerBehavior pipeline behavior.
    /// </summary>
    public class RequestExceptionHandlerBehaviorTests
    {
        [Fact]
        public void Constructor_Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RequestExceptionHandlerBehavior<TestRequest, string>(null!));
        }

        [Fact]
        public async Task HandleAsync_Should_Return_Response_When_No_Exception_Occurs()
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => new ValueTask<string>("success");

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal("success", result);
            loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Exception_When_Handler_Returns_Handled_Result()
        {
            // Arrange
            var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
            handlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ExceptionHandlerResult<string>.Handle("handled"));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(new[] { handlerMock.Object });
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal("handled", result);
            handlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
            loggerMock.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Rethrow_Exception_When_Handler_Returns_Unhandled_Result()
        {
            // Arrange
            var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
            handlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ExceptionHandlerResult<string>.Unhandled());

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(new[] { handlerMock.Object });
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Test exception");

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await behavior.HandleAsync(request, next, cancellationToken));

            Assert.Same(expectedException, actualException);
            handlerMock.Verify(x => x.HandleAsync(request, expectedException, cancellationToken), Times.Once);
            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_Should_Rethrow_Exception_When_No_Handlers_Are_Available()
        {
            // Arrange
            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;
            var expectedException = new InvalidOperationException("Test exception");

            RequestHandlerDelegate<string> next = () => throw expectedException;

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await behavior.HandleAsync(request, next, cancellationToken));

            Assert.Same(expectedException, actualException);
            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        }

        [Fact]
        public async Task HandleAsync_Should_Handle_Exception_With_Base_Exception_Handler()
        {
            // Arrange
            var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, Exception>>();
            handlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ExceptionHandlerResult<string>.Handle("handled"));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(new[] { handlerMock.Object });

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal("handled", result);
            handlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Continue_To_Next_Handler_When_Handler_Throws_Exception()
        {
            // Arrange
            var failingHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
            failingHandlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Handler failed"));

            var successHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
            successHandlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ExceptionHandlerResult<string>.Handle("handled"));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(new[] { failingHandlerMock.Object, successHandlerMock.Object });
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test exception");

            // Act
            var result = await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            Assert.Equal("handled", result);
            failingHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
            successHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_Should_Log_Handler_Invocation_At_Trace_Level()
        {
            // Arrange
            var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
            handlerMock
                .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ExceptionHandlerResult<string>.Handle("handled"));

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
                .Returns(new[] { handlerMock.Object });
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
                .Returns(Array.Empty<object>());
            serviceProviderMock
                .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
                .Returns(Array.Empty<object>());

            var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

            var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
            var request = new TestRequest();
            var cancellationToken = CancellationToken.None;

            RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test exception");

            // Act
            await behavior.HandleAsync(request, next, cancellationToken);

            // Assert
            loggerMock.Verify(x => x.Log(LogLevel.Trace, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        // Test request class
        public class TestRequest { }
    }
}