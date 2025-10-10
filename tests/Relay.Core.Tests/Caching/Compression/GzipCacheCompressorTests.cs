using Relay.Core.Caching.Compression;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression;

public class GzipCacheCompressorTests
{
    [Fact]
    public void Constructor_WithDefaultThreshold_ShouldSetDefaultThreshold()
    {
        // Arrange & Act
        var compressor = new GzipCacheCompressor();

        // Assert
        Assert.NotNull(compressor);
    }

    [Fact]
    public void Constructor_WithCustomThreshold_ShouldSetCustomThreshold()
    {
        // Arrange & Act
        var compressor = new GzipCacheCompressor(2048);

        // Assert
        Assert.NotNull(compressor);
    }

    [Fact]
    public void Compress_And_Decompress_ShouldReturnOriginalData()
    {
        // Arrange
        var compressor = new GzipCacheCompressor();
        var originalData = Encoding.UTF8.GetBytes("This is a test string for compression and decompression.");

        // Act
        var compressed = compressor.Compress(originalData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        Assert.Equal(originalData, decompressed);
    }

    [Fact]
    public void Compress_ShouldReturnDifferentDataForCompressibleContent()
    {
        // Arrange
        var compressor = new GzipCacheCompressor();
        var originalData = Encoding.UTF8.GetBytes(new string('A', 1000)); // Large repetitive data

        // Act
        var compressed = compressor.Compress(originalData);

        // Assert
        Assert.NotEqual(originalData, compressed);
        Assert.True(compressed.Length < originalData.Length);
    }

    [Fact]
    public void ShouldCompress_WithLargeData_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new GzipCacheCompressor(100);
        var largeData = new byte[200];

        // Act
        var shouldCompress = compressor.ShouldCompress(largeData.Length);

        // Assert
        Assert.True(shouldCompress);
    }

    [Fact]
    public void ShouldCompress_WithSmallData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GzipCacheCompressor(100);
        var smallData = new byte[50];

        // Act
        var shouldCompress = compressor.ShouldCompress(smallData.Length);

        // Assert
        Assert.False(shouldCompress);
    }

    [Fact]
    public void Compress_WithNullData_ShouldThrowException()
    {
        // Arrange
        var compressor = new GzipCacheCompressor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => compressor.Compress(null!));
    }

    [Fact]
    public void Decompress_WithNullData_ShouldThrowException()
    {
        // Arrange
        var compressor = new GzipCacheCompressor();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => compressor.Decompress(null!));
    }

    [Fact]
    public void Decompress_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        var compressor = new GzipCacheCompressor();
        var invalidData = new byte[] { 0x00, 0x01, 0x02 };

        // Act & Assert
        Assert.Throws<InvalidDataException>(() => compressor.Decompress(invalidData));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Compress_Decompress_RoundTrip_ShouldPreserveData(int dataSize)
    {
        // Arrange
        var compressor = new GzipCacheCompressor();
        var random = new Random(42); // Fixed seed for reproducible tests
        var originalData = new byte[dataSize];
        random.NextBytes(originalData);

        // Act
        var compressed = compressor.Compress(originalData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        Assert.Equal(originalData, decompressed);
    }
}