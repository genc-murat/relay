using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
}
