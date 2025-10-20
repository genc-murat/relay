using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BrotliMessageCompressorTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public BrotliMessageCompressorTests()
    {
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
        _smallData = Encoding.UTF8.GetBytes("Hi");
        _emptyData = Array.Empty<byte>();
    }

    [Fact]
    public void BrotliMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new BrotliMessageCompressor();

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Brotli, compressor.Algorithm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(9)]
    public void BrotliMessageCompressor_Constructor_WithDifferentLevels_ShouldSucceed(int level)
    {
        // Act
        var compressor = new BrotliMessageCompressor(level);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Brotli, compressor.Algorithm);
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressAndDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        Assert.Equal(_testData, decompressed);
    }

    [Fact]
    public async Task BrotliMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        Assert.True(compressed.Length < _testData.Length);
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        Assert.Empty(compressed);
    }

    [Fact]
    public void BrotliMessageCompressor_IsCompressed_WithBrotliData_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();
        var brotliData = new byte[] { 0x01, 0x02, 0x03 }; // Simple brotli pattern

        // Act
        var isCompressed = compressor.IsCompressed(brotliData);

        // Assert
        Assert.True(isCompressed);
    }

    [Fact]
    public void BrotliMessageCompressor_IsCompressed_WithInvalidData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();
        var invalidData = new byte[] { 0x0F, 0xFF }; // Invalid brotli pattern

        // Act
        var isCompressed = compressor.IsCompressed(invalidData);

        // Assert
        Assert.False(isCompressed);
    }

    [Fact]
    public async Task BrotliMessageCompressor_DecompressInvalidData_ShouldThrow()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();
        var invalidData = Encoding.UTF8.GetBytes("This is not a valid brotli stream.");

        // Act & Assert
        await Assert.ThrowsAsync<System.InvalidOperationException>(async () => await compressor.DecompressAsync(invalidData));
    }
}