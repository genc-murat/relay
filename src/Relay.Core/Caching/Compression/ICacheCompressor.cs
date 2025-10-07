namespace Relay.Core.Caching.Compression;

/// <summary>
/// Interface for cache compression.
/// </summary>
public interface ICacheCompressor
{
    /// <summary>
    /// Compresses the given data.
    /// </summary>
    byte[] Compress(byte[] data);

    /// <summary>
    /// Decompresses the given data.
    /// </summary>
    byte[] Decompress(byte[] compressedData);

    /// <summary>
    /// Gets whether compression should be applied for the given data size.
    /// </summary>
    bool ShouldCompress(int dataSize);
}