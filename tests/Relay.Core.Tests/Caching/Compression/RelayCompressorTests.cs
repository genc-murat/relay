using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Relay.Core.Caching.Compression;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression;

public class RelayCompressorTests
{
    private readonly byte[] _testData;

    public RelayCompressorTests()
    {
        // Create test data - repeating pattern compresses well
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Constructor_WithAlgorithm_ShouldSetCorrectAlgorithm(CompressionAlgorithm algorithm)
    {
        // Arrange & Act
        var compressor = new RelayCompressor(algorithm);

        // Assert
        Assert.Equal(algorithm, compressor.Algorithm);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_And_Decompress_ShouldReturnOriginalData(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act
        var compressed = compressor.Compress(_testData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        Assert.Equal(_testData, decompressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task CompressAsync_And_DecompressAsync_ShouldReturnOriginalData(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        Assert.Equal(_testData, decompressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_ShouldReduceDataSize(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act
        var compressed = compressor.Compress(_testData);

        // Assert
        Assert.True(compressed.Length < _testData.Length);

        // Should achieve at least 50% compression for our test data
        var compressionRatio = (double)compressed.Length / _testData.Length;
        Assert.True(compressionRatio < 0.5);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void ShouldCompress_WithLargeData_ShouldReturnTrue(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm, 6, 100);
        var largeData = new byte[200];

        // Act
        var shouldCompress = compressor.ShouldCompress(largeData.Length);

        // Assert
        Assert.True(shouldCompress);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void ShouldCompress_WithSmallData_ShouldReturnFalse(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm, 6, 100);
        var smallData = new byte[50];

        // Act
        var shouldCompress = compressor.ShouldCompress(smallData.Length);

        // Assert
        Assert.False(shouldCompress);
    }

    [Fact]
    public void GZipCompressor_IsCompressed_WithGZipMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new RelayCompressor(CompressionAlgorithm.GZip);
        var data = new byte[] { 0x1f, 0x8b, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DeflateCompressor_IsCompressed_WithDeflateMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new RelayCompressor(CompressionAlgorithm.Deflate);
        var data = new byte[] { 0x78, 0x9c, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void BrotliCompressor_IsCompressed_WithValidPattern_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new RelayCompressor(CompressionAlgorithm.Brotli);
        var data = new byte[] { 0x8b, 0x02, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => compressor.Compress(null!));
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Decompress_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => compressor.Decompress(null!));
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task CompressAsync_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await compressor.CompressAsync(null!));
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task DecompressAsync_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await compressor.DecompressAsync(null!));
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_WithEmptyData_ShouldReturnEmpty(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);
        var emptyData = Array.Empty<byte>();

        // Act
        var compressed = compressor.Compress(emptyData);

        // Assert
        Assert.Empty(compressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task CompressAsync_WithEmptyData_ShouldReturnEmpty(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new RelayCompressor(algorithm);
        var emptyData = Array.Empty<byte>();

        // Act
        var compressed = await compressor.CompressAsync(emptyData);

        // Assert
        Assert.Empty(compressed);
    }

    [Fact]
    public void Constructor_WithUnsupportedAlgorithm_ShouldThrowNotSupportedException()
    {
        // Arrange & Act
        var compressor = new RelayCompressor(CompressionAlgorithm.LZ4);

        // Assert
        Assert.Throws<NotSupportedException>(() => compressor.Compress(_testData));
    }

    [Fact]
    public void Constructor_WithCompressionOptions_ShouldUseOptions()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Algorithm = CompressionAlgorithm.GZip,
            Level = 9,
            MinimumSizeBytes = 2048
        };

        // Act
        var compressor = new RelayCompressor(options);

        // Assert
        Assert.Equal(CompressionAlgorithm.GZip, compressor.Algorithm);
        Assert.True(compressor.ShouldCompress(3000));
        Assert.False(compressor.ShouldCompress(1000));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Compress_Decompress_RoundTrip_ShouldPreserveData(int dataSize)
    {
        // Arrange
        var compressor = new RelayCompressor(CompressionAlgorithm.GZip);
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

