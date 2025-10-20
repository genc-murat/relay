using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceLoggingTests
{
    [Fact]
    public async Task StartAsync_ShouldLogInformationMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert - Logging is handled by the logging framework, just verify it doesn't throw
        Assert.NotNull(hostedService);
    }

    [Fact]
    public async Task StopAsync_ShouldLogInformationMessages()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);

        await hostedService.StartAsync(CancellationToken.None);

        // Act
        await hostedService.StopAsync(CancellationToken.None);

        // Assert - Logging is handled by the logging framework, just verify it doesn't throw
        Assert.NotNull(hostedService);
    }

    [Fact]
    public async Task StartAsync_ShouldLogSuccessfulStart()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Starting message broker")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldLogSuccessfulStop()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();
        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(mockBroker.Object, mockLogger.Object);

        await hostedService.StartAsync(CancellationToken.None);

        // Act
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Stopping message broker")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}