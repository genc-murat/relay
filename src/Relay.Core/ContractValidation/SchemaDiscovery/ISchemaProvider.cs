using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Interface for schema providers that load schemas from various sources.
/// </summary>
public interface ISchemaProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher values execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Attempts to load a schema for the specified type.
    /// </summary>
    /// <param name="type">The type to load a schema for.</param>
    /// <param name="context">The schema resolution context.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The schema contract if found, otherwise null.</returns>
    ValueTask<JsonSchemaContract?> TryGetSchemaAsync(Type type, SchemaContext context, CancellationToken cancellationToken = default);
}
