using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceCancellationTests
{
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
    public async Task StartAsync_WithPreCancelledToken_ShouldHandleGracefully()
    {
        // Arrange
        var messageBrokerMock = new Mock<IMessageBroker>();
        var loggerMock = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBrokerMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should handle cancellation gracefully without throwing
        await hostedService.StartAsync(cts.Token);

        // Verify StartAsync was called with cancelled token
        messageBrokerMock.Verify(x => x.StartAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WithPreCancelledToken_ShouldHandleGracefully()
    {
        // Arrange
        var messageBrokerMock = new Mock<IMessageBroker>();
        var loggerMock = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBrokerMock.Object, loggerMock.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - Should handle cancellation gracefully without throwing
        await hostedService.StopAsync(cts.Token);

        // Verify StopAsync was called with cancelled token
        messageBrokerMock.Verify(x => x.StopAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithAlreadyCancelledToken_ShouldHandleGracefully()
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

        // Act & Assert - Should not throw, just handle cancellation
        await hostedService.StartAsync(cts.Token);
    }

    [Fact]
    public async Task StopAsync_WithAlreadyCancelledToken_ShouldHandleGracefully()
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

        // Act & Assert - Should not throw, just handle cancellation
        await hostedService.StopAsync(cts.Token);
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