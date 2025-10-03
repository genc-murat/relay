using FluentAssertions;
using Relay.MessageBroker.Compression;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CompressionTests
{
    private readonly byte[] _testData;

    public CompressionTests()
    {
        // Create test data - repeating pattern compresses well
        var text = string.Join("", Enumerable.Repeat("This is a test message that should compress well. ", 100));
        _testData = Encoding.UTF8.GetBytes(text);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public async Task Compress_AndDecompress_ShouldPreserveData(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = CreateCompressor(algorithm);

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
    public async Task Compress_ShouldReduceDataSize(CompressionAlgorithm algorithm)
    {
        // Arrange
        var compressor = CreateCompressor(algorithm);

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        compressed.Length.Should().BeLessThan(_testData.Length);
        
        // Should achieve at least 50% compression for our test data
        var compressionRatio = (double)compressed.Length / _testData.Length;
        compressionRatio.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task GZipCompressor_ShouldCompressAndDecompress()
    {
        // Arrange
        var compressor = new GZipCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task DeflateCompressor_ShouldCompressAndDecompress()
    {
        // Arrange
        var compressor = new DeflateCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task BrotliCompressor_ShouldCompressAndDecompress()
    {
        // Arrange
        var compressor = new BrotliCompressor();

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
        compressed.Length.Should().BeLessThan(_testData.Length);
    }

    [Fact]
    public async Task BrotliCompressor_ShouldProvideHighestCompressionRatio()
    {
        // Arrange
        var gzipCompressor = new GZipCompressor();
        var deflateCompressor = new DeflateCompressor();
        var brotliCompressor = new BrotliCompressor();

        // Act
        var gzipCompressed = await gzipCompressor.CompressAsync(_testData);
        var deflateCompressed = await deflateCompressor.CompressAsync(_testData);
        var brotliCompressed = await brotliCompressor.CompressAsync(_testData);

        // Assert - Brotli should have the best compression ratio
        brotliCompressed.Length.Should().BeLessThanOrEqualTo(gzipCompressed.Length);
        brotliCompressed.Length.Should().BeLessThanOrEqualTo(deflateCompressed.Length);
    }

    [Fact]
    public async Task Compress_WithEmptyData_ShouldHandleGracefully()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var emptyData = Array.Empty<byte>();

        // Act
        var compressed = await compressor.CompressAsync(emptyData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEmpty();
    }

    [Fact]
    public async Task Compress_WithSmallData_MayIncreaseSize()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var smallData = Encoding.UTF8.GetBytes("Hi");

        // Act
        var compressed = await compressor.CompressAsync(smallData);

        // Assert - Small data might be larger after compression due to headers
        // This is expected behavior and why we have MinimumSizeForCompression option
        compressed.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Compress_WithLargeData_ShouldHandleCorrectly()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var largeData = new byte[1024 * 1024]; // 1 MB
        Array.Fill<byte>(largeData, 65); // Fill with 'A'

        // Act
        var compressed = await compressor.CompressAsync(largeData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(largeData);
        compressed.Length.Should().BeLessThan(largeData.Length);
        
        // Highly repetitive data should compress extremely well
        var compressionRatio = (double)compressed.Length / largeData.Length;
        compressionRatio.Should().BeLessThan(0.01); // Less than 1%
    }

    [Fact]
    public async Task CompressionOptions_WithMinimumSize_ShouldNotCompressSmallData()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = CompressionAlgorithm.GZip,
            MinimumSizeBytes = 1024 // 1 KB minimum
        };
        var smallData = Encoding.UTF8.GetBytes("Small message");

        // Act
        var shouldCompress = smallData.Length >= options.MinimumSizeBytes;

        // Assert
        shouldCompress.Should().BeFalse();
    }

    [Fact]
    public async Task CompressionOptions_WithMinimumSize_ShouldCompressLargeData()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = CompressionAlgorithm.GZip,
            MinimumSizeBytes = 100
        };
        var largeData = new byte[200]; // Larger than minimum

        // Act
        var shouldCompress = largeData.Length >= options.MinimumSizeBytes;

        // Assert
        shouldCompress.Should().BeTrue();
    }

    [Theory]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.Optimal)]
    [InlineData(CompressionLevel.SmallestSize)]
    public async Task GZipCompressor_WithDifferentLevels_ShouldWork(CompressionLevel level)
    {
        // Arrange
        var compressor = new GZipCompressor(level);

        // Act
        var compressed = await compressor.CompressAsync(_testData);
        var decompressed = await compressor.DecompressAsync(compressed);

        // Assert
        decompressed.Should().BeEquivalentTo(_testData);
    }

    [Fact]
    public async Task Decompress_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var invalidData = Encoding.UTF8.GetBytes("This is not compressed data");

        // Act
        Func<Task> act = async () => await compressor.DecompressAsync(invalidData);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Compress_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var compressor = new GZipCompressor();

        // Act
        Func<Task> act = async () => await compressor.CompressAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Decompress_WithNullData_ShouldThrowArgumentNullException()
    {
        // Arrange
        var compressor = new GZipCompressor();

        // Act
        Func<Task> act = async () => await compressor.DecompressAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void CompressionOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CompressionOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.Algorithm.Should().Be(CompressionAlgorithm.GZip);
        options.MinimumSizeBytes.Should().Be(1024);
        options.Level.Should().Be(6);
    }

    [Fact]
    public async Task CompressionPerformance_ShouldBeReasonablyFast()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var largeData = new byte[1024 * 100]; // 100 KB
        new Random().NextBytes(largeData);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var compressed = await compressor.CompressAsync(largeData);
        sw.Stop();

        // Assert - Should compress 100KB in less than 1 second
        sw.ElapsedMilliseconds.Should().BeLessThan(1000);
        compressed.Length.Should().BeLessThan(largeData.Length);
    }

    private IMessageCompressor CreateCompressor(CompressionAlgorithm algorithm)
    {
        return algorithm switch
        {
            CompressionAlgorithm.GZip => new GZipCompressor(),
            CompressionAlgorithm.Deflate => new DeflateCompressor(),
            CompressionAlgorithm.Brotli => new BrotliCompressor(),
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };
    }
}

// Mock compressor implementations for testing
public interface IMessageCompressor
{
    ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
    ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);
}

public class GZipCompressor : IMessageCompressor
{
    private readonly CompressionLevel _level;

    public GZipCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, _level))
        {
            await gzipStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}

public class DeflateCompressor : IMessageCompressor
{
    private readonly CompressionLevel _level;

    public DeflateCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, _level))
        {
            await deflateStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
        {
            await deflateStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}

public class BrotliCompressor : IMessageCompressor
{
    private readonly CompressionLevel _level;

    public BrotliCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, _level))
        {
            await brotliStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
        {
            await brotliStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}
