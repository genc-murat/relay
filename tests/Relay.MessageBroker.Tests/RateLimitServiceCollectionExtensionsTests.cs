using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RateLimitServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBrokerRateLimit_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RateLimitServiceCollectionExtensions.AddMessageBrokerRateLimit(null!));
    }

    [Fact]
    public void AddMessageBrokerRateLimit_ShouldRegisterTokenBucketRateLimiterByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerRateLimit();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var rateLimiter = serviceProvider.GetService<IRateLimiter>();
        Assert.NotNull(rateLimiter);
        Assert.IsType<TokenBucketRateLimiter>(rateLimiter);
    }

    [Fact]
    public void AddMessageBrokerRateLimit_WithTokenBucketStrategy_ShouldRegisterTokenBucketRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerRateLimit(options =>
        {
            options.Strategy = RateLimitStrategy.TokenBucket;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var rateLimiter = serviceProvider.GetService<IRateLimiter>();
        Assert.NotNull(rateLimiter);
        Assert.IsType<TokenBucketRateLimiter>(rateLimiter);
    }

    [Fact]
    public void AddMessageBrokerRateLimit_WithSlidingWindowStrategy_ShouldRegisterSlidingWindowRateLimiter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerRateLimit(options =>
        {
            options.Strategy = RateLimitStrategy.SlidingWindow;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var rateLimiter = serviceProvider.GetService<IRateLimiter>();
        Assert.NotNull(rateLimiter);
        Assert.IsType<SlidingWindowRateLimiter>(rateLimiter);
    }

    [Fact]
    public void AddMessageBrokerRateLimit_WithFixedWindowStrategy_ShouldThrowNotImplementedException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerRateLimit(options =>
        {
            options.Strategy = RateLimitStrategy.FixedWindow;
        });

        // Assert
        Assert.Throws<NotImplementedException>(() =>
            services.BuildServiceProvider().GetService<IRateLimiter>());
    }

    [Fact]
    public void AddMessageBrokerRateLimit_WithRequestsPerSecond_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBrokerRateLimit(500, 1000);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var rateLimiter = serviceProvider.GetService<IRateLimiter>();
        Assert.NotNull(rateLimiter);
        Assert.IsType<TokenBucketRateLimiter>(rateLimiter);
    }

    [Fact]
    public void AddMessageBrokerPerTenantRateLimit_ShouldConfigurePerTenantOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var tenantLimits = new Dictionary<string, int>
        {
            ["tenant1"] = 100,
            ["tenant2"] = 200
        };

        // Act
        services.AddMessageBrokerPerTenantRateLimit(50, tenantLimits);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var rateLimiter = serviceProvider.GetService<IRateLimiter>();
        Assert.NotNull(rateLimiter);
        Assert.IsType<TokenBucketRateLimiter>(rateLimiter);
    }

    [Fact]
    public void DecorateMessageBrokerWithRateLimit_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            RateLimitServiceCollectionExtensions.DecorateMessageBrokerWithRateLimit(null!));
    }

    [Fact]
    public void DecorateMessageBrokerWithRateLimit_ShouldDecorateMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBroker, TestMessageBroker>();
        services.AddMessageBrokerRateLimit();

        // Act
        services.DecorateMessageBrokerWithRateLimit();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var broker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(broker);
        Assert.IsType<RateLimitMessageBrokerDecorator>(broker);
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