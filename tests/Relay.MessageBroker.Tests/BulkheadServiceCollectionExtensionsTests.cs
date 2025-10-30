using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Bulkhead;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BulkheadServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBrokerBulkhead_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BulkheadServiceCollectionExtensions.AddMessageBrokerBulkhead(null!));
    }

    [Fact]
    public void AddMessageBrokerBulkhead_ShouldRegisterBulkheadServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerBulkhead();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var bulkhead = serviceProvider.GetService<IBulkhead>();
        Assert.NotNull(bulkhead);
        Assert.IsType<Relay.MessageBroker.Bulkhead.Bulkhead>(bulkhead);
    }

    [Fact]
    public void AddMessageBrokerBulkhead_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerBulkhead(options =>
        {
            options.Enabled = true;
            options.MaxConcurrentOperations = 50;
            options.MaxQueuedOperations = 200;
            options.AcquisitionTimeout = TimeSpan.FromMinutes(1);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var bulkhead = serviceProvider.GetService<IBulkhead>();
        Assert.NotNull(bulkhead);
        var metrics = bulkhead.GetMetrics();
        Assert.Equal(50, metrics.MaxConcurrentOperations);
        Assert.Equal(200, metrics.MaxQueuedOperations);
    }

    [Fact]
    public void AddMessageBrokerBulkhead_ShouldRegisterBulkheadService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerBulkhead();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var bulkhead = serviceProvider.GetService<IBulkhead>();
        Assert.NotNull(bulkhead);
    }

    [Fact]
    public void DecorateMessageBrokerWithBulkhead_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BulkheadServiceCollectionExtensions.DecorateMessageBrokerWithBulkhead(null!));
    }

    [Fact]
    public void DecorateMessageBrokerWithBulkhead_ShouldDecorateMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();
        services.AddMessageBrokerBulkhead();

        // Act
        services.DecorateMessageBrokerWithBulkhead();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var broker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(broker);
        Assert.IsType<BulkheadMessageBrokerDecorator>(broker);
    }

    [Fact]
    public void DecorateMessageBrokerWithBulkhead_ShouldCreateSeparateBulkheadInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();
        services.AddMessageBrokerBulkhead();

        // Act
        services.DecorateMessageBrokerWithBulkhead();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var decorator = (BulkheadMessageBrokerDecorator)serviceProvider.GetService<IMessageBroker>()!;

        var publishMetrics = decorator.GetPublishMetrics();
        var subscribeMetrics = decorator.GetSubscribeMetrics();

        // They should be separate instances (different names)
        Assert.NotEqual(publishMetrics, subscribeMetrics);
    }

    private class TestMessageBroker : IMessageBroker
    {
        public ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask SubscribeAsync<TMessage>(Func<TMessage, MessageContext, CancellationToken, ValueTask> handler, SubscriptionOptions? options = null, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask StartAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}