using System;
using System.Buffers;

namespace Relay.Core.Performance;

/// <summary>
/// Default implementation of pooled buffer manager using ArrayPool
/// </summary>
public class DefaultPooledBufferManager : IPooledBufferManager
{
    private readonly ArrayPool<byte> _arrayPool;

    /// <summary>
    /// Initializes a new instance of DefaultPooledBufferManager
    /// </summary>
    /// <param name="arrayPool">The array pool to use (optional, defaults to shared pool)</param>
    public DefaultPooledBufferManager(ArrayPool<byte>? arrayPool = null)
    {
        _arrayPool = arrayPool ?? ArrayPool<byte>.Shared;
    }

    /// <summary>
    /// Rents a buffer of at least the specified size
    /// </summary>
    /// <param name="minimumLength">Minimum length of the buffer</param>
    /// <returns>A rented buffer</returns>
    public byte[] RentBuffer(int minimumLength)
    {
        return _arrayPool.Rent(minimumLength);
    }

    /// <summary>
    /// Returns a buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning</param>
    public void ReturnBuffer(byte[] buffer, bool clearArray = false)
    {
        if (buffer != null)
        {
            _arrayPool.Return(buffer, clearArray);
        }
    }

    /// <summary>
    /// Gets a span from a rented buffer
    /// </summary>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>A span backed by a pooled buffer</returns>
    public Span<byte> RentSpan(int minimumLength)
    {
        var buffer = RentBuffer(minimumLength);
        return buffer.AsSpan(0, minimumLength);
    }

    /// <summary>
    /// Returns a span's underlying buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning</param>
    public void ReturnSpan(byte[] buffer, bool clearArray = false)
    {
        ReturnBuffer(buffer, clearArray);
    }
}