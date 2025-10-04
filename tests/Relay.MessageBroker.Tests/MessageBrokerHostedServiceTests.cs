using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceTests
{
    [Fact]
    public async Task StartAsync_ShouldStartMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);
        var cancellationToken = CancellationToken.None;
        
        // Act
        await hostedService.StartAsync(cancellationToken);
        
        // Assert - Should not throw
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public async Task StopAsync_ShouldStopMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);
        var cancellationToken = CancellationToken.None;
        
        await hostedService.StartAsync(cancellationToken);
        
        // Act
        await hostedService.StopAsync(cancellationToken);
        
        // Assert - Should not throw
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public async Task StartAsync_WithCancellationToken_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert - Should handle cancellation gracefully
        await hostedService.StartAsync(cts.Token);
    }

    [Fact]
    public async Task StopAsync_WithCancellationToken_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);
        var cts = new CancellationTokenSource();
        
        await hostedService.StartAsync(CancellationToken.None);
        cts.Cancel();
        
        // Act & Assert - Should handle cancellation gracefully
        await hostedService.StopAsync(cts.Token);
    }

    [Fact]
    public async Task MultipleStartStop_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);
        var cancellationToken = CancellationToken.None;
        
        // Act
        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);
        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);
        
        // Assert - Should not throw
        Assert.NotNull(messageBroker);
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
    public async Task StartAsync_ShouldPassCancellationTokenToBroker()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();
        
        // Act
        await hostedService.StartAsync(cts.Token);
        
        // Assert
        mockBroker.Verify(b => b.StartAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldPassCancellationTokenToBroker()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);
        var cts = new CancellationTokenSource();

        await hostedService.StartAsync(CancellationToken.None);

        // Act
        await hostedService.StopAsync(cts.Token);
        
        // Assert
        mockBroker.Verify(b => b.StopAsync(cts.Token), Times.Once);
    }
}