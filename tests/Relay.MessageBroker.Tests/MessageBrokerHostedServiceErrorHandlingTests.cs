using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceErrorHandlingTests
{
    [Fact]
    public async Task StartAsync_WhenMessageBrokerThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var messageBrokerMock = new Mock<IMessageBroker>();
        messageBrokerMock.Setup(x => x.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker failed to start"));

        var loggerMock = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBrokerMock.Object, loggerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await hostedService.StartAsync(CancellationToken.None));
        Assert.Equal("Broker failed to start", exception.Message);

        // Verify logging
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to start message broker")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenMessageBrokerThrows_ShouldLogError()
    {
        // Arrange
        var messageBrokerMock = new Mock<IMessageBroker>();
        messageBrokerMock.Setup(x => x.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Broker failed to stop"));

        var loggerMock = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBrokerMock.Object, loggerMock.Object);

        // Act - Should not throw, just log the error
        await hostedService.StopAsync(CancellationToken.None);

        // Verify logging
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to stop message broker")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenMessageBrokerThrows_ShouldRethrowAndLog()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var exception = new InvalidOperationException("Failed to start");
        mockBroker.Setup(b => b.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start message broker")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenMessageBrokerThrows_ShouldNotRethrowButLog()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var exception = new InvalidOperationException("Failed to stop");
        mockBroker.Setup(b => b.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        mockBroker.Setup(b => b.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        await hostedService.StartAsync(CancellationToken.None);

        // Act
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to stop message broker")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_WhenBrokerStartThrows_ShouldLogAndRethrow()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var exception = new InvalidOperationException("Start failed");
        mockBroker.Setup(b => b.StartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => hostedService.StartAsync(CancellationToken.None));

        Assert.Equal(exception, thrownException);

        // Verify logging happened
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to start message broker")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhenBrokerStopThrows_ShouldLogButNotRethrow()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var exception = new InvalidOperationException("Stop failed");
        mockBroker.Setup(b => b.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        mockBroker.Setup(b => b.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        await hostedService.StartAsync(CancellationToken.None);

        // Act - Should not throw
        await hostedService.StopAsync(CancellationToken.None);

        // Assert - Verify logging happened
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to stop message broker")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}