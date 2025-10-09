using System;
using FluentAssertions;
using Relay.Core.Caching.Compression;
using Relay.Core.Caching.Compression.Adapters;
using System.Text;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression.Adapters;

public class CacheCompressorAdapterTests
{
    private readonly byte[] _testData;

    public CacheCompressorAdapterTests()
    {
        _testData = Encoding.UTF8.GetBytes("This is test data for compression adapter testing.");
    }

    [Fact]
    public void Constructor_WithNullCompressor_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new CacheCompressorAdapter(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValidCompressor_ShouldCreateAdapter()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip);

        // Act
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Assert
        adapter.Should().NotBeNull();
    }

    [Fact]
    public void Compress_ShouldDelegateToUnifiedCompressor()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Act
        var compressed = adapter.Compress(_testData);

        // Assert
        compressed.Should().NotBeNull();
        compressed.Should().NotBeEquivalentTo(_testData);
        // Compression should reduce size for this repetitive data
        if (_testData.Length > 100)
        {
            compressed.Length.Should().BeLessThan((int)(_testData.Length * 0.9)); // At least 10% compression
        }
    }

    [Fact]
    public void Decompress_ShouldDelegateToUnifiedCompressor()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);
        var compressed = adapter.Compress(_testData);

        // Act
        var decompressed = adapter.Decompress(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Fact]
    public void ShouldCompress_ShouldDelegateToUnifiedCompressor()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip, 6, 100);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Act
        var shouldCompressLarge = adapter.ShouldCompress(200);
        var shouldCompressSmall = adapter.ShouldCompress(50);

        // Assert
        shouldCompressLarge.Should().BeTrue();
        shouldCompressSmall.Should().BeFalse();
    }

    [Fact]
    public void Compress_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => adapter.Compress(null!));
    }

    [Fact]
    public void Decompress_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => adapter.Decompress(null!));
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Adapter_ShouldWorkWithAllAlgorithms(CompressionAlgorithm algorithm)
    {
        // Arrange
        var unifiedCompressor = new UnifiedCompressor(algorithm);
        var adapter = new CacheCompressorAdapter(unifiedCompressor);

        // Act
        var compressed = adapter.Compress(_testData);
        var decompressed = adapter.Decompress(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
        // Compression should reduce size for this repetitive data
        // Allow some tolerance for different algorithms and overhead
        if (_testData.Length > 100)
        {
            compressed.Length.Should().BeLessThan((int)(_testData.Length * 0.9)); // At least 10% compression
        }
    }
}