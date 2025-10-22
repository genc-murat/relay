using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument

namespace Relay.MessageBroker.Tests;

public class DeflateMessageCompressorTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public DeflateMessageCompressorTests()
    {
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
        _smallData = Encoding.UTF8.GetBytes("Hi");
        _emptyData = Array.Empty<byte>();
    }

    [Fact]
    public void DeflateMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new DeflateMessageCompressor();

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Deflate, compressor.Algorithm);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(9)]
    public void DeflateMessageCompressor_Constructor_WithDifferentLevels_ShouldSucceed(int level)
    {
        // Act
        var compressor = new DeflateMessageCompressor(level);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.Deflate, compressor.Algorithm);
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressAndDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        Assert.Equal(_testData, decompressed);
    }

    [Fact]
    public async Task DeflateMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        Assert.True(compressed.Length < _testData.Length);
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        Assert.Empty(compressed);
    }

    [Fact]
    public void DeflateMessageCompressor_IsCompressed_WithDeflateData_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();
        var deflateData = new byte[] { 0x78, 0x9c }; // Deflate default compression

        // Act
        var isCompressed = compressor.IsCompressed(deflateData);

        // Assert
        Assert.True(isCompressed);
    }

    [Fact]
    public void DeflateMessageCompressor_IsCompressed_WithDeflateBestData_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();
        var deflateData = new byte[] { 0x78, 0xda }; // Deflate best compression

        // Act
        var isCompressed = compressor.IsCompressed(deflateData);

        // Assert
        Assert.True(isCompressed);
    }

    [Fact]
    public void DeflateMessageCompressor_IsCompressed_WithNonDeflateData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(_testData);

        // Assert
        Assert.False(isCompressed);
    }

    [Fact]
    public async Task DeflateMessageCompressor_DecompressInvalidData_ShouldThrow()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();
        var invalidData = Encoding.UTF8.GetBytes("This is not a valid deflate stream.");

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(async () => await compressor.DecompressAsync(invalidData));
    }
}