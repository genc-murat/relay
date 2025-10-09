using FluentAssertions;
using Relay.Core.Caching.Compression;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression;

public class CompressionFactoryTests
{
    [Fact]
    public void CreateUnified_WithDefaultParameters_ShouldCreateGZipCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateUnified();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.GZip);
    }

    [Fact]
    public void CreateUnified_WithAlgorithm_ShouldCreateCorrectCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateUnified(CompressionAlgorithm.Deflate);

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Deflate);
    }

    [Fact]
    public void CreateUnified_WithOptions_ShouldUseOptions()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Algorithm = CompressionAlgorithm.Brotli,
            Level = 8,
            MinimumSizeBytes = 2048
        };

        // Act
        var compressor = CompressionFactory.CreateUnified(options);

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Brotli);
        compressor.ShouldCompress(3000).Should().BeTrue();
        compressor.ShouldCompress(1000).Should().BeFalse();
    }

    [Fact]
    public void CreateCache_WithDefaultParameters_ShouldCreateCacheCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateCache();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Should().BeAssignableTo<ICacheCompressor>();
    }

    [Fact]
    public void CreateCache_WithAlgorithm_ShouldCreateCorrectCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateCache(CompressionAlgorithm.Deflate);

        // Assert
        compressor.Should().NotBeNull();
        compressor.Should().BeAssignableTo<ICacheCompressor>();
    }

    [Fact]
    public void CreateCache_WithOptions_ShouldUseOptions()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Algorithm = CompressionAlgorithm.Brotli,
            Level = 8,
            MinimumSizeBytes = 2048
        };

        // Act
        var compressor = CompressionFactory.CreateCache(options);

        // Assert
        compressor.Should().NotBeNull();
        compressor.Should().BeAssignableTo<ICacheCompressor>();
        compressor.ShouldCompress(3000).Should().BeTrue();
        compressor.ShouldCompress(1000).Should().BeFalse();
    }



    [Fact]
    public void CreateGzipCache_WithDefaultThreshold_ShouldCreateGZipCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateGzipCache();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Should().BeAssignableTo<ICacheCompressor>();
        compressor.ShouldCompress(2000).Should().BeTrue();
        compressor.ShouldCompress(500).Should().BeFalse();
    }

    [Fact]
    public void CreateGzipCache_WithCustomThreshold_ShouldCreateGZipCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateGzipCache(2048);

        // Assert
        compressor.Should().NotBeNull();
        compressor.Should().BeAssignableTo<ICacheCompressor>();
        compressor.ShouldCompress(3000).Should().BeTrue();
        compressor.ShouldCompress(1000).Should().BeFalse();
    }
}