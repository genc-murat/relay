using Relay.MessageBroker;
using Relay.MessageBroker.Compression;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.Saga;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CommonMessageBrokerOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new CommonMessageBrokerOptions();

        // Assert
        Assert.Equal("relay.events", options.DefaultExchange);
        Assert.Equal("{MessageType}", options.DefaultRoutingKeyPattern);
        Assert.False(options.AutoPublishResults);
        Assert.True(options.EnableSerialization);
        Assert.Equal(MessageSerializerType.Json, options.SerializerType);
        Assert.Null(options.RetryPolicy);
        Assert.Null(options.CircuitBreaker);
        Assert.Null(options.Compression);
        Assert.Null(options.Telemetry);
        Assert.Null(options.Saga);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var options = new CommonMessageBrokerOptions();

        // Act
        options.DefaultExchange = "custom.exchange";
        options.DefaultRoutingKeyPattern = "custom.{Type}";
        options.AutoPublishResults = true;
        options.EnableSerialization = false;
        options.SerializerType = MessageSerializerType.MessagePack;
        options.RetryPolicy = new RetryPolicy { MaxAttempts = 5 };
        options.CircuitBreaker = new CircuitBreakerOptions();
        options.Compression = new CompressionOptions();
        options.Telemetry = new RelayTelemetryOptions();
        options.Saga = new SagaOptions();

        // Assert
        Assert.Equal("custom.exchange", options.DefaultExchange);
        Assert.Equal("custom.{Type}", options.DefaultRoutingKeyPattern);
        Assert.True(options.AutoPublishResults);
        Assert.False(options.EnableSerialization);
        Assert.Equal(MessageSerializerType.MessagePack, options.SerializerType);
        Assert.NotNull(options.RetryPolicy);
        Assert.NotNull(options.CircuitBreaker);
        Assert.NotNull(options.Compression);
        Assert.NotNull(options.Telemetry);
        Assert.NotNull(options.Saga);
    }

    [Fact]
    public void RetryPolicy_ShouldHaveCorrectDefaults()
    {
        // Act
        var retryPolicy = new RetryPolicy();

        // Assert
        Assert.Equal(3, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryPolicy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), retryPolicy.MaxDelay);
        Assert.Equal(2.0, retryPolicy.BackoffMultiplier);
        Assert.True(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_Properties_ShouldBeSettable()
    {
        // Arrange
        var retryPolicy = new RetryPolicy();

        // Act
        retryPolicy.MaxAttempts = 10;
        retryPolicy.InitialDelay = TimeSpan.FromMilliseconds(500);
        retryPolicy.MaxDelay = TimeSpan.FromMinutes(5);
        retryPolicy.BackoffMultiplier = 1.5;
        retryPolicy.UseExponentialBackoff = false;

        // Assert
        Assert.Equal(10, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), retryPolicy.InitialDelay);
        Assert.Equal(TimeSpan.FromMinutes(5), retryPolicy.MaxDelay);
        Assert.Equal(1.5, retryPolicy.BackoffMultiplier);
        Assert.False(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_WithZeroMaxAttempts_ShouldBeValid()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { MaxAttempts = 0 };

        // Assert
        Assert.Equal(0, retryPolicy.MaxAttempts);
    }

    [Fact]
    public void RetryPolicy_WithNegativeMaxAttempts_ShouldBeAllowed()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { MaxAttempts = -1 };

        // Assert
        Assert.Equal(-1, retryPolicy.MaxAttempts);
    }

    [Fact]
    public void RetryPolicy_WithZeroBackoffMultiplier_ShouldBeValid()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { BackoffMultiplier = 0.0 };

        // Assert
        Assert.Equal(0.0, retryPolicy.BackoffMultiplier);
    }

    [Fact]
    public void RetryPolicy_WithNegativeBackoffMultiplier_ShouldBeAllowed()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { BackoffMultiplier = -1.0 };

        // Assert
        Assert.Equal(-1.0, retryPolicy.BackoffMultiplier);
    }

    [Fact]
    public void RetryPolicy_WithZeroInitialDelay_ShouldBeValid()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { InitialDelay = TimeSpan.Zero };

        // Assert
        Assert.Equal(TimeSpan.Zero, retryPolicy.InitialDelay);
    }

    [Fact]
    public void RetryPolicy_WithNegativeInitialDelay_ShouldBeAllowed()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { InitialDelay = TimeSpan.FromSeconds(-1) };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(-1), retryPolicy.InitialDelay);
    }

    [Fact]
    public void RetryPolicy_WithZeroMaxDelay_ShouldBeValid()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { MaxDelay = TimeSpan.Zero };

        // Assert
        Assert.Equal(TimeSpan.Zero, retryPolicy.MaxDelay);
    }

    [Fact]
    public void RetryPolicy_WithNegativeMaxDelay_ShouldBeAllowed()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy { MaxDelay = TimeSpan.FromSeconds(-1) };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(-1), retryPolicy.MaxDelay);
    }
}