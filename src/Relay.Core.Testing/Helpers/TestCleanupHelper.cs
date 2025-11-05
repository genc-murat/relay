using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Helper class for managing test cleanup operations.
/// Provides utilities for cleaning up resources, files, and resetting test state.
/// </summary>
public class TestCleanupHelper : IDisposable, IAsyncDisposable
{
    private readonly List<Func<Task>> _cleanupActions = new();
    private readonly List<Action> _syncCleanupActions = new();
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirectories = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// Registers an asynchronous cleanup action.
    /// </summary>
    /// <param name="cleanupAction">The cleanup action to execute.</param>
    public void RegisterCleanupAction(Func<Task> cleanupAction)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        _cleanupActions.Add(cleanupAction);
    }

    /// <summary>
    /// Registers a synchronous cleanup action.
    /// </summary>
    /// <param name="cleanupAction">The cleanup action to execute.</param>
    public void RegisterCleanupAction(Action cleanupAction)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        _syncCleanupActions.Add(cleanupAction);
    }

    /// <summary>
    /// Registers a temporary file for automatic cleanup.
    /// </summary>
    /// <param name="filePath">The path to the temporary file.</param>
    public void RegisterTempFile(string filePath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        _tempFiles.Add(filePath);
    }

    /// <summary>
    /// Registers a temporary directory for automatic cleanup.
    /// </summary>
    /// <param name="directoryPath">The path to the temporary directory.</param>
    public void RegisterTempDirectory(string directoryPath)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        _tempDirectories.Add(directoryPath);
    }

    /// <summary>
    /// Creates a temporary file and registers it for cleanup.
    /// </summary>
    /// <param name="extension">The file extension (without dot).</param>
    /// <returns>The path to the created temporary file.</returns>
    public string CreateTempFile(string extension = "tmp")
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{extension}");
        RegisterTempFile(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Creates a temporary directory and registers it for cleanup.
    /// </summary>
    /// <returns>The path to the created temporary directory.</returns>
    public string CreateTempDirectory()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestCleanupHelper));

        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPath);
        RegisterTempDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Registers an IDisposable resource for cleanup.
    /// </summary>
    /// <param name="disposable">The resource to dispose.</param>
    public void RegisterDisposable(IDisposable disposable)
    {
        RegisterCleanupAction(() => { disposable.Dispose(); return Task.CompletedTask; });
    }

    /// <summary>
    /// Registers an IAsyncDisposable resource for cleanup.
    /// </summary>
    /// <param name="asyncDisposable">The resource to dispose.</param>
    public void RegisterAsyncDisposable(IAsyncDisposable asyncDisposable)
    {
        RegisterCleanupAction(async () => await asyncDisposable.DisposeAsync());
    }

    /// <summary>
    /// Adds a cleanup action to reset static state.
    /// </summary>
    /// <typeparam name="T">The type containing the static state.</typeparam>
    /// <param name="resetAction">The action to reset the static state.</param>
    public void RegisterStaticReset<T>(Action resetAction)
    {
        RegisterCleanupAction(resetAction);
    }

    /// <summary>
    /// Adds a cleanup action to clear a collection.
    /// </summary>
    /// <typeparam name="T">The collection item type.</typeparam>
    /// <param name="collection">The collection to clear.</param>
    public void RegisterCollectionClear<T>(ICollection<T> collection)
    {
        RegisterCleanupAction(() => collection.Clear());
    }

    /// <summary>
    /// Executes all cleanup actions synchronously.
    /// </summary>
    public void Cleanup()
    {
        CleanupAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Executes all cleanup actions asynchronously.
    /// </summary>
    /// <returns>A task representing the cleanup operation.</returns>
    public async Task CleanupAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Execute synchronous cleanup actions
            foreach (var action in _syncCleanupActions)
            {
                try
                {
                    action();
                }
                catch
                {
                    // Continue with other cleanup actions even if one fails
                }
            }

            // Execute asynchronous cleanup actions
            foreach (var action in _cleanupActions)
            {
                try
                {
                    await action();
                }
                catch
                {
                    // Continue with other cleanup actions even if one fails
                }
            }

            // Clean up temporary files
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Continue with other cleanup even if file deletion fails
                }
            }

            // Clean up temporary directories
            foreach (var directory in _tempDirectories)
            {
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        if (Directory.Exists(directory))
                        {
                            Directory.Delete(directory, true);
                            break; // Success, exit retry loop
                        }
                    }
                    catch
                    {
                        if (i == 2) // Last attempt
                        {
                            // Continue with other cleanup even if directory deletion fails
                        }
                        else
                        {
                            Thread.Sleep(10); // Wait a bit before retry
                        }
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Disposes the cleanup helper and executes all cleanup actions.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously disposes the cleanup helper and executes all cleanup actions.
    /// </summary>
    /// <returns>A task representing the dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        await CleanupAsync();
        _semaphore.Dispose();
    }
}

/// <summary>
/// Extension methods for test cleanup operations.
/// </summary>
public static class TestCleanupExtensions
{
    /// <summary>
    /// Creates a cleanup scope that automatically cleans up when disposed.
    /// </summary>
    /// <param name="helper">The cleanup helper.</param>
    /// <returns>A disposable cleanup scope.</returns>
    public static IDisposable CreateScope(this TestCleanupHelper helper)
    {
        return new CleanupScope(helper);
    }

    /// <summary>
    /// Registers cleanup for a database context that implements IDisposable.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type.</typeparam>
    /// <param name="helper">The cleanup helper.</param>
    /// <param name="context">The database context to clean up.</param>
    public static void RegisterDatabaseContext<TDbContext>(this TestCleanupHelper helper, TDbContext context)
        where TDbContext : IDisposable
    {
        helper.RegisterDisposable(context);
    }

    /// <summary>
    /// Registers cleanup for an HTTP client.
    /// </summary>
    /// <param name="helper">The cleanup helper.</param>
    /// <param name="client">The HTTP client to clean up.</param>
    public static void RegisterHttpClient(this TestCleanupHelper helper, System.Net.Http.HttpClient client)
    {
        helper.RegisterDisposable(client);
    }

    /// <summary>
    /// Registers cleanup for a stream.
    /// </summary>
    /// <param name="helper">The cleanup helper.</param>
    /// <param name="stream">The stream to clean up.</param>
    public static void RegisterStream(this TestCleanupHelper helper, Stream stream)
    {
        helper.RegisterDisposable(stream);
    }
}

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