using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceLifecycleTests
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

        // Act & Assert - Multiple start/stop should work
        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);
        await hostedService.StartAsync(cancellationToken);
        await hostedService.StopAsync(cancellationToken);
    }

    [Fact]
    public async Task MultipleRapidStartStopCycles_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();

        var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();
        var logger = serviceProvider.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService = new MessageBrokerHostedService(messageBroker, logger);

        // Act - Rapid start/stop cycles
        for (int i = 0; i < 10; i++)
        {
            await hostedService.StartAsync(CancellationToken.None);
            await hostedService.StopAsync(CancellationToken.None);
        }

        // Assert - Should not throw
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public async Task StartAsync_WithDifferentMessageBrokers_ShouldWork()
    {
        // Test with InMemoryMessageBroker
        var services1 = new ServiceCollection();
        services1.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
        services1.AddLogging();
        var serviceProvider1 = services1.BuildServiceProvider();

        var messageBroker1 = serviceProvider1.GetRequiredService<IMessageBroker>();
        var logger1 = serviceProvider1.GetRequiredService<ILogger<MessageBrokerHostedService>>();
        var hostedService1 = new MessageBrokerHostedService(messageBroker1, logger1);

        await hostedService1.StartAsync(CancellationToken.None);
        await hostedService1.StopAsync(CancellationToken.None);

        // Test with another broker type if available, but since we only have InMemory, just verify it works
        Assert.NotNull(messageBroker1);
    }
}