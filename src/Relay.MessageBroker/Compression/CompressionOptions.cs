using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Configuration options for message compression.
/// This extends the core compression options with message-specific settings.
/// </summary>
public sealed class CompressionOptions : Relay.Core.Caching.Compression.CompressionOptions
{
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
    /// Gets or sets whether compression is enabled for messages.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Converts to core compression options for base class functionality.
    /// </summary>
    /// <returns>Core compression options.</returns>
    public Relay.Core.Caching.Compression.CompressionOptions ToCoreOptions()
    {
        return new Relay.Core.Caching.Compression.CompressionOptions
        {
            Algorithm = Algorithm,
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
