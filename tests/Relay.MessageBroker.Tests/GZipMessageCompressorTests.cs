using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

#pragma warning disable CS8604 // Possible null reference argument

namespace Relay.MessageBroker.Tests;

public class GZipMessageCompressorTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public GZipMessageCompressorTests()
    {
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
        _smallData = Encoding.UTF8.GetBytes("Hi");
        _emptyData = Array.Empty<byte>();
    }

    [Fact]
    public void GZipMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new GZipMessageCompressor();

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.GZip, compressor.Algorithm);
    }

    [Theory]
    [InlineData(0)]  // Fastest
    [InlineData(3)]  // Fastest
    [InlineData(5)]  // Optimal
    [InlineData(6)]  // Optimal
    [InlineData(7)]  // SmallestSize
    [InlineData(9)]  // SmallestSize
    public void GZipMessageCompressor_Constructor_WithDifferentLevels_ShouldSucceed(int level)
    {
        // Act
        var compressor = new GZipMessageCompressor(level);

        // Assert
        Assert.NotNull(compressor);
        Assert.Equal(CompressionAlgorithm.GZip, compressor.Algorithm);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressAndDecompress_ShouldPreserveData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        Assert.Equal(_testData, decompressed);
    }

    [Fact]
    public async Task GZipMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        Assert.True(compressed.Length < _testData.Length);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        Assert.Empty(compressed);
    }

    [Fact]
    public async Task GZipMessageCompressor_DecompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var decompressed = await compressor.DecompressAsync(_emptyData);

        // Assert
        Assert.Empty(decompressed);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(null!);

        // Assert
        Assert.Null(compressed);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithGZipData_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var gzipData = new byte[] { 0x1f, 0x8b, 0x08, 0x00 }; // GZip magic number

        // Act
        var isCompressed = compressor.IsCompressed(gzipData);

        // Assert
        Assert.True(isCompressed);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithNonGZipData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(_testData);

        // Assert
        Assert.False(isCompressed);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithNullData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(null!);

        // Assert
        Assert.False(isCompressed);
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithShortData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var shortData = new byte[] { 0x1f }; // Only 1 byte

        // Act
        var isCompressed = compressor.IsCompressed(shortData);

        // Assert
        Assert.False(isCompressed);
    }

    [Fact]
    public async Task GZipMessageCompressor_DecompressInvalidData_ShouldThrow()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();
        var invalidData = Encoding.UTF8.GetBytes("This is not a valid gzip stream.");

        // Act & Assert
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(async () => await compressor.DecompressAsync(invalidData));
    }
}