using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Schema provider that loads schemas from the file system.
/// </summary>
public sealed class FileSystemSchemaProvider : ISchemaProvider
{
    private readonly SchemaDiscoveryOptions _options;
    private readonly ILogger<FileSystemSchemaProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemSchemaProvider"/> class.
    /// </summary>
    /// <param name="options">The schema discovery options.</param>
    /// <param name="logger">The logger instance.</param>
    public FileSystemSchemaProvider(SchemaDiscoveryOptions options, ILogger<FileSystemSchemaProvider>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<FileSystemSchemaProvider>.Instance;
    }

    /// <inheritdoc />
    public int Priority => 100;

    /// <inheritdoc />
    public async ValueTask<JsonSchemaContract?> TryGetSchemaAsync(Type type, SchemaContext context, CancellationToken cancellationToken = default)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (_options.SchemaDirectories.Count == 0)
        {
            _logger.LogDebug("No schema directories configured for FileSystemSchemaProvider");
            return null;
        }

        var fileName = GenerateFileName(type, context);
        _logger.LogDebug("Searching for schema file: {FileName}", fileName);

        foreach (var directory in _options.SchemaDirectories)
        {
            var filePath = Path.Combine(directory, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogTrace("Schema file not found at: {FilePath}", filePath);
                continue;
            }

            try
            {
                _logger.LogDebug("Loading schema from file: {FilePath}", filePath);
                var schemaContent = await File.ReadAllTextAsync(filePath, cancellationToken);

                if (string.IsNullOrWhiteSpace(schemaContent))
                {
                    _logger.LogWarning("Schema file is empty: {FilePath}", filePath);
                    continue;
                }

                return new JsonSchemaContract
                {
                    Schema = schemaContent,
                    SchemaVersion = context.SchemaVersion ?? "http://json-schema.org/draft-07/schema#"
                };
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Failed to read schema file: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Access denied to schema file: {FilePath}", filePath);
            }
        }

        _logger.LogDebug("Schema not found in any configured directory for type: {TypeName}", type.Name);
        return null;
    }

    /// <summary>
    /// Generates a file name for the schema based on the type and context.
    /// </summary>
    private string GenerateFileName(Type type, SchemaContext context)
    {
        var fileName = _options.NamingConvention
            .Replace("{TypeName}", type.Name)
            .Replace("{TypeNamespace}", type.Namespace ?? string.Empty)
            .Replace("{IsRequest}", context.IsRequest ? "request" : "response");

        return fileName;
    }
}
