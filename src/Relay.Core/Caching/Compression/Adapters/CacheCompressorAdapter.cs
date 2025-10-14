using System;

namespace Relay.Core.Caching.Compression.Adapters;

/// <summary>
/// Adapter that wraps IUnifiedCompressor to implement ICacheCompressor interface.
/// </summary>
public sealed class CacheCompressorAdapter : ICacheCompressor
{
    private readonly IRelayCompressor _unifiedCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheCompressorAdapter"/> class.
    /// </summary>
    /// <param name="unifiedCompressor">The unified compressor.</param>
    public CacheCompressorAdapter(IRelayCompressor unifiedCompressor)
    {
        _unifiedCompressor = unifiedCompressor ?? throw new ArgumentNullException(nameof(unifiedCompressor));
    }

    /// <inheritdoc/>
    public byte[] Compress(byte[] data)
    {
        return _unifiedCompressor.Compress(data);
    }

    /// <inheritdoc/>
    public byte[] Decompress(byte[] compressedData)
    {
        return _unifiedCompressor.Decompress(compressedData);
    }

    /// <inheritdoc/>
    public bool ShouldCompress(int dataSize)
    {
        return _unifiedCompressor.ShouldCompress(dataSize);
    }
}