using System.IO.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// GZip compression implementation.
/// </summary>
public sealed class GZipMessageCompressor : IMessageCompressor
{
    private readonly CompressionLevel _compressionLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GZipMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public GZipMessageCompressor(int level = 6)
    {
        _compressionLevel = level switch
        {
            <= 3 => CompressionLevel.Fastest,
            >= 7 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.GZip;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var outputStream = new MemoryStream();
        using (var gzipStream = new GZipStream(outputStream, _compressionLevel, leaveOpen: true))
        {
            await gzipStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }

        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        
        await gzipStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        if (data == null || data.Length < 2)
        {
            return false;
        }

        // GZip magic number: 0x1f, 0x8b
        return data[0] == 0x1f && data[1] == 0x8b;
    }
}

/// <summary>
/// Deflate compression implementation.
/// </summary>
public sealed class DeflateMessageCompressor : IMessageCompressor
{
    private readonly CompressionLevel _compressionLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeflateMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public DeflateMessageCompressor(int level = 6)
    {
        _compressionLevel = level switch
        {
            <= 3 => CompressionLevel.Fastest,
            >= 7 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(outputStream, _compressionLevel, leaveOpen: true))
        {
            await deflateStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }

        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress);
        
        await deflateStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        if (data == null || data.Length < 2)
        {
            return false;
        }

        // Deflate magic number: 0x78, 0x9c (default) or 0x78, 0xda (best compression)
        return data[0] == 0x78 && (data[1] == 0x9c || data[1] == 0xda || data[1] == 0x01);
    }
}

/// <summary>
/// Brotli compression implementation.
/// </summary>
public sealed class BrotliMessageCompressor : IMessageCompressor
{
    private readonly CompressionLevel _compressionLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrotliMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public BrotliMessageCompressor(int level = 6)
    {
        _compressionLevel = level switch
        {
            <= 3 => CompressionLevel.Fastest,
            >= 7 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Brotli;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var outputStream = new MemoryStream();
        using (var brotliStream = new BrotliStream(outputStream, _compressionLevel, leaveOpen: true))
        {
            await brotliStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        }

        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null || data.Length == 0)
        {
            return data;
        }

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        
        await brotliStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        // Brotli doesn't have a clear magic number, but we can check for typical patterns
        // This is a simple heuristic and may not be 100% accurate
        if (data == null || data.Length < 1)
        {
            return false;
        }

        // Brotli streams typically start with specific bit patterns
        // This is a simplified check
        return (data[0] & 0x0F) <= 0x0D;
    }
}
