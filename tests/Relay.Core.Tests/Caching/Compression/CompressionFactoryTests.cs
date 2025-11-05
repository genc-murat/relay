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
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.GZip, compressor.Algorithm);
    }

    [Fact]
    public void CreateUnified_WithAlgorithm_ShouldCreateCorrectCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateUnified(CompressionAlgorithm.Deflate);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Deflate, compressor.Algorithm);
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
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Brotli, compressor.Algorithm);
        Assert.True(compressor.ShouldCompress(3000));
        Assert.False(compressor.ShouldCompress(1000));
    }

    [Fact]
    public void CreateCache_WithDefaultParameters_ShouldCreateCacheCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateCache();

        // Assert
        Assert.NotNull(compressor);
        Assert.IsAssignableFrom<ICacheCompressor>(compressor);
    }

    [Fact]
    public void CreateCache_WithAlgorithm_ShouldCreateCorrectCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateCache(CompressionAlgorithm.Deflate);

        // Assert
        Assert.NotNull(compressor);
        Assert.IsAssignableFrom<ICacheCompressor>(compressor);
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
        Assert.NotNull(compressor);
        Assert.IsAssignableFrom<ICacheCompressor>(compressor);
        Assert.True(compressor.ShouldCompress(3000));
        Assert.False(compressor.ShouldCompress(1000));
    }

    [Fact]
    public void CreateGzipCache_WithDefaultThreshold_ShouldCreateGZipCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateGzipCache();

        // Assert
        Assert.NotNull(compressor);
        Assert.IsAssignableFrom<ICacheCompressor>(compressor);
        Assert.True(compressor.ShouldCompress(2000));
        Assert.False(compressor.ShouldCompress(500));
    }

    [Fact]
    public void CreateGzipCache_WithCustomThreshold_ShouldCreateGZipCompressor()
    {
        // Arrange & Act
        var compressor = CompressionFactory.CreateGzipCache(2048);

        // Assert
        Assert.NotNull(compressor);
        Assert.IsAssignableFrom<ICacheCompressor>(compressor);
        Assert.True(compressor.ShouldCompress(3000));
        Assert.False(compressor.ShouldCompress(1000));
    }
}

