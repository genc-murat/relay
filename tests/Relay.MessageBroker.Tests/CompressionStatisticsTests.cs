using Relay.MessageBroker.Compression;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CompressionStatisticsTests
{
    [Fact]
    public void CompressionStatistics_DefaultValues_ShouldBeZero()
    {
        // Act
        var stats = new CompressionStatistics();

        // Assert
        Assert.Equal(0, stats.TotalMessages);
        Assert.Equal(0, stats.CompressedMessages);
        Assert.Equal(0, stats.SkippedMessages);
        Assert.Equal(0L, stats.TotalOriginalBytes);
        Assert.Equal(0L, stats.TotalCompressedBytes);
        Assert.Equal(0, stats.AverageCompressionRatio);
        Assert.Equal(0L, stats.TotalBytesSaved);
        Assert.Equal(0, stats.CompressionRate);
    }

    [Fact]
    public void CompressionStatistics_AverageCompressionRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalOriginalBytes = 1000,
            TotalCompressedBytes = 500
        };

        // Act
        var ratio = stats.AverageCompressionRatio;

        // Assert
        Assert.Equal(0.5, ratio);
    }

    [Fact]
    public void CompressionStatistics_TotalBytesSaved_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalOriginalBytes = 1000,
            TotalCompressedBytes = 300
        };

        // Act
        var saved = stats.TotalBytesSaved;

        // Assert
        Assert.Equal(700L, saved);
    }

    [Fact]
    public void CompressionStatistics_CompressionRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalMessages = 100,
            CompressedMessages = 75
        };

        // Act
        var rate = stats.CompressionRate;

        // Assert
        Assert.Equal(0.75, rate);
    }

    [Fact]
    public void CompressionStatistics_AverageCompressionTime_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            CompressedMessages = 10,
            TotalCompressionTime = TimeSpan.FromSeconds(5)
        };

        // Act
        var avgTime = stats.AverageCompressionTime;

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(500), avgTime);
    }

    [Fact]
    public void CompressionStatistics_AverageDecompressionTime_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            CompressedMessages = 20,
            TotalDecompressionTime = TimeSpan.FromSeconds(4)
        };

        // Act
        var avgTime = stats.AverageDecompressionTime;

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(200), avgTime);
    }

    [Fact]
    public void CompressionStatistics_WithZeroMessages_ShouldReturnZero()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalMessages = 0,
            CompressedMessages = 0
        };

        // Act & Assert
        Assert.Equal(0, stats.AverageCompressionRatio);
        Assert.Equal(0, stats.CompressionRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageCompressionTime);
        Assert.Equal(TimeSpan.Zero, stats.AverageDecompressionTime);
    }
}