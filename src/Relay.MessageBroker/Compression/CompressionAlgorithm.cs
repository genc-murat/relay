// This file is deprecated - use Relay.Core.Caching.Compression.CompressionAlgorithm instead
// This type alias is provided for backward compatibility
using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Compression algorithms supported by the message broker.
/// This is now an alias for Relay.Core.Caching.Compression.CompressionAlgorithm.
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
