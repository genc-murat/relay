namespace Relay.MessageBroker.Compression;

/// <summary>
/// Interface for message compression services.
/// </summary>
public interface IMessageCompressor
{
    /// <summary>
    /// Compresses the input data.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compressed data.</returns>
    ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses the input data.
    /// </summary>
    /// <param name="data">The data to decompress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the compression algorithm.
    /// </summary>
    CompressionAlgorithm Algorithm { get; }

    /// <summary>
    /// Checks if the data appears to be already compressed.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data appears compressed, false otherwise.</returns>
    bool IsCompressed(byte[] data);
}
