using System;
using System.Runtime.CompilerServices;

namespace Relay.Core.Performance;

/// <summary>
/// Extensions and utilities for memory-efficient data handling with Span&lt;T&gt;
/// </summary>
public static class SpanExtensions
{
    /// <summary>
    /// Efficiently copies data to a span without additional allocations
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="source">Source span</param>
    /// <param name="destination">Destination span</param>
    /// <returns>Number of elements copied</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyToSpan<T>(this ReadOnlySpan<T> source, Span<T> destination)
    {
        var length = Math.Min(source.Length, destination.Length);
        source.Slice(0, length).CopyTo(destination);
        return length;
    }

    /// <summary>
    /// Efficiently slices a span with bounds checking
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="span">The span to slice</param>
    /// <param name="start">Start index</param>
    /// <param name="length">Length of the slice</param>
    /// <returns>A sliced span</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> SafeSlice<T>(this Span<T> span, int start, int length)
    {
        if (start < 0 || start >= span.Length)
            return Span<T>.Empty;
            
        var actualLength = Math.Min(length, span.Length - start);
        return actualLength <= 0 ? Span<T>.Empty : span.Slice(start, actualLength);
    }

    /// <summary>
    /// Efficiently slices a read-only span with bounds checking
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    /// <param name="span">The span to slice</param>
    /// <param name="start">Start index</param>
    /// <param name="length">Length of the slice</param>
    /// <returns>A sliced read-only span</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> SafeSlice<T>(this ReadOnlySpan<T> span, int start, int length)
    {
        if (start < 0 || start >= span.Length)
            return ReadOnlySpan<T>.Empty;
            
        var actualLength = Math.Min(length, span.Length - start);
        return actualLength <= 0 ? ReadOnlySpan<T>.Empty : span.Slice(start, actualLength);
    }
}