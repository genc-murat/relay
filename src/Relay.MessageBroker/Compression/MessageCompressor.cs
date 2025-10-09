using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Compression implementations using the unified compression library.
/// These classes are now thin wrappers around the unified system for backward compatibility.
/// Consider using MessageBrokerCompressionFactory directly for new code.
/// </summary>

/// <summary>
/// GZip compression implementation using the unified compression library.
/// </summary>
public sealed class GZipMessageCompressor : IMessageCompressor
{
    private readonly IMessageCompressor _innerCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="GZipMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public GZipMessageCompressor(int level = 6)
    {
        var unified = Relay.Core.Caching.Compression.CompressionFactory.CreateUnified(
            Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, level, 1024);
        _innerCompressor = new MessageCompressorAdapter(unified);
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.GZip;

    /// <inheritdoc/>
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.GZip;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.CompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        return _innerCompressor.IsCompressed(data);
    }
}

/// <summary>
/// Deflate compression implementation using the unified compression library.
/// </summary>
public sealed class DeflateMessageCompressor : IMessageCompressor
{
    private readonly IMessageCompressor _innerCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeflateMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public DeflateMessageCompressor(int level = 6)
    {
        var unified = Relay.Core.Caching.Compression.CompressionFactory.CreateUnified(
            Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate, level, 1024);
        _innerCompressor = new MessageCompressorAdapter(unified);
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Deflate;

    /// <inheritdoc/>
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.CompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        return _innerCompressor.IsCompressed(data);
    }
}

/// <summary>
/// Brotli compression implementation using the unified compression library.
/// </summary>
public sealed class BrotliMessageCompressor : IMessageCompressor
{
    private readonly IMessageCompressor _innerCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrotliMessageCompressor"/> class.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    public BrotliMessageCompressor(int level = 6)
    {
        var unified = Relay.Core.Caching.Compression.CompressionFactory.CreateUnified(
            Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli, level, 1024);
        _innerCompressor = new MessageCompressorAdapter(unified);
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => CompressionAlgorithm.Brotli;

    /// <inheritdoc/>
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.CompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        return await _innerCompressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        return _innerCompressor.IsCompressed(data);
    }
}
