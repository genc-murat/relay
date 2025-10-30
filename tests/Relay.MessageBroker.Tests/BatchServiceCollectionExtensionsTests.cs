using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Batch;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BatchServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBrokerBatching_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BatchServiceCollectionExtensions.AddMessageBrokerBatching(null!));
    }

    [Fact]
    public void AddMessageBrokerBatching_ShouldRegisterBatchOptionsAndDecorateBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();

        // Act
        services.AddMessageBrokerBatching();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<BatchOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
        Assert.True(options.Value.Enabled); // Default enabled

        var broker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(broker);
        Assert.IsType<BatchMessageBrokerDecorator>(broker);
    }

    [Fact]
    public void AddMessageBrokerBatching_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();

        // Act
        services.AddMessageBrokerBatching(options =>
        {
            options.Enabled = true;
            options.MaxBatchSize = 500;
            options.FlushInterval = TimeSpan.FromSeconds(2);
            options.EnableCompression = false;
            options.PartialRetry = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<BatchOptions>>();
        Assert.NotNull(options);
        Assert.True(options!.Value.Enabled);
        Assert.Equal(500, options.Value.MaxBatchSize);
        Assert.Equal(TimeSpan.FromSeconds(2), options.Value.FlushInterval);
        Assert.False(options.Value.EnableCompression);
        Assert.False(options.Value.PartialRetry);
    }

    [Fact]
    public void AddMessageBrokerBatching_WithBatchOptionsObject_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();

        var batchOptions = new BatchOptions
        {
            Enabled = true,
            MaxBatchSize = 250,
            FlushInterval = TimeSpan.FromMilliseconds(500),
            EnableCompression = true,
            PartialRetry = true
        };

        // Act
        services.AddMessageBrokerBatching(batchOptions);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<BatchOptions>>();
        Assert.NotNull(options);
        Assert.True(options!.Value.Enabled);
        Assert.Equal(250, options.Value.MaxBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.Value.FlushInterval);
        Assert.True(options.Value.EnableCompression);
        Assert.True(options.Value.PartialRetry);

        var broker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(broker);
        Assert.IsType<BatchMessageBrokerDecorator>(broker);
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