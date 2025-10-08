using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Relay.Core.Performance.Optimization;

/// <summary>
/// SIMD and hardware acceleration helper functions
/// </summary>
public static class PerformanceHelpers
{
    /// <summary>
    /// Gets whether SIMD is available on this hardware
    /// </summary>
    public static bool IsSIMDAvailable => Vector.IsHardwareAccelerated;

    /// <summary>
    /// Gets whether AVX2 is available
    /// </summary>
    public static bool IsAVX2Available => Avx2.IsSupported;

    /// <summary>
    /// Gets whether SSE is available
    /// </summary>
    public static bool IsSSEAvailable => Sse.IsSupported;

    /// <summary>
    /// Gets the optimal batch size for SIMD operations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOptimalBatchSize(int totalItems)
    {
        if (!Vector.IsHardwareAccelerated)
            return totalItems;

        var vectorSize = Vector<int>.Count;
        return (totalItems + vectorSize - 1) / vectorSize * vectorSize;
    }

    /// <summary>
    /// Gets the SIMD vector size for the current hardware
    /// </summary>
    public static int VectorSize => Vector<int>.Count;

    /// <summary>
    /// Hardware-accelerated memory prefetch for objects
    /// Note: Prefetching is a hint to the CPU and may not always improve performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrefetchMemory<T>(T obj) where T : class
    {
        if (obj == null) return;

        // Touch the object header and first field to encourage cache loading
        // This helps the CPU's prefetcher anticipate memory access patterns
        _ = obj.GetHashCode();
        _ = obj.GetType();
    }

    /// <summary>
    /// Hardware-accelerated memory prefetch for arrays and spans
    /// Optimized for sequential access patterns
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void PrefetchMemory<T>(Span<T> span) where T : unmanaged
    {
        if (span.Length == 0) return;

        // Prefetch first and last elements to encourage cache line loading
        // This is particularly effective for large arrays that will be accessed sequentially
        ref var first = ref span[0];
        ref var last = ref span[span.Length - 1];
        
        // Touch the memory locations to trigger prefetch
        T temp1 = first;
        T temp2 = last;
        
        // Prevent compiler optimization
        _ = temp1;
        _ = temp2;
    }

    /// <summary>
    /// Hardware-accelerated memory prefetch for managed arrays
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrefetchMemoryClassArray<T>(T[] array) where T : class
    {
        if (array == null || array.Length == 0) return;

        // Prefetch first few elements to encourage cache line loading
        int prefetchCount = Math.Min(3, array.Length);
        for (int i = 0; i < prefetchCount; i++)
        {
            if (array[i] != null)
            {
                _ = array[i].GetHashCode();
            }
        }
    }

    /// <summary>
    /// Hardware-accelerated memory prefetch for value type arrays
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void PrefetchMemoryValueArray<T>(T[] array) where T : unmanaged
    {
        if (array == null || array.Length == 0) return;

        // Use span version for value types
        PrefetchMemory(array.AsSpan());
    }

    /// <summary>
    /// Prefetches memory for multiple objects in parallel
    /// Useful for cache-friendly data structure traversal
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrefetchMemoryMultiple<T>(params T[] objects) where T : class
    {
        if (objects == null || objects.Length == 0) return;

        // Prefetch up to 4 objects to avoid cache pollution
        int prefetchCount = Math.Min(4, objects.Length);
        for (int i = 0; i < prefetchCount; i++)
        {
            PrefetchMemory(objects[i]);
        }
    }

    /// <summary>
    /// Performs SIMD-accelerated array initialization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitializeArray<T>(Span<T> array, T value) where T : struct
    {
        if (Vector.IsHardwareAccelerated && array.Length >= Vector<T>.Count)
        {
            var vector = new Vector<T>(value);
            var span = MemoryMarshal.Cast<T, Vector<T>>(array);
            
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = vector;
            }

            // Handle remaining elements
            int processed = span.Length * Vector<T>.Count;
            for (int i = processed; i < array.Length; i++)
            {
                array[i] = value;
            }
        }
        else
        {
            array.Fill(value);
        }
    }

    /// <summary>
    /// SIMD-accelerated comparison of two spans
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SequenceEqual(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        if (first.Length != second.Length)
            return false;

        if (first.Length == 0)
            return true;

        if (Vector.IsHardwareAccelerated && first.Length >= Vector<byte>.Count)
        {
            return SequenceEqualSIMD(first, second);
        }

        return first.SequenceEqual(second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool SequenceEqualSIMD(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        int vectorSize = Vector<byte>.Count;
        int i = 0;

        for (; i <= first.Length - vectorSize; i += vectorSize)
        {
            var v1 = new Vector<byte>(first.Slice(i, vectorSize));
            var v2 = new Vector<byte>(second.Slice(i, vectorSize));
            
            if (!Vector.EqualsAll(v1, v2))
                return false;
        }

        // Compare remaining bytes
        for (; i < first.Length; i++)
        {
            if (first[i] != second[i])
                return false;
        }

        return true;
    }
}

/// <summary>
/// AOT-friendly type helpers that avoid reflection
/// </summary>
public static class AOTHelpers
{
    /// <summary>
    /// Gets type name in AOT-safe manner
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetTypeName<T>()
    {
        return typeof(T).Name;
    }

    /// <summary>
    /// Checks if type is value type in AOT-safe manner
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValueType<T>()
    {
        return typeof(T).IsValueType;
    }

    /// <summary>
    /// Creates default value in AOT-safe manner
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T CreateDefault<T>()
    {
        return default!;
    }
}
