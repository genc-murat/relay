using System;
using System.Linq;
using FluentAssertions;
using Relay.Core.Caching.Compression;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression;

public class UnifiedCompressorTests
{
    private readonly byte[] _testData;

    public UnifiedCompressorTests()
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
        var compressor = new UnifiedCompressor(algorithm);

        // Assert
        compressor.Algorithm.Should().Be(algorithm);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_And_Decompress_ShouldReturnOriginalData(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);

        // Act
        var compressed = compressor.Compress(_testData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task CompressAsync_And_DecompressAsync_ShouldReturnOriginalData(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_ShouldReduceDataSize(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);

        // Act
        var compressed = compressor.Compress(_testData);

        // Assert
        compressed.Length.Should().BeLessThan(_testData.Length);
        
        // Should achieve at least 50% compression for our test data
        var compressionRatio = (double)compressed.Length / _testData.Length;
        compressionRatio.Should().BeLessThan(0.5);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void ShouldCompress_WithLargeData_ShouldReturnTrue(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm, 6, 100);
        var largeData = new byte[200];

        // Act
        var shouldCompress = compressor.ShouldCompress(largeData.Length);

        // Assert
        shouldCompress.Should().BeTrue();
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void ShouldCompress_WithSmallData_ShouldReturnFalse(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm, 6, 100);
        var smallData = new byte[50];

        // Act
        var shouldCompress = compressor.ShouldCompress(smallData.Length);

        // Assert
        shouldCompress.Should().BeFalse();
    }

    [Fact]
    public void GZipCompressor_IsCompressed_WithGZipMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var data = new byte[] { 0x1f, 0x8b, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void DeflateCompressor_IsCompressed_WithDeflateMagicNumber_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new UnifiedCompressor(CompressionAlgorithm.Deflate);
        var data = new byte[] { 0x78, 0x9c, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void BrotliCompressor_IsCompressed_WithValidPattern_ShouldReturnTrue()
    {
        // Arrange
        var compressor = new UnifiedCompressor(CompressionAlgorithm.Brotli);
        var data = new byte[] { 0x8b, 0x02, 0x00, 0x00 };

        // Act
        var result = compressor.IsCompressed(data);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);

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
        var compressor = new UnifiedCompressor(algorithm);

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
        var compressor = new UnifiedCompressor(algorithm);

        // Act
        Func<Task> act = async () => await compressor.CompressAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task DecompressAsync_WithNullData_ShouldThrowArgumentNullException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);

        // Act
        Func<Task> act = async () => await compressor.DecompressAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void Compress_WithEmptyData_ShouldReturnEmpty(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);
        var emptyData = Array.Empty<byte>();

        // Act
        var compressed = compressor.Compress(emptyData);

        // Assert
        compressed.Should().BeEmpty();
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task CompressAsync_WithEmptyData_ShouldReturnEmpty(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = new UnifiedCompressor(algorithm);
        var emptyData = Array.Empty<byte>();

        // Act
        var compressed = await compressor.CompressAsync(emptyData);

        // Assert
        compressed.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithUnsupportedAlgorithm_ShouldThrowNotSupportedException()
    {
        // Arrange & Act
        var compressor = new UnifiedCompressor(CompressionAlgorithm.LZ4);

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
        var compressor = new UnifiedCompressor(options);

        // Assert
        compressor.Algorithm.Should().Be(CompressionAlgorithm.GZip);
        compressor.ShouldCompress(3000).Should().BeTrue();
        compressor.ShouldCompress(1000).Should().BeFalse();
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void Compress_Decompress_RoundTrip_ShouldPreserveData(int dataSize)
    {
        // Arrange
        var compressor = new UnifiedCompressor(CompressionAlgorithm.GZip);
        var random = new Random(42); // Fixed seed for reproducible tests
        var originalData = new byte[dataSize];
        random.NextBytes(originalData);

        // Act
        var compressed = compressor.Compress(originalData);
        var decompressed = compressor.Decompress(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(originalData);
    }
}