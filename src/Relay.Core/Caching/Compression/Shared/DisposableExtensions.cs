using System;

namespace Relay.Core.Caching.Compression;

/// <summary>
/// Extension methods for disposable pattern.
/// </summary>
internal static class DisposableExtensions
{
    public static void DisposeAfter<T>(this T disposable, Action<T> action) where T : IDisposable
    {
        using (disposable)
        {
            action(disposable);
        }
    }
}