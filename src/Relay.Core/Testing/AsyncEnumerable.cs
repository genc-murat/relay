using System.Collections.Generic;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Empty async enumerable for testing.
/// </summary>
internal static class AsyncEnumerable
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}