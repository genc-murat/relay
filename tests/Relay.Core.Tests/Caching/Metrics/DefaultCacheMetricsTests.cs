using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching.Metrics;
using System;
using Xunit;

namespace Relay.Core.Tests.Caching.Metrics;

public class DefaultCacheMetricsTests
{
    private readonly Mock<ILogger<DefaultCacheMetrics>> _loggerMock;
    private readonly DefaultCacheMetrics _metrics;

    public DefaultCacheMetricsTests()
    {
        _loggerMock = new Mock<ILogger<DefaultCacheMetrics>>();
        _metrics = new DefaultCacheMetrics(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultCacheMetrics(null!));
    }

    [Fact]
    public void RecordHit_ShouldIncrementHitCount()
    {
        // Arrange
        var cacheKey = "test-key";
        var requestType = "TestRequest";

        // Act
        _metrics.RecordHit(cacheKey, requestType);

        // Assert
        var stats = _metrics.GetStatistics();
        stats.Hits.Should().Be(1);
        stats.Misses.Should().Be(0);
        stats.Sets.Should().Be(0);
        stats.Evictions.Should().Be(0);
        stats.HitRatio.Should().Be(1.0);
    }

    [Fact]
    public void RecordMiss_ShouldIncrementMissCount()
    {
        // Arrange
        var cacheKey = "test-key";
        var requestType = "TestRequest";

        // Act
        _metrics.RecordMiss(cacheKey, requestType);

        // Assert
        var stats = _metrics.GetStatistics();
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(1);
        stats.Sets.Should().Be(0);
        stats.Evictions.Should().Be(0);
        stats.HitRatio.Should().Be(0.0);
    }

    [Fact]
    public void RecordSet_ShouldIncrementSetCountAndDataSize()
    {
        // Arrange
        var cacheKey = "test-key";
        var requestType = "TestRequest";
        var dataSize = 1024L;

        // Act
        _metrics.RecordSet(cacheKey, requestType, dataSize);

        // Assert
        var stats = _metrics.GetStatistics();
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.Sets.Should().Be(1);
        stats.Evictions.Should().Be(0);
        stats.TotalDataSize.Should().Be(dataSize);
        stats.AverageDataSize.Should().Be(dataSize);
    }

    [Fact]
    public void RecordEviction_ShouldIncrementEvictionCount()
    {
        // Arrange
        var cacheKey = "test-key";
        var requestType = "TestRequest";

        // Act
        _metrics.RecordEviction(cacheKey, requestType);

        // Assert
        var stats = _metrics.GetStatistics();
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.Sets.Should().Be(0);
        stats.Evictions.Should().Be(1);
    }

    [Fact]
    public void GetStatistics_WithMultipleOperations_ShouldCalculateCorrectly()
    {
        // Arrange
        var cacheKey = "test-key";
        var requestType = "TestRequest";

        // Act
        _metrics.RecordHit(cacheKey, requestType);
        _metrics.RecordHit(cacheKey, requestType);
        _metrics.RecordMiss(cacheKey, requestType);
        _metrics.RecordSet(cacheKey, requestType, 500);
        _metrics.RecordSet(cacheKey, requestType, 1500);
        _metrics.RecordEviction(cacheKey, requestType);

        // Assert
        var stats = _metrics.GetStatistics();
        stats.Hits.Should().Be(2);
        stats.Misses.Should().Be(1);
        stats.Sets.Should().Be(2);
        stats.Evictions.Should().Be(1);
        stats.TotalDataSize.Should().Be(2000);
        stats.HitRatio.Should().Be(2.0 / 3.0); // 2 hits out of 3 total requests
        stats.AverageDataSize.Should().Be(1000); // 2000 / 2 sets
    }

    [Fact]
    public void GetStatistics_WithSpecificRequestType_ShouldReturnFilteredStats()
    {
        // Arrange
        _metrics.RecordHit("key1", "RequestType1");
        _metrics.RecordMiss("key2", "RequestType1");
        _metrics.RecordHit("key3", "RequestType2");
        _metrics.RecordSet("key4", "RequestType1", 1000);

        // Act
        var globalStats = _metrics.GetStatistics();
        var type1Stats = _metrics.GetStatistics("RequestType1");
        var type2Stats = _metrics.GetStatistics("RequestType2");

        // Assert
        globalStats.Hits.Should().Be(2);
        globalStats.Misses.Should().Be(1);
        globalStats.Sets.Should().Be(1);

        type1Stats.Hits.Should().Be(1);
        type1Stats.Misses.Should().Be(1);
        type1Stats.Sets.Should().Be(1);

        type2Stats.Hits.Should().Be(1);
        type2Stats.Misses.Should().Be(0);
        type2Stats.Sets.Should().Be(0);
    }

    [Fact]
    public void GetStatistics_WithNoOperations_ShouldReturnZeroes()
    {
        // Act
        var stats = _metrics.GetStatistics();

        // Assert
        stats.Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.Sets.Should().Be(0);
        stats.Evictions.Should().Be(0);
        stats.TotalDataSize.Should().Be(0);
        stats.HitRatio.Should().Be(0.0);
        stats.AverageDataSize.Should().Be(0.0);
    }

    [Fact]
    public void HitRatio_WithNoHitsOrMisses_ShouldBeZero()
    {
        // Act
        var stats = _metrics.GetStatistics();

        // Assert
        stats.HitRatio.Should().Be(0.0);
    }

    [Theory]
    [InlineData(1, 0, 1.0)]
    [InlineData(0, 1, 0.0)]
    [InlineData(5, 5, 0.5)]
    [InlineData(10, 3, 10.0 / 13.0)]
    public void HitRatio_ShouldCalculateCorrectly(int hits, int misses, double expectedRatio)
    {
        // Arrange
        for (int i = 0; i < hits; i++)
        {
            _metrics.RecordHit("key", "RequestType");
        }
        for (int i = 0; i < misses; i++)
        {
            _metrics.RecordMiss("key", "RequestType");
        }

        // Act
        var stats = _metrics.GetStatistics();

        // Assert
        stats.HitRatio.Should().BeApproximately(expectedRatio, 0.0001);
    }
}