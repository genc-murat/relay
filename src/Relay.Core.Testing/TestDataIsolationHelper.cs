using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Helper class for managing test data isolation across different storage mechanisms.
/// Provides utilities for creating isolated test data contexts, managing transactions,
/// and ensuring cleanup of test data.
/// </summary>
public class TestDataIsolationHelper : IDisposable, IAsyncDisposable
{
    private readonly List<Func<Task>> _cleanupActions = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly List<IAsyncDisposable> _asyncDisposables = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isDisposed;

    /// <summary>
    /// Gets a unique test identifier for this isolation context.
    /// </summary>
    public string TestId { get; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets the isolation level for this test context.
    /// </summary>
    public IsolationLevel Level { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataIsolationHelper"/> class.
    /// </summary>
    /// <param name="level">The isolation level to use.</param>
    public TestDataIsolationHelper(IsolationLevel level = IsolationLevel.DatabaseTransaction)
    {
        Level = level;
    }

    /// <summary>
    /// Registers a cleanup action to be executed when the test completes.
    /// </summary>
    /// <param name="cleanupAction">The cleanup action.</param>
    public void RegisterCleanupAction(Func<Task> cleanupAction)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        _cleanupActions.Add(cleanupAction);
    }

    /// <summary>
    /// Registers a disposable resource for automatic cleanup.
    /// </summary>
    /// <param name="disposable">The disposable resource.</param>
    public void RegisterDisposable(IDisposable disposable)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        _disposables.Add(disposable);
    }

    /// <summary>
    /// Registers an async disposable resource for automatic cleanup.
    /// </summary>
    /// <param name="asyncDisposable">The async disposable resource.</param>
    public void RegisterAsyncDisposable(IAsyncDisposable asyncDisposable)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        _asyncDisposables.Add(asyncDisposable);
    }

    /// <summary>
    /// Creates an isolated database transaction context.
    /// </summary>
    /// <typeparam name="TDbContext">The database context type.</typeparam>
    /// <param name="createContext">Function to create the database context.</param>
    /// <returns>An isolated database context wrapper.</returns>
    public IsolatedDatabaseContext<TDbContext> CreateIsolatedDatabaseContext<TDbContext>(
        Func<TDbContext> createContext)
        where TDbContext : IDisposable
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        var context = createContext();
        RegisterDisposable(context);

        return new IsolatedDatabaseContext<TDbContext>(context, TestId, Level);
    }

    /// <summary>
    /// Creates an isolated in-memory data store.
    /// </summary>
    /// <typeparam name="TData">The data type to store.</typeparam>
    /// <returns>An isolated in-memory store.</returns>
    public IsolatedMemoryStore<TData> CreateIsolatedMemoryStore<TData>()
        where TData : class
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        var store = new IsolatedMemoryStore<TData>(TestId);
        RegisterDisposable(store);

        return store;
    }

    /// <summary>
    /// Creates a temporary file with isolated naming.
    /// </summary>
    /// <param name="extension">The file extension (without dot).</param>
    /// <returns>The path to the temporary file.</returns>
    public string CreateIsolatedTempFile(string extension = "tmp")
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        var fileName = $"{TestId}.{extension}";
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);

        // Register cleanup action to delete the file
        RegisterCleanupAction(() => Task.Run(() =>
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }));

        return tempPath;
    }

    /// <summary>
    /// Executes an action within an isolated context.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteIsolatedAsync(Func<Task> action)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        await _semaphore.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Executes a function within an isolated context and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <returns>The result of the function.</returns>
    public async Task<TResult> ExecuteIsolatedAsync<TResult>(Func<Task<TResult>> function)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(TestDataIsolationHelper));

        await _semaphore.WaitAsync();
        try
        {
            return await function();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Disposes of all resources and executes cleanup actions.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously disposes of all resources and executes cleanup actions.
    /// </summary>
    /// <returns>A task representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        // Execute cleanup actions in reverse order
        for (var i = _cleanupActions.Count - 1; i >= 0; i--)
        {
            try
            {
                await _cleanupActions[i]();
            }
            catch
            {
                // Continue with other cleanup actions even if one fails
            }
        }

        // Dispose async disposables in reverse order
        for (var i = _asyncDisposables.Count - 1; i >= 0; i--)
        {
            try
            {
                await _asyncDisposables[i].DisposeAsync();
            }
            catch
            {
                // Continue with other disposables even if one fails
            }
        }

        // Dispose regular disposables in reverse order
        for (var i = _disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                _disposables[i].Dispose();
            }
            catch
            {
                // Continue with other disposables even if one fails
            }
        }

        _semaphore.Dispose();
    }
}

/// <summary>
/// Defines isolation levels for test data.
/// </summary>
public enum IsolationLevel
{
    /// <summary>
    /// No isolation - data may be shared across tests.
    /// </summary>
    None,

    /// <summary>
    /// Memory isolation - data is isolated in memory only.
    /// </summary>
    Memory,

    /// <summary>
    /// Database transaction isolation - uses database transactions.
    /// </summary>
    DatabaseTransaction,

    /// <summary>
    /// Full isolation - complete separation including file system and external resources.
    /// </summary>
    Full
}

/// <summary>
/// Wrapper for isolated database contexts.
/// </summary>
/// <typeparam name="TDbContext">The database context type.</typeparam>
public class IsolatedDatabaseContext<TDbContext> : IDisposable
    where TDbContext : IDisposable
{
    private readonly TDbContext _context;
    private readonly string _testId;
    private readonly IsolationLevel _level;

    /// <summary>
    /// Gets the underlying database context.
    /// </summary>
    public TDbContext Context => _context;

    /// <summary>
    /// Gets the test identifier.
    /// </summary>
    public string TestId => _testId;

    /// <summary>
    /// Gets the isolation level.
    /// </summary>
    public IsolationLevel Level => _level;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedDatabaseContext{TDbContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="testId">The test identifier.</param>
    /// <param name="level">The isolation level.</param>
    public IsolatedDatabaseContext(TDbContext context, string testId, IsolationLevel level)
    {
        _context = context;
        _testId = testId;
        _level = level;
    }

    /// <summary>
    /// Disposes the database context.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
    }
}

/// <summary>
/// Isolated in-memory data store for testing.
/// </summary>
/// <typeparam name="TData">The data type to store.</typeparam>
public class IsolatedMemoryStore<TData> : IDisposable
    where TData : class
{
    private readonly Dictionary<string, TData> _store = new();
    private readonly string _testId;

    /// <summary>
    /// Gets the test identifier.
    /// </summary>
    public string TestId => _testId;

    /// <summary>
    /// Gets all stored items.
    /// </summary>
    public IEnumerable<TData> Items => _store.Values;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsolatedMemoryStore{TData}"/> class.
    /// </summary>
    /// <param name="testId">The test identifier.</param>
    public IsolatedMemoryStore(string testId)
    {
        _testId = testId;
    }

    /// <summary>
    /// Stores an item with the specified key.
    /// </summary>
    /// <param name="key">The key to store the item under.</param>
    /// <param name="item">The item to store.</param>
    public void Store(string key, TData item)
    {
        _store[key] = item;
    }

    /// <summary>
    /// Retrieves an item by key.
    /// </summary>
    /// <param name="key">The key of the item to retrieve.</param>
    /// <returns>The stored item, or null if not found.</returns>
    public TData? Retrieve(string key)
    {
        _store.TryGetValue(key, out var item);
        return item;
    }

    /// <summary>
    /// Removes an item by key.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>True if the item was removed, false otherwise.</returns>
    public bool Remove(string key)
    {
        return _store.Remove(key);
    }

    /// <summary>
    /// Clears all stored items.
    /// </summary>
    public void Clear()
    {
        _store.Clear();
    }

    /// <summary>
    /// Disposes the memory store.
    /// </summary>
    public void Dispose()
    {
        _store.Clear();
    }
}