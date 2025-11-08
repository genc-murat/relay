using System;
using System.IO;

namespace Relay.Core.Testing;

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
