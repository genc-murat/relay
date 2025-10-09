namespace Relay.Core.Caching.Compression;

/// <summary>
/// GZIP-based cache compressor using the unified compression library.
/// </summary>
public class GzipCacheCompressor : ICacheCompressor
{
    private readonly ICacheCompressor _innerCompressor;

    /// <summary>
    /// Initializes a new instance of the <see cref="GzipCacheCompressor"/> class.
    /// </summary>
    /// <param name="compressionThreshold">Minimum data size in bytes to compress (default: 1024).</param>
    public GzipCacheCompressor(int compressionThreshold = 1024)
    {
        _innerCompressor = CompressionFactory.CreateGzipCache(compressionThreshold);
    }

    /// <summary>
    /// Compresses the given data.
    /// </summary>
    public byte[] Compress(byte[] data)
    {
        return _innerCompressor.Compress(data);
    }

    /// <summary>
    /// Decompresses the given data.
    /// </summary>
    public byte[] Decompress(byte[] compressedData)
    {
        return _innerCompressor.Decompress(compressedData);
    }

    /// <summary>
    /// Gets whether compression should be applied for the given data size.
    /// </summary>
    public bool ShouldCompress(int dataSize)
    {
        return _innerCompressor.ShouldCompress(dataSize);
    }
}