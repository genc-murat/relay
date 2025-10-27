using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

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

    #region Additional Coverage Tests

    [Fact]
    public async Task HandleAsync_Should_Execute_First_Handling_Handler_When_Multiple_Handlers_For_Same_Exception_Type()
    {
        // Arrange
        var firstHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        firstHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("first-handled"));

        var secondHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        secondHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("second-handled")); // This shouldn't be called

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
            .Returns(new[] { firstHandlerMock.Object, secondHandlerMock.Object });
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
        Assert.Equal("first-handled", result);
        // Verify first handler was called
        firstHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
        // Verify second handler was NOT called
        secondHandlerMock.Verify(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Try_Next_Handler_When_First_Handler_Returns_Unhandled_For_Same_Exception_Type()
    {
        // Arrange
        var firstHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        firstHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Unhandled());

        var secondHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        secondHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("second-handled"));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
            .Returns(new[] { firstHandlerMock.Object, secondHandlerMock.Object });
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
        Assert.Equal("second-handled", result);
        // Verify both handlers were called
        firstHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
        secondHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Derived_Exception_Using_Base_Exception_Handler()
    {
        // Arrange
        var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, Exception>>();
        handlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<ArgumentException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("derived-handled"));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, ArgumentException>>)))
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

        RequestHandlerDelegate<string> next = () => throw new ArgumentException("Derived exception");

        // Act
        var result = await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        Assert.Equal("derived-handled", result);
        handlerMock.Verify(x => x.HandleAsync(request, It.IsAny<ArgumentException>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Exception_Hierarchy_In_Correct_Order()
    {
        // Arrange - Test inheritance chain: ArgumentException -> SystemException -> Exception
        var argumentHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, ArgumentException>>();
        argumentHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<ArgumentException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("argument-handled"));

        var systemHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, SystemException>>();
        systemHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<SystemException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("system-handled"));

        var exceptionHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, Exception>>();
        exceptionHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("exception-handled"));

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, ArgumentException>>)))
            .Returns(new[] { argumentHandlerMock.Object });
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, SystemException>>)))
            .Returns(new[] { systemHandlerMock.Object });
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, Exception>>)))
            .Returns(new[] { exceptionHandlerMock.Object });

        var loggerMock = new Mock<ILogger<RequestExceptionHandlerBehavior<TestRequest, string>>>();

        var behavior = new RequestExceptionHandlerBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => throw new ArgumentException("Test exception");

        // Act
        var result = await behavior.HandleAsync(request, next, cancellationToken);

        // Assert - Should use the most specific handler (ArgumentException)
        Assert.Equal("argument-handled", result);
        argumentHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<ArgumentException>(), cancellationToken), Times.Once);
        systemHandlerMock.Verify(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<SystemException>(), It.IsAny<CancellationToken>()), Times.Never);
        exceptionHandlerMock.Verify(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Exception_Handler_That_Throws_By_Logging_Error_And_Continuing()
    {
        // Arrange
        var failingHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        failingHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Handler failed"));

        var successHandlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        successHandlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("success"));

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
        Assert.Equal("success", result);
        // Verify both handlers were attempted
        failingHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
        successHandlerMock.Verify(x => x.HandleAsync(request, It.IsAny<InvalidOperationException>(), cancellationToken), Times.Once);
        // Verify error was logged for the failing handler
        loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Cancelled_Token_In_Exception_Handler()
    {
        // Arrange
        var handlerMock = new Mock<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>();
        handlerMock
            .Setup(x => x.HandleAsync(It.IsAny<TestRequest>(), It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, InvalidOperationException, CancellationToken>((_, _, ct) => 
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);
            })
            .ReturnsAsync(ExceptionHandlerResult<string>.Handle("should-not-be-reached"));

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
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before exception occurs

        RequestHandlerDelegate<string> next = () => throw new InvalidOperationException("Test exception");

        // Act & Assert - Should handle cancellation gracefully
        await Assert.ThrowsAsync<InvalidOperationException>(async () => 
            await behavior.HandleAsync(request, next, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Null_Handlers_In_Service_Collection()
    {
        // Arrange - Test when GetServices returns null or contains null items
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
            .Returns(new[] { (object)null! }); // Contains null handler
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

        // Act & Assert - Should skip null handlers and rethrow
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, cancellationToken));

        Assert.Same(expectedException, actualException);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Handler_Without_HandleAsync_Method()
    {
        // Arrange - Create a mock handler that doesn't have HandleAsync method
        var invalidHandler = new InvalidHandler(); // Class without HandleAsync method

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
            .Returns(new[] { invalidHandler });
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

        // Act & Assert - Should skip invalid handler and rethrow
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, cancellationToken));

        Assert.Same(expectedException, actualException);
    }



    [Fact]
    public async Task HandleAsync_Should_Handle_Handler_With_Non_ValueTask_Result()
    {
        // Arrange - Create a handler that returns Task instead of ValueTask
        var taskReturningHandler = new TaskReturningHandler();

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestExceptionHandler<TestRequest, string, InvalidOperationException>>)))
            .Returns(new[] { taskReturningHandler });
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

        // Act & Assert - Should handle Task results and rethrow if not handled
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, cancellationToken));

        Assert.Same(expectedException, actualException);
    }

    #endregion

    // Test request class
    public class TestRequest { }

    // Test helper classes for edge cases
    private class InvalidHandler
    {
        // No HandleAsync method
    }

    private class TaskReturningHandler : IRequestExceptionHandler<TestRequest, string, InvalidOperationException>
    {
        public async ValueTask<Relay.Core.Pipeline.Interfaces.ExceptionHandlerResult<string>> HandleAsync(TestRequest request, InvalidOperationException exception, CancellationToken cancellationToken)
        {
            await Task.Delay(1); // Simulate async work
            return Relay.Core.Pipeline.Interfaces.ExceptionHandlerResult<string>.Unhandled();
        }
    }
}