using Relay.MessageBroker.Compression;
using System.Text;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageCompressorIntegrationTests
{
    private readonly byte[] _testData;
    private readonly byte[] _smallData;
    private readonly byte[] _emptyData;

    public MessageCompressorIntegrationTests()
    {
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
        _smallData = Encoding.UTF8.GetBytes("Hi");
        _emptyData = Array.Empty<byte>();
    }

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