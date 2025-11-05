using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// Base class for xUnit collection fixtures that provide shared TestRelay instances across multiple test classes.
/// </summary>
public abstract class RelayCollectionFixture : IAsyncLifetime
{
    private TestRelay? _sharedTestRelay;
    private IServiceProvider? _sharedServices;

    /// <summary>
    /// Gets the shared TestRelay instance.
    /// </summary>
    public TestRelay TestRelay => _sharedTestRelay ?? throw new InvalidOperationException("Shared TestRelay not initialized.");

    /// <summary>
    /// Gets the shared service provider.
    /// </summary>
    public IServiceProvider Services => _sharedServices ?? throw new InvalidOperationException("Shared services not initialized.");

    /// <summary>
    /// Initializes the shared test environment asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task InitializeAsync()
    {
        _sharedTestRelay = new TestRelay();
        ConfigureSharedTestRelay(_sharedTestRelay);
        _sharedServices = new TestServiceProvider();

        await OnSharedTestInitializedAsync();
    }

    /// <summary>
    /// Cleans up the shared test environment asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task DisposeAsync()
    {
        await OnSharedTestCleanupAsync();

        if (_sharedTestRelay != null)
        {
            _sharedTestRelay.Clear();
            _sharedTestRelay = null;
        }

        _sharedServices = null;
    }

    /// <summary>
    /// Configures the shared TestRelay instance.
    /// Override this method to customize the shared setup.
    /// </summary>
    /// <param name="testRelay">The TestRelay instance to configure.</param>
    protected virtual void ConfigureSharedTestRelay(TestRelay testRelay)
    {
        // Default configuration - override in derived classes
    }

    /// <summary>
    /// Called after the shared test environment has been initialized.
    /// Override this method to perform additional shared setup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnSharedTestInitializedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called before the shared test environment is cleaned up.
    /// Override this method to perform additional shared cleanup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnSharedTestCleanupAsync()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// xUnit collection definition for tests that share a TestRelay instance.
/// </summary>
[CollectionDefinition("RelayTestCollection")]
public class RelayTestCollection : ICollectionFixture<RelayCollectionFixture>
{
}