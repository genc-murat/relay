using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Configuration options for message compression.
/// This wraps the core compression options with message-specific settings.
/// </summary>
public sealed class CompressionOptions
{
    /// <summary>
    /// Gets or sets whether compression is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the compression algorithm to use.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.GZip;

    /// <summary>
    /// Gets or sets the compression level (0-9, where 0 is no compression and 9 is maximum).
    /// </summary>
    public int Level { get; set; } = 6;

    /// <summary>
    /// Gets or sets the minimum message size (in bytes) to compress.
    /// Messages smaller than this will not be compressed.
    /// </summary>
    public int MinimumSizeBytes { get; set; } = 1024; // 1 KB

    /// <summary>
    /// Gets or sets whether to automatically detect if message is already compressed.
    /// </summary>
    public bool AutoDetectCompressed { get; set; } = true;

    /// <summary>
    /// Gets or sets the expected compression ratio threshold (0.0 to 1.0).
    /// If actual ratio is worse than this, compression will be skipped.
    /// </summary>
    public double ExpectedCompressionRatio { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the content types that should be compressed (e.g., "application/json", "text/plain").
    /// Empty list means compress all types.
    /// </summary>
    public List<string> CompressibleContentTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the content types that should NOT be compressed (e.g., "image/jpeg", "video/mp4").
    /// </summary>
    public List<string> NonCompressibleContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
        "video/mp4",
        "video/mpeg",
        "video/webm",
        "audio/mp3",
        "audio/mpeg",
        "application/zip",
        "application/gzip",
        "application/x-7z-compressed",
        "application/x-rar-compressed"
    };

    /// <summary>
    /// Gets or sets whether to add compression metadata to message headers.
    /// </summary>
    public bool AddMetadataHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track compression statistics.
    /// </summary>
    public bool TrackStatistics { get; set; } = true;

    /// <summary>
    /// Converts to core compression options.
    /// </summary>
    /// <returns>Core compression options.</returns>
    internal Relay.Core.Caching.Compression.CompressionOptions ToCoreOptions()
    {
        return new Relay.Core.Caching.Compression.CompressionOptions
        {
            Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)Enum.Parse(
                typeof(Relay.Core.Caching.Compression.CompressionAlgorithm), 
                Algorithm.ToString()),
            Level = Level,
            MinimumSizeBytes = MinimumSizeBytes,
            AutoDetectCompressed = AutoDetectCompressed,
            ExpectedCompressionRatio = ExpectedCompressionRatio
        };
    }
}

/// <summary>
/// Compression statistics for monitoring.
/// </summary>
public sealed class CompressionStatistics
{
    /// <summary>
    /// Gets or sets the total number of messages processed.
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of compressed messages.
    /// </summary>
    public long CompressedMessages { get; set; }

    /// <summary>
    /// Gets or sets the number of skipped messages (too small or already compressed).
    /// </summary>
    public long SkippedMessages { get; set; }

    /// <summary>
    /// Gets or sets the total original size in bytes.
    /// </summary>
    public long TotalOriginalBytes { get; set; }

    /// <summary>
    /// Gets or sets the total compressed size in bytes.
    /// </summary>
    public long TotalCompressedBytes { get; set; }

    /// <summary>
    /// Gets or sets the total time spent on compression.
    /// </summary>
    public TimeSpan TotalCompressionTime { get; set; }

    /// <summary>
    /// Gets or sets the total time spent on decompression.
    /// </summary>
    public TimeSpan TotalDecompressionTime { get; set; }

    /// <summary>
    /// Gets the average compression ratio.
    /// </summary>
    public double AverageCompressionRatio => TotalOriginalBytes > 0 
        ? (double)TotalCompressedBytes / TotalOriginalBytes 
        : 0;

    /// <summary>
    /// Gets the total bytes saved.
    /// </summary>
    public long TotalBytesSaved => TotalOriginalBytes - TotalCompressedBytes;

    /// <summary>
    /// Gets the percentage of messages compressed.
    /// </summary>
    public double CompressionRate => TotalMessages > 0 
        ? (double)CompressedMessages / TotalMessages 
        : 0;

    /// <summary>
    /// Gets the average compression time per message.
    /// </summary>
    public TimeSpan AverageCompressionTime => CompressedMessages > 0 
        ? TimeSpan.FromTicks(TotalCompressionTime.Ticks / CompressedMessages)
        : TimeSpan.Zero;

    /// <summary>
    /// Gets the average decompression time per message.
    /// </summary>
    public TimeSpan AverageDecompressionTime => CompressedMessages > 0 
        ? TimeSpan.FromTicks(TotalDecompressionTime.Ticks / CompressedMessages)
        : TimeSpan.Zero;
}
