using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Adapter that wraps IUnifiedCompressor to implement IMessageCompressor interface.
/// </summary>
public sealed class MessageCompressorAdapter : IMessageCompressor
{
    private readonly IUnifiedCompressor _unifiedCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageCompressorAdapter"/> class.
    /// </summary>
    /// <param name="unifiedCompressor">The unified compressor.</param>
    public MessageCompressorAdapter(IUnifiedCompressor unifiedCompressor)
    {
        _unifiedCompressor = unifiedCompressor ?? throw new ArgumentNullException(nameof(unifiedCompressor));
    }

    /// <inheritdoc/>
    public CompressionAlgorithm Algorithm => 
        (CompressionAlgorithm)Enum.Parse(
            typeof(CompressionAlgorithm), 
            _unifiedCompressor.Algorithm.ToString());

    /// <summary>
    /// Gets the core compression algorithm.
    /// </summary>
    public Relay.Core.Caching.Compression.CompressionAlgorithm CoreAlgorithm => _unifiedCompressor.Algorithm;

    /// <inheritdoc/>
    public async ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;
        return await _unifiedCompressor.CompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        if (data == null) return null;
        return await _unifiedCompressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool IsCompressed(byte[] data)
    {
        return _unifiedCompressor.IsCompressed(data);
    }
}