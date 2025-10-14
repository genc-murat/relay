using Relay.Core.Caching.Compression.Adapters;

namespace Relay.Core.Caching.Compression;

/// <summary>
/// Factory for creating compression instances.
/// </summary>
public static class CompressionFactory
{
    /// <summary>
    /// Creates a unified compressor with the specified algorithm and options.
    /// </summary>
    /// <param name="algorithm">The compression algorithm.</param>
    /// <param name="level">The compression level (0-9).</param>
    /// <param name="minimumSizeBytes">Minimum data size to compress.</param>
    /// <returns>A unified compressor instance.</returns>
    public static IRelayCompressor CreateUnified(
        CompressionAlgorithm algorithm = CompressionAlgorithm.GZip, 
        int level = 6, 
        int minimumSizeBytes = 1024)
    {
        return new RelayCompressor(algorithm, level, minimumSizeBytes);
    }

    /// <summary>
    /// Creates a unified compressor with the specified options.
    /// </summary>
    /// <param name="options">The compression options.</param>
    /// <returns>A unified compressor instance.</returns>
    public static IRelayCompressor CreateUnified(CompressionOptions options)
    {
        return new RelayCompressor(options);
    }

    /// <summary>
    /// Creates a cache compressor adapter.
    /// </summary>
    /// <param name="algorithm">The compression algorithm.</param>
    /// <param name="level">The compression level (0-9).</param>
    /// <param name="minimumSizeBytes">Minimum data size to compress.</param>
    /// <returns>A cache compressor instance.</returns>
    public static ICacheCompressor CreateCache(
        CompressionAlgorithm algorithm = CompressionAlgorithm.GZip, 
        int level = 6, 
        int minimumSizeBytes = 1024)
    {
        var unified = CreateUnified(algorithm, level, minimumSizeBytes);
        return new CacheCompressorAdapter(unified);
    }

    /// <summary>
    /// Creates a cache compressor adapter with custom options.
    /// </summary>
    /// <param name="options">The compression options.</param>
    /// <returns>A cache compressor instance.</returns>
    public static ICacheCompressor CreateCache(CompressionOptions options)
    {
        var unified = CreateUnified(options);
        return new CacheCompressorAdapter(unified);
    }



    /// <summary>
    /// Creates a GZip cache compressor (for backward compatibility).
    /// </summary>
    /// <param name="compressionThreshold">Minimum data size in bytes to compress.</param>
    /// <returns>A GZip cache compressor.</returns>
    public static ICacheCompressor CreateGzipCache(int compressionThreshold = 1024)
    {
        return CreateCache(CompressionAlgorithm.GZip, 6, compressionThreshold);
    }
}