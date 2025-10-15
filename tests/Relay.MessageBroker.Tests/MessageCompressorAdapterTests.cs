using Relay.MessageBroker.Compression;
using Relay.Core.Caching.Compression;
using Moq;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageCompressorAdapterTests
{
    private readonly Mock<IRelayCompressor> _mockUnifiedCompressor;

    public MessageCompressorAdapterTests()
    {
        _mockUnifiedCompressor = new Mock<IRelayCompressor>();
    }

    [Fact]
    public void Constructor_WithNullUnifiedCompressor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageCompressorAdapter(null!));
    }

    [Fact]
    public void Constructor_WithValidUnifiedCompressor_ShouldCreateAdapter()
    {
        // Arrange
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);

        // Act
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Assert
        Assert.NotNull(adapter);
        Assert.Equal(Relay.MessageBroker.Compression.CompressionAlgorithm.GZip, adapter.Algorithm);
    }

    [Theory]
    [InlineData(Relay.MessageBroker.Compression.CompressionAlgorithm.GZip, Relay.Core.Caching.Compression.CompressionAlgorithm.GZip)]
    [InlineData(Relay.MessageBroker.Compression.CompressionAlgorithm.Deflate, Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate)]
    [InlineData(Relay.MessageBroker.Compression.CompressionAlgorithm.Brotli, Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli)]
    [InlineData(Relay.MessageBroker.Compression.CompressionAlgorithm.None, Relay.Core.Caching.Compression.CompressionAlgorithm.None)]
    public void Algorithm_ShouldMapCorrectlyFromCoreAlgorithm(
        Relay.MessageBroker.Compression.CompressionAlgorithm expectedAlgorithm,
        Relay.Core.Caching.Compression.CompressionAlgorithm coreAlgorithm)
    {
        // Arrange
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(coreAlgorithm);

        // Act
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Assert
        Assert.Equal(expectedAlgorithm, adapter.Algorithm);
    }

    [Fact]
    public void CoreAlgorithm_ShouldReturnUnifiedCompressorAlgorithm()
    {
        // Arrange
        var expectedAlgorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli;
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(expectedAlgorithm);
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var coreAlgorithm = adapter.CoreAlgorithm;

        // Assert
        Assert.Equal(expectedAlgorithm, coreAlgorithm);
    }

    [Fact]
    public async Task CompressAsync_WithValidData_ShouldCallUnifiedCompressor()
    {
        // Arrange
        var inputData = new byte[] { 1, 2, 3, 4, 5 };
        var compressedData = new byte[] { 10, 20, 30 };
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.CompressAsync(inputData, CancellationToken.None))
            .ReturnsAsync(compressedData);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.CompressAsync(inputData);

        // Assert
        Assert.Equal(compressedData, result);
        _mockUnifiedCompressor.Verify(c => c.CompressAsync(inputData, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task CompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.CompressAsync(null);

        // Assert
        Assert.Null(result);
        _mockUnifiedCompressor.Verify(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompressAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var inputData = new byte[] { 1, 2, 3 };
        var compressedData = new byte[] { 4, 5, 6 };
        var cancellationToken = new CancellationToken(true);
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.CompressAsync(inputData, cancellationToken))
            .ReturnsAsync(compressedData);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.CompressAsync(inputData, cancellationToken);

        // Assert
        Assert.Equal(compressedData, result);
        _mockUnifiedCompressor.Verify(c => c.CompressAsync(inputData, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task DecompressAsync_WithValidData_ShouldCallUnifiedCompressor()
    {
        // Arrange
        var compressedData = new byte[] { 10, 20, 30 };
        var decompressedData = new byte[] { 1, 2, 3, 4, 5 };
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.DecompressAsync(compressedData, CancellationToken.None))
            .ReturnsAsync(decompressedData);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.DecompressAsync(compressedData);

        // Assert
        Assert.Equal(decompressedData, result);
        _mockUnifiedCompressor.Verify(c => c.DecompressAsync(compressedData, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task DecompressAsync_WithNullData_ShouldReturnNull()
    {
        // Arrange
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.DecompressAsync(null);

        // Assert
        Assert.Null(result);
        _mockUnifiedCompressor.Verify(c => c.DecompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DecompressAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var compressedData = new byte[] { 4, 5, 6 };
        var decompressedData = new byte[] { 1, 2, 3 };
        var cancellationToken = new CancellationToken(true);
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.DecompressAsync(compressedData, cancellationToken))
            .ReturnsAsync(decompressedData);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = await adapter.DecompressAsync(compressedData, cancellationToken);

        // Assert
        Assert.Equal(decompressedData, result);
        _mockUnifiedCompressor.Verify(c => c.DecompressAsync(compressedData, cancellationToken), Times.Once);
    }

    [Theory]
    [InlineData(new byte[] { 1, 2, 3, 4, 5 }, true)]
    [InlineData(new byte[] { 31, 139, 8, 0, 0, 0, 0, 0, 4, 0 }, true)] // GZip header
    [InlineData(new byte[] { 1, 2, 3 }, false)]
    public void IsCompressed_ShouldDelegateToUnifiedCompressor(byte[] data, bool expectedResult)
    {
        // Arrange
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor.Setup(c => c.IsCompressed(data)).Returns(expectedResult);
        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act
        var result = adapter.IsCompressed(data);

        // Assert
        Assert.Equal(expectedResult, result);
        _mockUnifiedCompressor.Verify(c => c.IsCompressed(data), Times.Once);
    }

    [Fact]
    public async Task CompressAsync_WhenUnifiedCompressorThrows_ShouldPropagateException()
    {
        // Arrange
        var inputData = new byte[] { 1, 2, 3 };
        var expectedException = new InvalidOperationException("Compression failed");
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.CompressAsync(inputData, CancellationToken.None))
            .ThrowsAsync(expectedException);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.CompressAsync(inputData));
        Assert.Equal("Compression failed", exception.Message);
    }

    [Fact]
    public async Task DecompressAsync_WhenUnifiedCompressorThrows_ShouldPropagateException()
    {
        // Arrange
        var compressedData = new byte[] { 10, 20, 30 };
        var expectedException = new InvalidOperationException("Decompression failed");
        _mockUnifiedCompressor.Setup(c => c.Algorithm).Returns(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        _mockUnifiedCompressor
            .Setup(c => c.DecompressAsync(compressedData, CancellationToken.None))
            .ThrowsAsync(expectedException);

        var adapter = new MessageCompressorAdapter(_mockUnifiedCompressor.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await adapter.DecompressAsync(compressedData));
        Assert.Equal("Decompression failed", exception.Message);
    }
}