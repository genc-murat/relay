using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching.Compression;

/// <summary>
/// Unified compressor implementation supporting multiple algorithms.
/// </summary>
public sealed class RelayCompressor : IRelayCompressor
{
    private readonly CompressionOptions _options;
    private readonly CompressionLevel _compressionLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedCompressor"/> class.
    /// </summary>
    /// <param name="algorithm">The compression algorithm.</param>
    /// <param name="level">The compression level (0-9).</param>
    /// <param name="minimumSizeBytes">Minimum data size to compress.</param>
    public RelayCompressor(CompressionAlgorithm algorithm = CompressionAlgorithm.GZip, int level = 6, int minimumSizeBytes = 1024)
    {
        _options = new CompressionOptions
        {
            Algorithm = algorithm,
            Level = level,
            MinimumSizeBytes = minimumSizeBytes
        };

        _compressionLevel = level switch
        {
            <= 3 => CompressionLevel.Fastest,
            >= 7 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedCompressor"/> class.
    /// </summary>
    /// <param name="options">The compression options.</param>
    public RelayCompressor(CompressionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _compressionLevel = options.Level switch
        {
            <= 3 => CompressionLevel.Fastest,
            >= 7 => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Optimal
        };
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => _options.Algorithm;

    /// <inheritdoc/>
    public byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return data;

        using var outputStream = new MemoryStream();
        using var compressionStream = CreateCompressionStream(outputStream, CompressionMode.Compress);
        compressionStream.Write(data, 0, data.Length);
        compressionStream.Close(); // Ensure the stream is properly closed
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return data;

        using var outputStream = new MemoryStream();
        await using var compressionStream = CreateCompressionStream(outputStream, CompressionMode.Compress);
        await compressionStream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
        await compressionStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public byte[] Decompress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return data;

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        using var compressionStream = CreateCompressionStream(inputStream, CompressionMode.Decompress);
        compressionStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (data.Length == 0) return data;

        using var inputStream = new MemoryStream(data);
        using var outputStream = new MemoryStream();
        await using var compressionStream = CreateCompressionStream(inputStream, CompressionMode.Decompress);
        await compressionStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        if (data == null || data.Length < 2)
        {
            return false;
        }

        return _options.Algorithm switch
        {
            CompressionAlgorithm.GZip => data[0] == 0x1f && data[1] == 0x8b,
            CompressionAlgorithm.Deflate => data[0] == 0x78 && (data[1] == 0x9c || data[1] == 0xda || data[1] == 0x01),
            CompressionAlgorithm.Brotli => (data[0] & 0x0F) <= 0x0D,
            _ => false
        };
    }

    /// <inheritdoc/>
    public bool ShouldCompress(int dataSize)
    {
        return dataSize >= _options.MinimumSizeBytes;
    }

    private Stream CreateCompressionStream(Stream stream, CompressionMode mode)
    {
        return _options.Algorithm switch
        {
            CompressionAlgorithm.GZip => mode == CompressionMode.Compress 
                ? new GZipStream(stream, _compressionLevel) 
                : new GZipStream(stream, mode),
            CompressionAlgorithm.Deflate => mode == CompressionMode.Compress 
                ? new DeflateStream(stream, _compressionLevel) 
                : new DeflateStream(stream, mode),
            CompressionAlgorithm.Brotli => mode == CompressionMode.Compress 
                ? new BrotliStream(stream, _compressionLevel) 
                : new BrotliStream(stream, mode),
            CompressionAlgorithm.LZ4 => throw new NotSupportedException("LZ4 compression is not yet implemented"),
            CompressionAlgorithm.Zstd => throw new NotSupportedException("Zstd compression is not yet implemented"),
            _ => throw new NotSupportedException($"Compression algorithm {_options.Algorithm} is not supported")
        };
    }
}

/// <summary>
/// Extension methods for disposable pattern.
/// </summary>
internal static class DisposableExtensions
{
    public static void DisposeAfter<T>(this T disposable, Action<T> action) where T : IDisposable
    {
        using (disposable)
        {
            action(disposable);
        }
    }
}