using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Factory for creating message compression instances using the unified compression library.
/// </summary>
public static class MessageBrokerCompressionFactory
{
    /// <summary>
    /// Creates a message compressor adapter.
    /// </summary>
    /// <param name="algorithm">The compression algorithm.</param>
    /// <param name="level">The compression level (0-9).</param>
    /// <param name="minimumSizeBytes">Minimum data size to compress.</param>
    /// <returns>A message compressor instance.</returns>
    public static IMessageCompressor CreateMessage(
        CompressionAlgorithm algorithm = CompressionAlgorithm.GZip, 
        int level = 6, 
        int minimumSizeBytes = 1024)
    {
        var coreAlgorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)Enum.Parse(
            typeof(Relay.Core.Caching.Compression.CompressionAlgorithm), 
            algorithm.ToString());
        var unified = CompressionFactory.CreateUnified(coreAlgorithm, level, minimumSizeBytes);
        return new MessageCompressorAdapter(unified);
    }

    /// <summary>
    /// Creates a message compressor adapter with custom options.
    /// </summary>
    /// <param name="options">The compression options.</param>
    /// <returns>A message compressor instance.</returns>
    public static IMessageCompressor CreateMessage(CompressionOptions options)
    {
        var coreOptions = new Relay.Core.Caching.Compression.CompressionOptions
        {
            Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)Enum.Parse(
                typeof(Relay.Core.Caching.Compression.CompressionAlgorithm), 
                options.Algorithm.ToString()),
            Level = options.Level,
            MinimumSizeBytes = options.MinimumSizeBytes,
            AutoDetectCompressed = options.AutoDetectCompressed,
            ExpectedCompressionRatio = options.ExpectedCompressionRatio
        };
        var unified = CompressionFactory.CreateUnified(coreOptions);
        return new MessageCompressorAdapter(unified);
    }

    /// <summary>
    /// Creates a GZip message compressor.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    /// <returns>A GZip message compressor.</returns>
    public static IMessageCompressor CreateGZip(int level = 6)
    {
        return CreateMessage(CompressionAlgorithm.GZip, level, 1024);
    }

    /// <summary>
    /// Creates a Deflate message compressor.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    /// <returns>A Deflate message compressor.</returns>
    public static IMessageCompressor CreateDeflate(int level = 6)
    {
        return CreateMessage(CompressionAlgorithm.Deflate, level, 1024);
    }

    /// <summary>
    /// Creates a Brotli message compressor.
    /// </summary>
    /// <param name="level">The compression level (0-9).</param>
    /// <returns>A Brotli message compressor.</returns>
    public static IMessageCompressor CreateBrotli(int level = 6)
    {
        return CreateMessage(CompressionAlgorithm.Brotli, level, 1024);
    }
}