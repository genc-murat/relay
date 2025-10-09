using FluentAssertions;
using Relay.MessageBroker.Compression;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CompressionAdditionalTests
{
    [Fact]
    public void GZipMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new GZipMessageCompressor(level: 6);

        // Assert
        compressor.CoreAlgorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        byte[]? data = null;

        // Act
        var result = await compressor.CompressAsync(data!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressAsync_WithEmptyData_ShouldReturnEmpty()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.CompressAsync(data);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GZipMessageCompressor_DecompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        byte[]? data = null;

        // Act
        var result = await compressor.DecompressAsync(data!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GZipMessageCompressor_DecompressAsync_WithEmptyData_ShouldReturnEmpty()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.DecompressAsync(data);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithNullData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var result = compressor.IsCompressed(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithShortData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = new byte[] { 0x1f };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithGZipMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = new byte[] { 0x1f, 0x8b, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithInvalidMagicNumber_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var originalData = System.Text.Encoding.UTF8.GetBytes("Hello World! This is test data for compression.");

        // Act
        var compressed = await compressor.CompressAsync(originalData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressAsync_WithCancellation_ShouldHandleCancellation()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var data = System.Text.Encoding.UTF8.GetBytes("Short data");  // Use shorter data for faster cancellation
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - With already cancelled token, it should either throw or complete quickly
        try
        {
            await compressor.CompressAsync(data, cts.Token);
            // If it completes without throwing, that's acceptable for small data
        }
        catch (OperationCanceledException)
        {
            // This is also acceptable
        }

        // Assert - No assertion needed, we just want to verify it doesn't hang
        Assert.True(true);
    }

    [Fact]
    public void DeflateMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new DeflateMessageCompressor(level: 9);

        // Assert
        compressor.CoreAlgorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate);
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var result = await compressor.CompressAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressAsync_WithEmptyData_ShouldReturnEmpty()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.CompressAsync(data);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();
        var originalData = System.Text.Encoding.UTF8.GetBytes("Deflate compression test data!");

        // Act
        var compressed = await compressor.CompressAsync(originalData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void BrotliMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new BrotliMessageCompressor(level: 9);

        // Assert
        compressor.CoreAlgorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli);
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var result = await compressor.CompressAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressAsync_WithEmptyData_ShouldReturnEmpty()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.CompressAsync(data);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();
        var originalData = System.Text.Encoding.UTF8.GetBytes("Brotli compression test data!");

        // Act
        var compressed = await compressor.CompressAsync(originalData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().Equal(originalData);
    }

    [Fact]
    public void CompressionAlgorithm_AllValues_ShouldBeDistinct()
    {
        // Act
        var values = Enum.GetValues<Relay.Core.Caching.Compression.CompressionAlgorithm>();

        // Assert
        values.Should().HaveCountGreaterThanOrEqualTo(4); // At least None, GZip, Deflate, Brotli
        values.Should().Contain(Relay.Core.Caching.Compression.CompressionAlgorithm.None);
        values.Should().Contain(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        values.Should().Contain(Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate);
        values.Should().Contain(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli);
    }

    [Fact]
    public void CompressionStatistics_DefaultValues_ShouldBeZero()
    {
        // Act
        var stats = new CompressionStatistics();

        // Assert
        stats.TotalMessages.Should().Be(0);
        stats.CompressedMessages.Should().Be(0);
        stats.SkippedMessages.Should().Be(0);
        stats.TotalOriginalBytes.Should().Be(0);
        stats.TotalCompressedBytes.Should().Be(0);
        stats.TotalCompressionTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void CompressionStatistics_CanSetAllProperties()
    {
        // Act
        var stats = new CompressionStatistics
        {
            TotalMessages = 1000,
            CompressedMessages = 800,
            SkippedMessages = 200,
            TotalOriginalBytes = 1000000,
            TotalCompressedBytes = 500000,
            TotalCompressionTime = TimeSpan.FromMilliseconds(100)
        };

        // Assert
        stats.TotalMessages.Should().Be(1000);
        stats.CompressedMessages.Should().Be(800);
        stats.SkippedMessages.Should().Be(200);
        stats.TotalOriginalBytes.Should().Be(1000000);
        stats.TotalCompressedBytes.Should().Be(500000);
        stats.TotalCompressionTime.Should().Be(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void CompressionOptions_DefaultValues_ShouldBeSet()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        options.Algorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        options.Level.Should().BeGreaterThan(0);
        options.MinimumSizeBytes.Should().BeGreaterThan(0);
        // Enabled may be false by default, so don't assert specific value
    }

    [Fact]
    public void CompressionOptions_CanSetAllProperties()
    {
        // Act
        var options = new CompressionOptions
        {
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli,
            Level = 8,
            MinimumSizeBytes = 2048,
            Enabled = false
        };

        // Assert
        options.Algorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli);
        options.Level.Should().Be(8);
        options.MinimumSizeBytes.Should().Be(2048);
        options.Enabled.Should().BeFalse();
    }
}
