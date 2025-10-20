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
