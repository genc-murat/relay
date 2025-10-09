namespace Relay.Core.Caching.Compression;

/// <summary>
/// Configuration options for unified compression.
/// </summary>
public sealed class CompressionOptions
{
    /// <summary>
    /// Gets or sets the compression algorithm to use.
    /// </summary>
    public CompressionAlgorithm Algorithm { get; set; } = CompressionAlgorithm.GZip;

    /// <summary>
    /// Gets or sets the compression level (0-9, where 0 is no compression and 9 is maximum).
    /// </summary>
    public int Level { get; set; } = 6;

    /// <summary>
    /// Gets or sets the minimum data size (in bytes) to compress.
    /// Data smaller than this will not be compressed.
    /// </summary>
    public int MinimumSizeBytes { get; set; } = 1024; // 1 KB

    /// <summary>
    /// Gets or sets whether to automatically detect if data is already compressed.
    /// </summary>
    public bool AutoDetectCompressed { get; set; } = true;

    /// <summary>
    /// Gets or sets the expected compression ratio threshold (0.0 to 1.0).
    /// If actual ratio is worse than this, compression will be skipped.
    /// </summary>
    public double ExpectedCompressionRatio { get; set; } = 0.7;
}