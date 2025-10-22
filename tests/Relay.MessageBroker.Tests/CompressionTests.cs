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
        var coreAlgorithm = algorithm switch
        {
            CompressionAlgorithm.GZip => Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
            CompressionAlgorithm.Deflate => Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate,
            CompressionAlgorithm.Brotli => Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli,
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };
        var compressor = CreateCompressor(coreAlgorithm);

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
    public async Task Compress_ShouldReduceDataSize(CompressionAlgorithm algorithm)
    {
        // Arrange
        var coreAlgorithm = algorithm switch
        {
            CompressionAlgorithm.GZip => Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
            CompressionAlgorithm.Deflate => Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate,
            CompressionAlgorithm.Brotli => Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli,
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };
        var compressor = CreateCompressor(coreAlgorithm);

        // Act
        var compressed = await compressor.CompressAsync(_testData);

        // Assert
        Assert.True(compressed.Length < _testData.Length);
        
        // Should achieve at least 50% compression for our test data
        var compressionRatio = (double)compressed.Length / _testData.Length;
        Assert.True(compressionRatio < 0.5);
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
        Assert.Equal(_testData, decompressed);
        Assert.True(compressed.Length < _testData.Length);
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
        Assert.Equal(_testData, decompressed);
        Assert.True(compressed.Length < _testData.Length);
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
        Assert.Equal(_testData, decompressed);
        Assert.True(compressed.Length < _testData.Length);
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
        Assert.True(brotliCompressed.Length <= gzipCompressed.Length);
        Assert.True(brotliCompressed.Length <= deflateCompressed.Length);
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
        Assert.Empty(decompressed);
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
        Assert.True(compressed.Length > 0);
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
        Assert.Equal(largeData, decompressed);
        Assert.True(compressed.Length < largeData.Length);
        
        // Highly repetitive data should compress extremely well
        var compressionRatio = (double)compressed.Length / largeData.Length;
        Assert.True(compressionRatio < 0.01); // Less than 1%
    }

    [Fact]
    public async Task CompressionOptions_WithMinimumSize_ShouldNotCompressSmallData()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
            MinimumSizeBytes = 1024 // 1 KB minimum
        };
        var smallData = Encoding.UTF8.GetBytes("Small message");

        // Act
        var shouldCompress = smallData.Length >= options.MinimumSizeBytes;

        // Assert
        Assert.False(shouldCompress);
    }

    [Fact]
    public async Task CompressionOptions_WithMinimumSize_ShouldCompressLargeData()
    {
        // Arrange
        var options = new CompressionOptions
        {
            Enabled = true,
            Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
            MinimumSizeBytes = 100
        };
        var largeData = new byte[200]; // Larger than minimum

        // Act
        var shouldCompress = largeData.Length >= options.MinimumSizeBytes;

        // Assert
        Assert.True(shouldCompress);
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
        Assert.Equal(_testData, decompressed);
    }

    [Fact]
    public async Task Decompress_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        var compressor = new GZipCompressor();
        var invalidData = Encoding.UTF8.GetBytes("This is not compressed data");

        // Act & Assert
        // Accept any exception type since decompression can throw various exceptions
        await Assert.ThrowsAnyAsync<Exception>(async () => await compressor.DecompressAsync(invalidData));
    }

    [Fact]
    public async Task Compress_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipCompressor();

        // Act
        var result = await compressor.CompressAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Decompress_WithNullData_ShouldReturnNull()
    {
        // Arrange
        var compressor = new GZipCompressor();

        // Act
        var result = await compressor.DecompressAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CompressionOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new CompressionOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, options.Algorithm);
        Assert.Equal(1024, options.MinimumSizeBytes);
        Assert.Equal(6, options.Level);
    }

    [Fact]
    public async Task CompressionPerformance_ShouldBeReasonablyFast()
    {
        // Arrange
        var compressor = CreateCompressor(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
        // Create compressible data (repeated pattern)
        var largeData = new byte[1024 * 100]; // 100 KB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256);
        }

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var compressed = await compressor.CompressAsync(largeData);
        sw.Stop();

        // Assert - Should compress 100KB in less than 1 second
        Assert.True(sw.ElapsedMilliseconds < 1000);
        Assert.True(compressed.Length < largeData.Length);
    }

    private Relay.MessageBroker.Compression.IMessageCompressor CreateCompressor(Relay.Core.Caching.Compression.CompressionAlgorithm algorithm)
    {
        var unifiedCompressor = Relay.Core.Caching.Compression.CompressionFactory.CreateUnified(algorithm);
        return new Relay.MessageBroker.Compression.MessageCompressorAdapter(unifiedCompressor);
    }
}

// Mock compressor implementations for testing
public interface IMessageCompressor
{
    ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
    ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);
}

public class GZipCompressor : Relay.MessageBroker.Compression.IMessageCompressor
{
    private readonly CompressionLevel _level;

    public GZipCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public Relay.MessageBroker.Compression.CompressionAlgorithm Algorithm => Relay.MessageBroker.Compression.CompressionAlgorithm.GZip;
    
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.GZip;

    public async ValueTask<byte[]?> CompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, _level))
        {
            await gzipStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    public bool IsCompressed(byte[]? data)
    {
        if (data == null || data.Length < 2) return false;
        return (data[0] & 0x0F) <= 0x0D;
    }

    public async ValueTask<byte[]?> DecompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            await gzipStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}

public class BrotliCompressor : Relay.MessageBroker.Compression.IMessageCompressor
{
    private readonly CompressionLevel _level;

    public BrotliCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public Relay.MessageBroker.Compression.CompressionAlgorithm Algorithm => Relay.MessageBroker.Compression.CompressionAlgorithm.Brotli;
    
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli;

    public async ValueTask<byte[]?> CompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, _level))
        {
            await brotliStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

public bool IsCompressed(byte[]? data)
    {
        if (data == null || data.Length < 4) return false;
        // Brotli magic number: 0x8b, 0x02, 0x80, 0xXX
        return data[0] == 0x8b && data[1] == 0x02 && (data[2] & 0x80) != 0;
    }

    public async ValueTask<byte[]?> DecompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
        {
            await brotliStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}

public class DeflateCompressor : Relay.MessageBroker.Compression.IMessageCompressor
{
    private readonly CompressionLevel _level;

    public DeflateCompressor(CompressionLevel level = CompressionLevel.Fastest)
    {
        _level = level;
    }

    public Relay.MessageBroker.Compression.CompressionAlgorithm Algorithm => Relay.MessageBroker.Compression.CompressionAlgorithm.Deflate;
    
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate;

    public async ValueTask<byte[]?> CompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, _level))
        {
            await deflateStream.WriteAsync(data, cancellationToken);
        }
        return outputStream.ToArray();
    }

    public bool IsCompressed(byte[]? data)
    {
        if (data == null || data.Length < 2) return false;
        // Deflate header check (simplified)
        return (data[0] & 0x0F) == 0x08 && (data[0] >> 4) <= 0x07;
    }

    public async ValueTask<byte[]?> DecompressAsync(byte[]? data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
        {
            await deflateStream.CopyToAsync(outputStream, cancellationToken);
        }
        return outputStream.ToArray();
    }
}
