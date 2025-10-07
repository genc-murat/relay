using System;
using System.Collections.Generic;
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
    /// Hardware-accelerated memory prefetch
    /// Note: Prefetching is a hint to the CPU and may not always improve performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PrefetchMemory<T>(T obj) where T : class
    {
        // Memory prefetch is primarily beneficial for value types and arrays
        // For reference types, the benefit is minimal as they're already cached
        // This is a no-op placeholder - actual prefetch happens at JIT level
        // In hot paths, the JIT compiler will optimize memory access patterns
        if (obj != null)
        {
            // Touch the object to ensure it's in cache
            _ = obj.GetType();
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

/// <summary>
/// Cache-friendly data structures for better performance
/// </summary>
public sealed class CacheFriendlyDictionary<TKey, TValue> where TKey : notnull
{
    private struct Entry
    {
        public int HashCode;
        public TKey Key;
        public TValue Value;
        public int Next;
    }

    private Entry[] _entries;
    private int[] _buckets;
    private int _count;
    private readonly int _bucketSize;

    public CacheFriendlyDictionary(int capacity = 16)
    {
        _bucketSize = GetPrime(capacity);
        _buckets = new int[_bucketSize];
        _entries = new Entry[_bucketSize];
        
        Array.Fill(_buckets, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        int hashCode = key.GetHashCode();
        int bucket = hashCode % _bucketSize;
        int index = _buckets[bucket];

        while (index >= 0)
        {
            ref var entry = ref _entries[index];
            if (entry.HashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.Key, key))
            {
                value = entry.Value;
                return true;
            }
            index = entry.Next;
        }

        value = default!;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        int hashCode = key.GetHashCode();
        int bucket = hashCode % _bucketSize;

        if (_count >= _entries.Length)
        {
            Resize();
        }

        ref var entry = ref _entries[_count];
        entry.HashCode = hashCode;
        entry.Key = key;
        entry.Value = value;
        entry.Next = _buckets[bucket];
        _buckets[bucket] = _count;
        _count++;
    }

    private void Resize()
    {
        int newSize = _entries.Length * 2;
        Array.Resize(ref _entries, newSize);
    }

    private static int GetPrime(int min)
    {
        int[] primes = { 17, 37, 79, 163, 331, 673, 1361, 2729, 5471, 10949, 21911, 43853, 87719 };
        foreach (int prime in primes)
        {
            if (prime >= min)
                return prime;
        }
        return min;
    }
}
