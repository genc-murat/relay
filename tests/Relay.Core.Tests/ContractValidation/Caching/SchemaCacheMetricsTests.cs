using System;
using Relay.Core.ContractValidation.Caching;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Caching;

public class SchemaCacheMetricsTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var metrics = new SchemaCacheMetrics
        {
            TotalRequests = 100,
            CacheHits = 80,
            CacheMisses = 20,
            CurrentSize = 50,
            MaxSize = 100,
            TotalEvictions = 5
        };

        // Assert
        Assert.Equal(100, metrics.TotalRequests);
        Assert.Equal(80, metrics.CacheHits);
        Assert.Equal(20, metrics.CacheMisses);
        Assert.Equal(50, metrics.CurrentSize);
        Assert.Equal(100, metrics.MaxSize);
        Assert.Equal(5, metrics.TotalEvictions);
    }

    [Fact]
    public void HitRate_CalculatesCorrectly_WithRequests()
    {
        // Arrange
        var metrics = new SchemaCacheMetrics
        {
            TotalRequests = 100,
            CacheHits = 80,
            CacheMisses = 20
        };

        // Act
        var hitRate = metrics.HitRate;

        // Assert
        Assert.Equal(0.8, hitRate);
    }

    [Fact]
    public void HitRate_ReturnsZero_WithNoRequests()
    {
        // Arrange
        var metrics = new SchemaCacheMetrics
        {
            TotalRequests = 0,
            CacheHits = 0,
            CacheMisses = 0
        };

        // Act
        var hitRate = metrics.HitRate;

        // Assert
        Assert.Equal(0, hitRate);
    }

    [Fact]
    public void HitRate_CalculatesCorrectly_WithPerfectHitRate()
    {
        // Arrange
        var metrics = new SchemaCacheMetrics
        {
            TotalRequests = 100,
            CacheHits = 100,
            CacheMisses = 0
        };

        // Act
        var hitRate = metrics.HitRate;

        // Assert
        Assert.Equal(1.0, hitRate);
    }

    [Fact]
    public void HitRate_CalculatesCorrectly_WithZeroHitRate()
    {
        // Arrange
        var metrics = new SchemaCacheMetrics
        {
            TotalRequests = 100,
            CacheHits = 0,
            CacheMisses = 100
        };

        // Act
        var hitRate = metrics.HitRate;

        // Assert
        Assert.Equal(0.0, hitRate);
    }

    [Fact]
    public void Timestamp_IsSetToCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var metrics = new SchemaCacheMetrics();
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.InRange(metrics.Timestamp, before, after);
    }
}
