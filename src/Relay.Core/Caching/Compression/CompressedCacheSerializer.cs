using System;
using Relay.Core.Caching.Compression;

namespace Relay.Core.Caching;

/// <summary>
/// Cache serializer with compression support.
/// </summary>
public class CompressedCacheSerializer : ICacheSerializer
{
    private readonly ICacheSerializer _innerSerializer;
    private readonly ICacheCompressor _compressor;

    public CompressedCacheSerializer(ICacheSerializer innerSerializer, ICacheCompressor compressor)
    {
        _innerSerializer = innerSerializer ?? throw new ArgumentNullException(nameof(innerSerializer));
        _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
    }

    public byte[] Serialize<T>(T obj)
    {
        var data = _innerSerializer.Serialize(obj);
        
        if (_compressor.ShouldCompress(data.Length))
        {
            var compressed = _compressor.Compress(data);
            
            // Add compression header (4 bytes: magic + original length)
            var result = new byte[4 + compressed.Length];
            result[0] = 0x43; // 'C'
            result[1] = 0x5A; // 'Z'
            result[2] = (byte)(data.Length >> 8);
            result[3] = (byte)data.Length;
            
            Buffer.BlockCopy(compressed, 0, result, 4, compressed.Length);
            return result;
        }

        return data;
    }

    public T Deserialize<T>(byte[] data)
    {
        if (data.Length >= 4 && data[0] == 0x43 && data[1] == 0x5A)
        {
            // Compressed data
            var originalLength = (data[2] << 8) | data[3];
            var compressedData = new byte[data.Length - 4];
            Buffer.BlockCopy(data, 4, compressedData, 0, compressedData.Length);
            
            var decompressed = _compressor.Decompress(compressedData);
            return _innerSerializer.Deserialize<T>(decompressed);
        }

        // Uncompressed data
        return _innerSerializer.Deserialize<T>(data);
    }
}