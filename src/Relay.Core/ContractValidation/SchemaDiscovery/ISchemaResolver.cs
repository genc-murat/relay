using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Interface for resolving schemas for request/response types.
/// </summary>
public interface ISchemaResolver
{
    /// <summary>
    /// Resolves a schema for the specified type.
    /// </summary>
    /// <param name="type">The type to resolve a schema for.</param>
    /// <param name="context">The schema resolution context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The schema contract if found, otherwise null.</returns>
    ValueTask<JsonSchemaContract?> ResolveSchemaAsync(Type type, SchemaContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached schemas for the specified type.
    /// </summary>
    /// <param name="type">The type to invalidate schemas for.</param>
    void InvalidateSchema(Type type);

    /// <summary>
    /// Invalidates all cached schemas.
    /// </summary>
    void InvalidateAll();
}
