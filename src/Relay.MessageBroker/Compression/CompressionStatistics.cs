namespace Relay.MessageBroker.Compression;

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
