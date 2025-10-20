using System.Runtime.CompilerServices;

namespace Relay.Core.Performance.Optimization;

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
