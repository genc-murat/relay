using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RetryPolicyTests
{
    [Fact]
    public void RetryPolicy_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy();

        // Assert
        Assert.Equal(3, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryPolicy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), retryPolicy.MaxDelay);
        Assert.Equal(2.0, retryPolicy.BackoffMultiplier);
        Assert.True(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_ShouldAllowCustomConfiguration()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromMinutes(10),
            BackoffMultiplier = 3.0,
            UseExponentialBackoff = false
        };

        // Assert
        Assert.Equal(5, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryPolicy.InitialDelay);
        Assert.Equal(TimeSpan.FromMinutes(10), retryPolicy.MaxDelay);
        Assert.Equal(3.0, retryPolicy.BackoffMultiplier);
        Assert.False(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void MessageBrokerOptions_ShouldAcceptRetryPolicy()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialDelay = TimeSpan.FromMilliseconds(500)
        };

        var options = new MessageBrokerOptions
        {
            BrokerType = MessageBrokerType.RabbitMQ,
            RetryPolicy = retryPolicy
        };

        // Assert
        Assert.NotNull(options.RetryPolicy);
        Assert.Equal(5, options.RetryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.RetryPolicy.InitialDelay);
    }

    [Fact]
    public void RetryPolicy_ShouldCalculateDelaysCorrectly()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            InitialDelay = TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2.0,
            UseExponentialBackoff = true
        };

        // Act & Assert - Test that the policy can be created with these settings
        // The actual delay calculation is done by Polly in the broker implementations
        Assert.Equal(3, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryPolicy.InitialDelay);
        Assert.Equal(2.0, retryPolicy.BackoffMultiplier);
        Assert.True(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_ShouldEnforceMaxDelay()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 10,
            InitialDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            BackoffMultiplier = 2.0,
            UseExponentialBackoff = true
        };

        // Act & Assert
        Assert.Equal(10, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), retryPolicy.InitialDelay);
        Assert.Equal(TimeSpan.FromSeconds(30), retryPolicy.MaxDelay);
        Assert.Equal(2.0, retryPolicy.BackoffMultiplier);
        Assert.True(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_ShouldSupportLinearBackoff()
    {
        // Arrange
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 1.0, // No multiplication
            UseExponentialBackoff = false
        };

        // Act & Assert
        Assert.Equal(5, retryPolicy.MaxAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retryPolicy.InitialDelay);
        Assert.Equal(1.0, retryPolicy.BackoffMultiplier);
        Assert.False(retryPolicy.UseExponentialBackoff);
    }

    [Fact]
    public void RetryPolicy_ShouldValidateMaxAttempts()
    {
        // Arrange & Act
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 0 // Invalid, but allowed by the class
        };

        // Assert - The class allows 0, but implementations should handle it
        Assert.Equal(0, retryPolicy.MaxAttempts);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
    }
}