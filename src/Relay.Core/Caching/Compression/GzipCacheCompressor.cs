using System;
using System.IO;
using System.IO.Compression;

namespace Relay.Core.Caching.Compression;

/// <summary>
/// GZIP-based cache compressor.
/// </summary>
public class GzipCacheCompressor : ICacheCompressor
{
    private readonly int _compressionThreshold;

    /// <summary>
    /// Initializes a new instance of the <see cref="GzipCacheCompressor"/> class.
    /// </summary>
    /// <param name="compressionThreshold">Minimum data size in bytes to compress (default: 1024).</param>
    public GzipCacheCompressor(int compressionThreshold = 1024)
    {
        _compressionThreshold = compressionThreshold;
    }

    public byte[] Compress(byte[] data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
        {
            gzip.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressedData)
    {
        if (compressedData == null) throw new ArgumentNullException(nameof(compressedData));

        using var input = new MemoryStream(compressedData);
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(input, CompressionMode.Decompress, true))
        {
            gzip.CopyTo(output);
        }
        return output.ToArray();
    }

    public bool ShouldCompress(int dataSize)
    {
        return dataSize >= _compressionThreshold;
    }
}