using System;

namespace Relay.Core.Performance.BufferManagement;

/// <summary>
/// Interface for managing pooled buffers to reduce allocations
/// </summary>
public interface IPooledBufferManager
{
    /// <summary>
    /// Rents a buffer of at least the specified size
    /// </summary>
    /// <param name="minimumLength">Minimum length of the buffer</param>
    /// <returns>A rented buffer</returns>
    byte[] RentBuffer(int minimumLength);

    /// <summary>
    /// Returns a buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning</param>
    void ReturnBuffer(byte[] buffer, bool clearArray = false);

    /// <summary>
    /// Gets a span from a rented buffer
    /// </summary>
    /// <param name="minimumLength">Minimum length needed</param>
    /// <returns>A span backed by a pooled buffer</returns>
    Span<byte> RentSpan(int minimumLength);

    /// <summary>
    /// Returns a span's underlying buffer to the pool
    /// </summary>
    /// <param name="buffer">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning</param>
    void ReturnSpan(byte[] buffer, bool clearArray = false);
}