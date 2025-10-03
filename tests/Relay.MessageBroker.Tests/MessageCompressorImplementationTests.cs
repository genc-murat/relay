using FluentAssertions;
using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageCompressorTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public MessageCompressorTests()
    {
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
        _smallData = Encoding.UTF8.GetBytes("Hi");
        _emptyData = Array.Empty<byte>();
    }

    #region GZipMessageCompressor Tests

    [Fact]
    public void GZipMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new GZipMessageCompressor();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.GZip);
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
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.GZip);
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
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Fact]
    public async Task GZipMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        compressed.Should().BeEmpty();
    }

    [Fact]
    public async Task GZipMessageCompressor_DecompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var decompressed = await compressor.DecompressAsync(_emptyData);

        // Assert
        decompressed.Should().BeEmpty();
    }

    [Fact]
    public async Task GZipMessageCompressor_CompressNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(null!);

        // Assert
        compressed.Should().BeNull();
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
        isCompressed.Should().BeTrue();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithNonGZipData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(_testData);

        // Assert
        isCompressed.Should().BeFalse();
    }

    [Fact]
    public void GZipMessageCompressor_IsCompressed_WithNullData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new GZipMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(null!);

        // Assert
        isCompressed.Should().BeFalse();
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
        isCompressed.Should().BeFalse();
    }

    #endregion

    #region DeflateMessageCompressor Tests

    [Fact]
    public void DeflateMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new DeflateMessageCompressor();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Deflate);
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
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Deflate);
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
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Fact]
    public async Task DeflateMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task DeflateMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        compressed.Should().BeEmpty();
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
        isCompressed.Should().BeTrue();
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
        isCompressed.Should().BeTrue();
    }

    [Fact]
    public void DeflateMessageCompressor_IsCompressed_WithNonDeflateData_ShouldReturnFalse()
    {
        // Arrange
        var compressor = new DeflateMessageCompressor();

        // Act
        var isCompressed = compressor.IsCompressed(_testData);

        // Assert
        isCompressed.Should().BeFalse();
    }

    #endregion

    #region BrotliMessageCompressor Tests

    [Fact]
    public void BrotliMessageCompressor_Constructor_WithDefaultLevel_ShouldSucceed()
    {
        // Act
        var compressor = new BrotliMessageCompressor();

        // Assert
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Brotli);
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
        compressor.Should().NotBeNull();
        compressor.Algorithm.Should().Be(CompressionAlgorithm.Brotli);
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
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Fact]
    public async Task BrotliMessageCompressor_Compress_ShouldReduceDataSize()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task BrotliMessageCompressor_CompressEmptyData_ShouldReturnEmptyData()
    {
        // Arrange
        var compressor = new BrotliMessageCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_emptyData);

        // Assert
        compressed.Should().BeEmpty();
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
        isCompressed.Should().BeTrue();
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
        isCompressed.Should().BeFalse();
    }

    #endregion

    #region CompressionStatistics Tests

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
        stats.AverageCompressionRatio.Should().Be(0);
        stats.TotalBytesSaved.Should().Be(0);
        stats.CompressionRate.Should().Be(0);
    }

    [Fact]
    public void CompressionStatistics_AverageCompressionRatio_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalOriginalBytes = 1000,
            TotalCompressedBytes = 500
        };

        // Act
        var ratio = stats.AverageCompressionRatio;

        // Assert
        ratio.Should().Be(0.5);
    }

    [Fact]
    public void CompressionStatistics_TotalBytesSaved_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalOriginalBytes = 1000,
            TotalCompressedBytes = 300
        };

        // Act
        var saved = stats.TotalBytesSaved;

        // Assert
        saved.Should().Be(700);
    }

    [Fact]
    public void CompressionStatistics_CompressionRate_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalMessages = 100,
            CompressedMessages = 75
        };

        // Act
        var rate = stats.CompressionRate;

        // Assert
        rate.Should().Be(0.75);
    }

    [Fact]
    public void CompressionStatistics_AverageCompressionTime_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            CompressedMessages = 10,
            TotalCompressionTime = TimeSpan.FromSeconds(5)
        };

        // Act
        var avgTime = stats.AverageCompressionTime;

        // Assert
        avgTime.Should().Be(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void CompressionStatistics_AverageDecompressionTime_ShouldCalculateCorrectly()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            CompressedMessages = 20,
            TotalDecompressionTime = TimeSpan.FromSeconds(4)
        };

        // Act
        var avgTime = stats.AverageDecompressionTime;

        // Assert
        avgTime.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void CompressionStatistics_WithZeroMessages_ShouldReturnZero()
    {
        // Arrange
        var stats = new CompressionStatistics
        {
            TotalMessages = 0,
            CompressedMessages = 0
        };

        // Act & Assert
        stats.AverageCompressionRatio.Should().Be(0);
        stats.CompressionRate.Should().Be(0);
        stats.AverageCompressionTime.Should().Be(TimeSpan.Zero);
        stats.AverageDecompressionTime.Should().Be(TimeSpan.Zero);
    }

    #endregion

    #region CompressionOptions Tests

    [Fact]
    public void CompressionOptions_ShouldHaveCorrectDefaults()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.Algorithm.Should().Be(CompressionAlgorithm.GZip);
        options.Level.Should().Be(6);
        options.MinimumSizeBytes.Should().Be(1024);
        options.AutoDetectCompressed.Should().BeTrue();
        options.AddMetadataHeaders.Should().BeTrue();
        options.TrackStatistics.Should().BeTrue();
        options.ExpectedCompressionRatio.Should().Be(0.7);
        options.CompressibleContentTypes.Should().BeEmpty();
        options.NonCompressibleContentTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void CompressionOptions_NonCompressibleContentTypes_ShouldContainCommonTypes()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        options.NonCompressibleContentTypes.Should().Contain("image/jpeg");
        options.NonCompressibleContentTypes.Should().Contain("image/png");
        options.NonCompressibleContentTypes.Should().Contain("video/mp4");
        options.NonCompressibleContentTypes.Should().Contain("application/zip");
    }

    [Fact]
    public void CompressionOptions_ShouldAllowCustomization()
    {
        // Act
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = CompressionAlgorithm.Brotli,
            Level = 9,
            MinimumSizeBytes = 2048,
            AutoDetectCompressed = false,
            AddMetadataHeaders = false,
            TrackStatistics = false,
            ExpectedCompressionRatio = 0.5
        };

        // Assert
        options.Enabled.Should().BeTrue();
        options.Algorithm.Should().Be(CompressionAlgorithm.Brotli);
        options.Level.Should().Be(9);
        options.MinimumSizeBytes.Should().Be(2048);
        options.AutoDetectCompressed.Should().BeFalse();
        options.AddMetadataHeaders.Should().BeFalse();
        options.TrackStatistics.Should().BeFalse();
        options.ExpectedCompressionRatio.Should().Be(0.5);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task AllCompressors_ShouldProduceDifferentResults()
    {
        // Arrange
        var gzipCompressor = new GZipMessageCompressor();
        var deflateCompressor = new DeflateMessageCompressor();
        var brotliCompressor = new BrotliMessageCompressor();

        // Act
        var gzipCompressed = await gzipCompressor.CompressAsync(_testData);
        var deflateCompressed = await deflateCompressor.CompressAsync(_testData);
        var brotliCompressed = await brotliCompressor.CompressAsync(_testData);

        // Assert - Each compressor should produce different compressed data
        gzipCompressed.Should().NotBeEquivalentTo(deflateCompressed);
        gzipCompressed.Should().NotBeEquivalentTo(brotliCompressed);
        deflateCompressed.Should().NotBeEquivalentTo(brotliCompressed);
    }

    [Fact]
    public async Task AllCompressors_ShouldDecompressToSameOriginalData()
    {
        // Arrange
        var gzipCompressor = new GZipMessageCompressor();
        var deflateCompressor = new DeflateMessageCompressor();
        var brotliCompressor = new BrotliMessageCompressor();

        // Act
        var gzipCompressed = await gzipCompressor.CompressAsync(_testData);
        var deflateCompressed = await deflateCompressor.CompressAsync(_testData);
        var brotliCompressed = await brotliCompressor.CompressAsync(_testData);

        var gzipDecompressed = await gzipCompressor.DecompressAsync(gzipCompressed);
        var deflateDecompressed = await deflateCompressor.DecompressAsync(deflateCompressed);
        var brotliDecompressed = await brotliCompressor.DecompressAsync(brotliCompressed);

        // Assert
        gzipDecompressed.Should().BeEquivalentTo(_testData);
        deflateDecompressed.Should().BeEquivalentTo(_testData);
        brotliDecompressed.Should().BeEquivalentTo(_testData);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task Compressor_WithCancellationToken_ShouldHandleCorrectly(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = CreateCompressor(algorithm);
        using var cts = new CancellationTokenSource();

        // Act
        var compressed = await compressor.CompressAsync(_testData, cts.Token);
        var decompressed = await compressor.DecompressAsync(compressed, cts.Token);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
    }

    #endregion

    private Compression.IMessageCompressor CreateCompressor(CompressionAlgorithm algorithm)
    {
        return algorithm switch
        {
            CompressionAlgorithm.GZip => new GZipMessageCompressor(),
            CompressionAlgorithm.Deflate => new DeflateMessageCompressor(),
            CompressionAlgorithm.Brotli => new BrotliMessageCompressor(),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };
    }
}
