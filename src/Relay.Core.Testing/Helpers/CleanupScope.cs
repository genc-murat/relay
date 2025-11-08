using System;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a cleanup scope that executes cleanup when disposed.
/// </summary>
internal class CleanupScope : IDisposable
{
    private readonly TestCleanupHelper _helper;
    private bool _disposed;

    public CleanupScope(TestCleanupHelper helper)
    {
        _helper = helper;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _helper.Cleanup();
    }
}