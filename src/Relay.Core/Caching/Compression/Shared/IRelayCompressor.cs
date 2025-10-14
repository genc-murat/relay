using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching.Compression;

/// <summary>
/// Unified interface for compression services supporting both sync and async operations.
/// </summary>
public interface IRelayCompressor
{
    /// <summary>
    /// Gets the compression algorithm.
    /// </summary>
    CompressionAlgorithm Algorithm { get; }

    /// <summary>
    /// Compresses the input data synchronously.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <returns>The compressed data.</returns>
    byte[] Compress(byte[] data);

    /// <summary>
    /// Compresses the input data asynchronously.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The compressed data.</returns>
    ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decompresses the input data synchronously.
    /// </summary>
    /// <param name="data">The data to decompress.</param>
    /// <returns>The decompressed data.</returns>
    byte[] Decompress(byte[] data);

    /// <summary>
    /// Decompresses the input data asynchronously.
    /// </summary>
    /// <param name="data">The data to decompress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decompressed data.</returns>
    ValueTask<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the data appears to be already compressed.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if data appears compressed, false otherwise.</returns>
    bool IsCompressed(byte[] data);

    /// <summary>
    /// Gets whether compression should be applied for the given data size.
    /// </summary>
    /// <param name="dataSize">The size of the data in bytes.</param>
    /// <returns>True if compression should be applied, false otherwise.</returns>
    bool ShouldCompress(int dataSize);
}