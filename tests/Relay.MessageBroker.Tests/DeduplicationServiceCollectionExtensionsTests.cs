using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker.Deduplication;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DeduplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageDeduplication_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeduplicationServiceCollectionExtensions.AddMessageDeduplication(null!));
    }

    [Fact]
    public void AddMessageDeduplication_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageDeduplication();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetService<IDeduplicationCache>();
        Assert.NotNull(cache);
        Assert.IsType<DeduplicationCache>(cache);
    }

    [Fact]
    public void AddMessageDeduplication_WithConfiguration_ShouldApplyConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageDeduplication(options =>
        {
            options.Enabled = true;
            options.Window = TimeSpan.FromMinutes(10);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetService<IDeduplicationCache>();
        Assert.NotNull(cache);
    }

    [Fact]
    public void DecorateWithDeduplication_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            DeduplicationServiceCollectionExtensions.DecorateWithDeduplication(null!));
    }

    [Fact]
    public void DecorateWithDeduplication_ShouldDecorateMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();
        services.AddMessageDeduplication();

        // Act
        services.DecorateWithDeduplication();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var broker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(broker);
        Assert.IsType<DeduplicationMessageBrokerDecorator>(broker);
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