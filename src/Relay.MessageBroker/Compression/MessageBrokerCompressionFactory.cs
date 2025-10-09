using Relay.Core.Caching.Compression;

namespace Relay.MessageBroker.Compression;

/// <summary>
/// Factory for creating message compression instances using the unified compression library.
/// </summary>
public static class MessageBrokerCompressionFactory
{


    /// <summary>
    /// Creates a message compressor adapter with custom options.
    /// </summary>
    /// <param name="options">The compression options.</param>
    /// <returns>A message compressor instance.</returns>
    public static IMessageCompressor CreateMessage(CompressionOptions options)
    {
        var coreOptions = options.ToCoreOptions();
        var unified = CompressionFactory.CreateUnified(coreOptions);
        return new MessageCompressorAdapter(unified);
    }

    /// <summary>
    /// Creates a message compressor adapter directly from core options.
    /// </summary>
    /// <param name="options">The core compression options.</param>
    /// <returns>A message compressor instance.</returns>
    public static IMessageCompressor CreateFromCore(Relay.Core.Caching.Compression.CompressionOptions options)
    {
        var unified = CompressionFactory.CreateUnified(options);
        return new MessageCompressorAdapter(unified);
    }


}