using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Relay.Core.Performance.Optimization;

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
        int bucket = Math.Abs(hashCode) % _bucketSize;
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
        int bucket = Math.Abs(hashCode) % _bucketSize;

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
