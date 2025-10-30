using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RateLimitOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            DefaultTenantLimit = 50,
            WindowSize = TimeSpan.FromSeconds(1),
            CleanupInterval = TimeSpan.FromMinutes(1),
            BucketCapacity = 200
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithRequestsPerSecondZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions { Enabled = true, RequestsPerSecond = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithRequestsPerSecondNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions { Enabled = true, RequestsPerSecond = -1 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithDefaultTenantLimitZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions { Enabled = true, DefaultTenantLimit = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithDefaultTenantLimitNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions { Enabled = true, DefaultTenantLimit = -1 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithWindowSizeZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            WindowSize = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithWindowSizeNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            WindowSize = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithCleanupIntervalZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            CleanupInterval = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithCleanupIntervalNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            CleanupInterval = TimeSpan.FromMinutes(-1)
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithBucketCapacityZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            BucketCapacity = 0
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithBucketCapacityNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            BucketCapacity = -1
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithPerTenantLimitsContainingZero_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            EnablePerTenantLimits = true,
            TenantLimits = new Dictionary<string, int> { ["tenant1"] = 0 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithPerTenantLimitsContainingNegative_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = true,
            EnablePerTenantLimits = true,
            TenantLimits = new Dictionary<string, int> { ["tenant1"] = -1 }
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithDisabledRateLimiting_ShouldNotValidateLimits()
    {
        // Arrange
        var options = new RateLimitOptions
        {
            Enabled = false,
            RequestsPerSecond = 0,
            DefaultTenantLimit = 0
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void DefaultValues_ShouldBeValid()
    {
        // Arrange
        var options = new RateLimitOptions();

        // Act & Assert
        options.Validate(); // Should not throw with defaults
        Assert.False(options.Enabled);
        Assert.Equal(1000, options.RequestsPerSecond);
        Assert.Equal(RateLimitStrategy.TokenBucket, options.Strategy);
        Assert.False(options.EnablePerTenantLimits);
        Assert.Equal(100, options.DefaultTenantLimit);
        Assert.Null(options.TenantLimits);
        Assert.Null(options.BucketCapacity);
        Assert.Equal(TimeSpan.FromSeconds(1), options.WindowSize);
        Assert.Equal(TimeSpan.FromMinutes(1), options.CleanupInterval);
    }
}