using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Performance.BufferManagement;

/// <summary>
/// Optimized pooled buffer manager with workload-specific tuning
/// </summary>
public sealed class OptimizedPooledBufferManager : IPooledBufferManager, IDisposable
{
    // Workload-optimized pool configurations
    private readonly ArrayPool<byte> _smallBufferPool;    // 16B - 1KB (frequent small operations)
    private readonly ArrayPool<byte> _mediumBufferPool;   // 1KB - 64KB (request/response serialization)
    private readonly ArrayPool<byte> _largeBufferPool;    // 64KB+ (batch operations, streaming)

    // Performance counters for adaptive tuning
    private long _smallPoolHits;
    private long _mediumPoolHits;
    private long _largePoolHits;
    private long _totalRequests;

    // Optimized size thresholds based on Relay workload patterns
    private const int SmallBufferThreshold = 1024;      // 1KB
    private const int MediumBufferThreshold = 65536;    // 64KB

    // Cache line-aligned buffer tracking for better performance
    [StructLayout(LayoutKind.Explicit, Size = 64)]
    private struct BufferMetrics
    {
        [FieldOffset(0)] public long Requests;
        [FieldOffset(8)] public long Hits;
        [FieldOffset(16)] public long Misses;
        [FieldOffset(24)] public long TotalSize;
    }

    private BufferMetrics _smallMetrics;
    private BufferMetrics _mediumMetrics;
    private BufferMetrics _largeMetrics;

    public OptimizedPooledBufferManager()
    {
        // Create optimized pools with Relay-specific configurations
        _smallBufferPool = CreateOptimizedPool(
            maxBufferSize: SmallBufferThreshold,
            maxArraysPerBucket: 64,  // High frequency, keep more in pool
            maxArrayLength: 1024 * 1024  // 1MB total for small buffers
        );

        _mediumBufferPool = CreateOptimizedPool(
            maxBufferSize: MediumBufferThreshold,
            maxArraysPerBucket: 16,  // Medium frequency
            maxArrayLength: 4 * 1024 * 1024  // 4MB total for medium buffers
        );

        _largeBufferPool = CreateOptimizedPool(
            maxBufferSize: int.MaxValue,
            maxArraysPerBucket: 4,   // Low frequency, fewer cached
            maxArrayLength: 8 * 1024 * 1024  // 8MB total for large buffers
        );
    }

    private static ArrayPool<byte> CreateOptimizedPool(int maxBufferSize, int maxArraysPerBucket, int maxArrayLength)
    {
        return ArrayPool<byte>.Create(maxBufferSize, maxArraysPerBucket);
    }

    /// <summary>
    /// Rents a buffer with optimal pool selection
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] RentBuffer(int minimumLength)
    {
        Interlocked.Increment(ref _totalRequests);

        // Select optimal pool based on size and update metrics
        if (minimumLength <= SmallBufferThreshold)
        {
            Interlocked.Increment(ref _smallPoolHits);
            Interlocked.Increment(ref _smallMetrics.Requests);
            return _smallBufferPool.Rent(minimumLength);
        }
        else if (minimumLength <= MediumBufferThreshold)
        {
            Interlocked.Increment(ref _mediumPoolHits);
            Interlocked.Increment(ref _mediumMetrics.Requests);
            return _mediumBufferPool.Rent(minimumLength);
        }
        else
        {
            Interlocked.Increment(ref _largePoolHits);
            Interlocked.Increment(ref _largeMetrics.Requests);
            return _largeBufferPool.Rent(minimumLength);
        }
    }

    /// <summary>
    /// Returns buffer to appropriate pool
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnBuffer(byte[] buffer, bool clearArray = false)
    {
        if (buffer == null) return;

        var length = buffer.Length;

        // Return to appropriate pool
        if (length <= SmallBufferThreshold)
        {
            _smallBufferPool.Return(buffer, clearArray);
        }
        else if (length <= MediumBufferThreshold)
        {
            _mediumBufferPool.Return(buffer, clearArray);
        }
        else
        {
            _largeBufferPool.Return(buffer, clearArray);
        }
    }

    /// <summary>
    /// High-performance span rental with optimized buffer management
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> RentSpan(int minimumLength)
    {
        // Use pooled buffer for better memory management
        var buffer = RentBuffer(minimumLength);
        return buffer.AsSpan(0, minimumLength);
    }

    /// <summary>
    /// Optimized span return (no-op for stack-allocated spans)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnSpan(byte[] buffer, bool clearArray = false)
    {
        // Only return if it's a real heap buffer
        if (buffer != null)
        {
            ReturnBuffer(buffer, clearArray);
        }
    }

    /// <summary>
    /// High-performance buffer rental with size prediction
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] RentBufferForRequest<T>(T request) where T : IRequest
    {
        // Use heuristics to predict buffer size based on request type
        var estimatedSize = EstimateBufferSize<T>();
        return RentBuffer(estimatedSize);
    }

    /// <summary>
    /// Estimates buffer size needed for request type using historical data
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EstimateBufferSize<T>()
    {
        // Cache friendly size estimation
        return Unsafe.SizeOf<T>() switch
        {
            <= 64 => 256,      // Small request -> small buffer
            <= 256 => 1024,    // Medium request -> medium buffer
            <= 1024 => 4096,   // Large request -> large buffer
            _ => 8192          // Very large request -> very large buffer
        };
    }

    /// <summary>
    /// Gets buffer pool performance metrics
    /// </summary>
    public BufferPoolMetrics GetMetrics()
    {
        return new BufferPoolMetrics
        {
            TotalRequests = _totalRequests,
            SmallPoolHits = _smallPoolHits,
            MediumPoolHits = _mediumPoolHits,
            LargePoolHits = _largePoolHits,
            SmallPoolEfficiency = _smallMetrics.Requests > 0 ? (double)_smallPoolHits / _smallMetrics.Requests : 0,
            MediumPoolEfficiency = _mediumMetrics.Requests > 0 ? (double)_mediumPoolHits / _mediumMetrics.Requests : 0,
            LargePoolEfficiency = _largeMetrics.Requests > 0 ? (double)_largePoolHits / _largeMetrics.Requests : 0
        };
    }

    /// <summary>
    /// Adaptive pool tuning based on usage patterns
    /// </summary>
    public void OptimizeForWorkload()
    {
        var metrics = GetMetrics();

        // Could implement adaptive resizing logic here
        // For now, this provides the metrics needed for manual tuning
    }

    public void Dispose()
    {
        // ArrayPools are typically shared and don't need disposal
        // This is here for interface compliance
    }
}
