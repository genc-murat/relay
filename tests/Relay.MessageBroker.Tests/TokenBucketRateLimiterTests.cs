using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class TokenBucketRateLimiterTests : IDisposable
{
    private readonly Mock<ILogger<TokenBucketRateLimiter>> _loggerMock;
    private readonly RateLimitOptions _options;

    public TokenBucketRateLimiterTests()
    {
        _loggerMock = new Mock<ILogger<TokenBucketRateLimiter>>();
        _options = new RateLimitOptions
        {
            Enabled = true,
            RequestsPerSecond = 10,
            BucketCapacity = 20,
            WindowSize = TimeSpan.FromSeconds(1),
            CleanupInterval = TimeSpan.FromMinutes(1)
        };
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TokenBucketRateLimiter(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TokenBucketRateLimiter(Options.Create(_options), null!));
    }

    [Fact]
    public void Constructor_WithInvalidOptions_ShouldThrowException()
    {
        // Arrange
        var invalidOptions = new RateLimitOptions { Enabled = true, RequestsPerSecond = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new TokenBucketRateLimiter(Options.Create(invalidOptions), _loggerMock.Object));
    }

    [Fact]
    public async Task CheckAsync_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        using var limiter = new TokenBucketRateLimiter(Options.Create(_options), _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await limiter.CheckAsync(null!));
    }

    [Fact]
    public async Task CheckAsync_WhenTokensAvailable_ShouldAllowRequest()
    {
        // Arrange
        using var limiter = new TokenBucketRateLimiter(Options.Create(_options), _loggerMock.Object);

        // Act
        var result = await limiter.CheckAsync("test-key");

        // Assert
        Assert.True(result.Allowed);
        Assert.True(result.RemainingRequests >= 0);
    }

    [Fact]
    public async Task CheckAsync_WhenTokensExhausted_ShouldRejectRequest()
    {
        // Arrange
        var smallCapacityOptions = new RateLimitOptions
        {
            Enabled = true,
            RequestsPerSecond = 1,
            BucketCapacity = 1,
            WindowSize = TimeSpan.FromSeconds(1),
            CleanupInterval = TimeSpan.FromMinutes(1)
        };
        using var limiter = new TokenBucketRateLimiter(Options.Create(smallCapacityOptions), _loggerMock.Object);

        // Consume the token
        await limiter.CheckAsync("test-key");

        // Act - Second request should be rejected
        var result = await limiter.CheckAsync("test-key");

        // Assert
        Assert.False(result.Allowed);
        Assert.True(result.RetryAfter > TimeSpan.Zero);
    }

    [Fact]
    public async Task CheckAsync_WithPerTenantLimits_ShouldUseTenantSpecificLimit()
    {
        // Arrange
        var tenantOptions = new RateLimitOptions
        {
            Enabled = true,
            RequestsPerSecond = 10,
            EnablePerTenantLimits = true,
            DefaultTenantLimit = 5,
            TenantLimits = new Dictionary<string, int> { ["tenant1"] = 1 },
            BucketCapacity = 1, // Set capacity to 1 to ensure only 1 request allowed
            WindowSize = TimeSpan.FromSeconds(1),
            CleanupInterval = TimeSpan.FromMinutes(1)
        };
        using var limiter = new TokenBucketRateLimiter(Options.Create(tenantOptions), _loggerMock.Object);

        // Consume tenant1 token
        await limiter.CheckAsync("tenant1");

        // Act - Second request should be rejected
        var result = await limiter.CheckAsync("tenant1");

        // Assert
        Assert.False(result.Allowed);
    }

    [Fact]
    public void GetMetrics_ShouldReturnCorrectMetrics()
    {
        // Arrange
        using var limiter = new TokenBucketRateLimiter(Options.Create(_options), _loggerMock.Object);

        // Act
        var metrics = limiter.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalRequests);
        Assert.Equal(0, metrics.AllowedRequests);
        Assert.Equal(0, metrics.RejectedRequests);
        Assert.Equal(0.0, metrics.CurrentRate);
        Assert.Equal(0, metrics.ActiveKeys);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}