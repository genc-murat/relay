using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Schema provider that loads schemas from embedded resources.
/// </summary>
public sealed class EmbeddedResourceSchemaProvider : ISchemaProvider
{
    private readonly SchemaDiscoveryOptions _options;
    private readonly ILogger<EmbeddedResourceSchemaProvider> _logger;
    private readonly Assembly[] _assemblies;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddedResourceSchemaProvider"/> class.
    /// </summary>
    /// <param name="options">The schema discovery options.</param>
    /// <param name="assemblies">The assemblies to search for embedded resources. If null, searches the calling assembly.</param>
    /// <param name="logger">The logger instance.</param>
    public EmbeddedResourceSchemaProvider(
        SchemaDiscoveryOptions options,
        Assembly[]? assemblies = null,
        ILogger<EmbeddedResourceSchemaProvider>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _assemblies = assemblies ?? new[] { Assembly.GetCallingAssembly() };
        _logger = logger ?? NullLogger<EmbeddedResourceSchemaProvider>.Instance;
    }

    /// <inheritdoc />
    public int Priority => 50;

    /// <inheritdoc />
    public ValueTask<JsonSchemaContract?> TryGetSchemaAsync(Type type, SchemaContext context, CancellationToken cancellationToken = default)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!_options.EnableEmbeddedResources)
        {
            _logger.LogDebug("Embedded resources are disabled in schema discovery options");
            return new ValueTask<JsonSchemaContract?>((JsonSchemaContract?)null);
        }

        var resourceName = GenerateResourceName(type, context);
        _logger.LogDebug("Searching for embedded resource: {ResourceName}", resourceName);

        foreach (var assembly in _assemblies)
        {
            var schema = TryLoadFromAssembly(assembly, resourceName, context);
            if (schema != null)
            {
                _logger.LogDebug("Found schema in assembly: {AssemblyName}", assembly.GetName().Name);
                return new ValueTask<JsonSchemaContract?>(schema);
            }
        }

        _logger.LogDebug("Schema not found in any embedded resources for type: {TypeName}", type.Name);
        return new ValueTask<JsonSchemaContract?>((JsonSchemaContract?)null);
    }

    /// <summary>
    /// Tries to load a schema from an assembly's embedded resources.
    /// </summary>
    private JsonSchemaContract? TryLoadFromAssembly(Assembly assembly, string resourceName, SchemaContext context)
    {
        try
        {
            var manifestResourceNames = assembly.GetManifestResourceNames();
            var matchingResource = manifestResourceNames.FirstOrDefault(name =>
                name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase) ||
                name.Contains(resourceName.Replace(".schema.json", ""), StringComparison.OrdinalIgnoreCase));

            if (matchingResource == null)
            {
                _logger.LogTrace("Resource not found in assembly {AssemblyName}: {ResourceName}",
                    assembly.GetName().Name, resourceName);
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(matchingResource);
            if (stream == null)
            {
                _logger.LogWarning("Failed to open embedded resource stream: {ResourceName}", matchingResource);
                return null;
            }

            using var reader = new StreamReader(stream);
            var schemaContent = reader.ReadToEnd();

            if (string.IsNullOrWhiteSpace(schemaContent))
            {
                _logger.LogWarning("Embedded resource is empty: {ResourceName}", matchingResource);
                return null;
            }

            return new JsonSchemaContract
            {
                Schema = schemaContent,
                SchemaVersion = context.SchemaVersion ?? "http://json-schema.org/draft-07/schema#"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load embedded resource from assembly {AssemblyName}: {ResourceName}",
                assembly.GetName().Name, resourceName);
            return null;
        }
    }

    /// <summary>
    /// Generates a resource name for the schema based on the type and context.
    /// </summary>
    private string GenerateResourceName(Type type, SchemaContext context)
    {
        var resourceName = _options.NamingConvention
            .Replace("{TypeName}", type.Name)
            .Replace("{TypeNamespace}", type.Namespace ?? string.Empty)
            .Replace("{IsRequest}", context.IsRequest ? "request" : "response");

        return resourceName;
    }
}
