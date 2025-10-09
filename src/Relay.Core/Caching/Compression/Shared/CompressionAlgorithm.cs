namespace Relay.Core.Caching.Compression;

/// <summary>
/// Compression algorithms supported by the unified compression library.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression.
    /// </summary>
    None,

    /// <summary>
    /// GZip compression algorithm.
    /// </summary>
    GZip,

    /// <summary>
    /// Deflate compression algorithm.
    /// </summary>
    Deflate,

    /// <summary>
    /// Brotli compression algorithm (higher compression ratio).
    /// </summary>
    Brotli,

    /// <summary>
    /// LZ4 compression algorithm (faster compression/decompression).
    /// </summary>
    LZ4,

    /// <summary>
    /// Zstandard compression algorithm (balanced speed and ratio).
    /// </summary>
    Zstd
}