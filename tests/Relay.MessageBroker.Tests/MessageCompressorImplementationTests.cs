using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageCompressorImplementationTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public MessageCompressorImplementationTests()
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

    #endregion

    #region DeflateMessageCompressor Tests

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

    #endregion

    #region BrotliMessageCompressor Tests

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

    #endregion

    #region CompressionStatistics Tests

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
        Assert.Equal(0, stats.AverageCompressionRatio);
        Assert.Equal(0L, stats.TotalBytesSaved);
        Assert.Equal(0, stats.CompressionRate);
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
        Assert.Equal(0.5, ratio);
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
        Assert.Equal(700L, saved);
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
        Assert.Equal(0.75, rate);
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
        Assert.Equal(TimeSpan.FromMilliseconds(500), avgTime);
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
        Assert.Equal(TimeSpan.FromMilliseconds(200), avgTime);
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
        Assert.Equal(0, stats.AverageCompressionRatio);
        Assert.Equal(0, stats.CompressionRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageCompressionTime);
        Assert.Equal(TimeSpan.Zero, stats.AverageDecompressionTime);
    }

    #endregion

    #region CompressionOptions Tests

    [Fact]
    public void CompressionOptions_ShouldHaveCorrectDefaults()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, options.Algorithm);
        Assert.Equal(6, options.Level);
        Assert.Equal(1024, options.MinimumSizeBytes);
        Assert.True(options.AutoDetectCompressed);
        Assert.True(options.AddMetadataHeaders);
        Assert.True(options.TrackStatistics);
        Assert.Equal(0.7, options.ExpectedCompressionRatio);
        Assert.Empty(options.CompressibleContentTypes);
        Assert.NotEmpty(options.NonCompressibleContentTypes);
    }

    [Fact]
    public void CompressionOptions_NonCompressibleContentTypes_ShouldContainCommonTypes()
    {
        // Act
        var options = new CompressionOptions();

        // Assert
        Assert.Contains("image/jpeg", options.NonCompressibleContentTypes);
        Assert.Contains("image/png", options.NonCompressibleContentTypes);
        Assert.Contains("video/mp4", options.NonCompressibleContentTypes);
        Assert.Contains("application/zip", options.NonCompressibleContentTypes);
    }

    [Fact]
    public void CompressionOptions_ShouldAllowCustomization()
    {
        // Act
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli,
            Level = 9,
            MinimumSizeBytes = 2048,
            AutoDetectCompressed = false,
            AddMetadataHeaders = false,
            TrackStatistics = false,
            ExpectedCompressionRatio = 0.5
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, options.Algorithm);
        Assert.Equal(9, options.Level);
        Assert.Equal(2048, options.MinimumSizeBytes);
        Assert.False(options.AutoDetectCompressed);
        Assert.False(options.AddMetadataHeaders);
        Assert.False(options.TrackStatistics);
        Assert.Equal(0.5, options.ExpectedCompressionRatio);
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
        Assert.NotEqual(deflateCompressed, gzipCompressed);
        Assert.NotEqual(brotliCompressed, gzipCompressed);
        Assert.NotEqual(brotliCompressed, deflateCompressed);
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
        Assert.Equal(_testData, gzipDecompressed);
        Assert.Equal(_testData, deflateDecompressed);
        Assert.Equal(_testData, brotliDecompressed);
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
        Assert.Equal(_testData, decompressed);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task Compressor_WithCanceledToken_ShouldThrowOperationCanceledException(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = CreateCompressor(algorithm);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        // TaskCanceledException derives from OperationCanceledException, so we accept both
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await compressor.CompressAsync(_testData, cts.Token));
        Assert.True(exception is OperationCanceledException);
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