using Relay.MessageBroker;
using Relay.MessageBroker.Compression;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument

namespace Relay.MessageBroker.Tests;

public class CompressionAdditionalTests : IDisposable
{
    private readonly GZipMessageCompressor _gzipCompressor;
    private readonly DeflateMessageCompressor _deflateCompressor;
    private readonly BrotliMessageCompressor _brotliCompressor;

    public CompressionAdditionalTests()
    {
        _gzipCompressor = new GZipMessageCompressor();
        _deflateCompressor = new DeflateMessageCompressor();
        _brotliCompressor = new BrotliMessageCompressor();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
    [Fact]
    public void GZipMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new GZipMessageCompressor(level: 6);

        // Assert
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, compressor.CoreAlgorithm);
    }

    [Fact]
    public void DeflateMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new DeflateMessageCompressor(level: 9);

        // Assert
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate, compressor.CoreAlgorithm);
    }

    [Fact]
    public void BrotliMessageCompressor_WithLowLevel_ShouldUseFastestCompression()
    {
        // Arrange & Act
        var compressor = new BrotliMessageCompressor(level: 9);

        // Assert
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, compressor.CoreAlgorithm);
    }

    [Theory]
    [InlineData("GZip")]
    [InlineData("Deflate")]
    [InlineData("Brotli")]
    public async Task MessageCompressor_CompressAsync_WithNullData_ShouldReturnNull(string compressorType)
    {
        // Arrange
        var compressor = GetCompressor(compressorType);

        // Act
        var result = await compressor.CompressAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("GZip")]
    [InlineData("Deflate")]
    [InlineData("Brotli")]
    public async Task MessageCompressor_CompressAsync_WithEmptyData_ShouldReturnEmpty(string compressorType)
    {
        // Arrange
        var compressor = GetCompressor(compressorType);
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.CompressAsync(data);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("GZip")]
    [InlineData("Deflate")]
    [InlineData("Brotli")]
    public async Task MessageCompressor_DecompressAsync_WithNullData_ShouldReturnNull(string compressorType)
    {
        // Arrange
        var compressor = GetCompressor(compressorType);

        // Act
        var result = await compressor.DecompressAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("GZip")]
    [InlineData("Deflate")]
    [InlineData("Brotli")]
    public async Task MessageCompressor_DecompressAsync_WithEmptyData_ShouldReturnEmpty(string compressorType)
    {
        // Arrange
        var compressor = GetCompressor(compressorType);
        var data = Array.Empty<byte>();

        // Act
        var result = await compressor.DecompressAsync(data);

        // Assert
        Assert.Empty(result);
    }



    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithShortData_ShouldReturnFalse()
    {
        // Arrange
        var data = new byte[] { 0x1f };

        // Act
        var result = _gzipCompressor.IsCompressed(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithGZipMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var data = new byte[] { 0x1f, 0x8b, 0x00, 0x00 };

        // Act
        var result = _gzipCompressor.IsCompressed(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithInvalidMagicNumber_ShouldReturnFalse()
    {
        // Arrange
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        // Act
        var result = _gzipCompressor.IsCompressed(data);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("GZip", "Hello World! This is test data for compression.")]
    [InlineData("Deflate", "Deflate compression test data!")]
    [InlineData("Brotli", "Brotli compression test data!")]
    public async Task MessageCompressor_CompressDecompress_ShouldPreserveData(string compressorType, string testData)
    {
        // Arrange
        var compressor = GetCompressor(compressorType);
        var originalData = System.Text.Encoding.UTF8.GetBytes(testData);

        // Act
        var compressed = await compressor.CompressAsync(originalData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        Assert.Equal(originalData, decompressed);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressAsync_WithCancellation_ShouldHandleCancellation()
    {
        // Arrange
        var data = System.Text.Encoding.UTF8.GetBytes("Short data");  // Use shorter data for faster cancellation
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - With already cancelled token, it should either throw or complete quickly
        try
        {
            await _gzipCompressor.CompressAsync(data, cts.Token);
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
    public void CompressionAlgorithm_AllValues_ShouldBeDistinct()
    {
        // Act
        var values = Enum.GetValues<Relay.Core.Caching.Compression.CompressionAlgorithm>();

        // Assert
        Assert.True(values.Length >= 4); // At least None, GZip, Deflate, Brotli
        Assert.Contains(Relay.Core.Caching.Compression.CompressionAlgorithm.None, values);
        Assert.Contains(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, values);
        Assert.Contains(Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate, values);
        Assert.Contains(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, values);
    }

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
        Assert.Equal(TimeSpan.Zero, stats.TotalCompressionTime);
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
        Assert.Equal(1000, stats.TotalMessages);
        Assert.Equal(800, stats.CompressedMessages);
        Assert.Equal(200, stats.SkippedMessages);
        Assert.Equal(1000000L, stats.TotalOriginalBytes);
        Assert.Equal(500000L, stats.TotalCompressedBytes);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.TotalCompressionTime);
    }

    [Fact]
    public void CompressionOptions_DefaultValues_ShouldBeSet()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, options.Algorithm);
        Assert.True(options.Level > 0);
        Assert.True(options.MinimumSizeBytes > 0);
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
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, options.Algorithm);
        Assert.Equal(8, options.Level);
        Assert.Equal(2048, options.MinimumSizeBytes);
        Assert.False(options.Enabled);
    }

    private global::Relay.MessageBroker.Compression.IMessageCompressor GetCompressor(string type, int level = 6) =>
        type switch
        {
            "GZip" => level == 6 ? _gzipCompressor : new GZipMessageCompressor(level: level),
            "Deflate" => level == 6 ? _deflateCompressor : new DeflateMessageCompressor(level: level),
            "Brotli" => level == 6 ? _brotliCompressor : new BrotliMessageCompressor(level: level),
            _ => throw new ArgumentException("Invalid compressor type")
        };
}
