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
/// Tests for RequestPostProcessorBehavior pipeline behavior.
/// </summary>
public class RequestPostProcessorBehaviorTests
{
    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_ServiceProvider_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RequestPostProcessorBehavior<TestRequest, string>(null!));
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Response_When_No_PostProcessors_Are_Registered()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(Array.Empty<IRequestPostProcessor<TestRequest, string>>());

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act
        var result = await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        Assert.Equal("response", result);
        loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Pass_CancellationToken_To_PostProcessors()
    {
        // Arrange
        var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        postProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = new CancellationToken(true);

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act
        await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        postProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Log_PostProcessor_Execution()
    {
        // Arrange
        var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        postProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act
        await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
        loggerMock.Verify(x => x.Log(LogLevel.Trace, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Exactly(2));
    }

    #region Additional Coverage Tests

    [Fact]
    public async Task HandleAsync_Should_Throw_Exception_When_PostProcessor_Fails()
    {
        // Arrange
        var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        var expectedException = new InvalidOperationException("Post-processor failed");
        postProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(expectedException);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, cancellationToken));

        Assert.Same(expectedException, actualException);
        loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), expectedException, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Multiple_PostProcessors_In_Order()
    {
        // Arrange
        var executionOrder = new List<string>();

        var firstPostProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        firstPostProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, string, CancellationToken>((_, _, _) => executionOrder.Add("first"))
            .Returns(new ValueTask());

        var secondPostProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        secondPostProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, string, CancellationToken>((_, _, _) => executionOrder.Add("second"))
            .Returns(new ValueTask());

        var thirdPostProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        thirdPostProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, string, CancellationToken>((_, _, _) => executionOrder.Add("third"))
            .Returns(new ValueTask());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { 
                firstPostProcessorMock.Object, 
                secondPostProcessorMock.Object, 
                thirdPostProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act
        await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal("first", executionOrder[0]);
        Assert.Equal("second", executionOrder[1]);
        Assert.Equal("third", executionOrder[2]);

        // Verify all post-processors were called with correct parameters
        firstPostProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
        secondPostProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
        thirdPostProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Continue_With_Next_PostProcessor_When_First_One_Fails_Then_Rethrow()
    {
        // Arrange
        var firstPostProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        firstPostProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new InvalidOperationException("First post-processor failed"));

        var secondPostProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        secondPostProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { 
                firstPostProcessorMock.Object, 
                secondPostProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act & Assert - Should throw the exception from the first post-processor
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, cancellationToken));

        // Verify first post-processor was called
        firstPostProcessorMock.Verify(x => x.ProcessAsync(request, "response", cancellationToken), Times.Once);
        // Verify second post-processor was NOT called because first one threw
        secondPostProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        // Verify error was logged
        loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Cancelled_Token_In_PostProcessor()
    {
        // Arrange
        var postProcessorMock = new Mock<IRequestPostProcessor<TestRequest, string>>();
        postProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<TestRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<TestRequest, string, CancellationToken>((_, _, ct) =>
            {
                if (ct.IsCancellationRequested)
                    throw new OperationCanceledException(ct);
            })
            .Returns(new ValueTask());

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(new IRequestPostProcessor<TestRequest, string>[] { postProcessorMock.Object });

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act & Assert - Should propagate the cancellation
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await behavior.HandleAsync(request, next, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Empty_PostProcessor_Collection_Gracefully()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<IRequestPostProcessor<TestRequest, string>>)))
            .Returns(Array.Empty<IRequestPostProcessor<TestRequest, string>>());

        var loggerMock = new Mock<ILogger<RequestPostProcessorBehavior<TestRequest, string>>>();

        var behavior = new RequestPostProcessorBehavior<TestRequest, string>(serviceProviderMock.Object, loggerMock.Object);
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        RequestHandlerDelegate<string> next = () => new ValueTask<string>("response");

        // Act
        var result = await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        Assert.Equal("response", result);
        // Should not log anything when there are no post-processors
        loggerMock.Verify(x => x.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }

    #endregion

    // Test request class
    public class TestRequest { }
}