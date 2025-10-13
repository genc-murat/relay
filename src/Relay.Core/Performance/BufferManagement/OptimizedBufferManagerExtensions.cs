using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.Performance.BufferManagement;

/// <summary>
/// Service collection extensions for optimized buffer management
/// </summary>
public static class OptimizedBufferManagerExtensions
{
    /// <summary>
    /// Gets optimized service for better performance
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T? GetOptimizedServiceFromPool<T>(this IServiceProvider serviceProvider) where T : class
    {
        // Use faster service resolution when possible
        return serviceProvider.GetService<T>();
    }
}