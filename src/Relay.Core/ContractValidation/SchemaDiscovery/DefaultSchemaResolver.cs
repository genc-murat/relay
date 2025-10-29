using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Observability;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.SchemaDiscovery;

/// <summary>
/// Default implementation of schema resolver that orchestrates multiple providers and caching.
/// </summary>
public sealed class DefaultSchemaResolver : ISchemaResolver
{
    private readonly IEnumerable<ISchemaProvider> _providers;
    private readonly ISchemaCache? _cache;
    private readonly ILogger<DefaultSchemaResolver> _logger;
    private readonly ContractValidationMetrics? _metrics;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<string, string> _originalSchemaStrings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSchemaResolver"/> class.
    /// </summary>
    /// <param name="providers">The schema providers to use for resolution.</param>
    /// <param name="cache">Optional schema cache for performance optimization.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metrics">Optional metrics collector.</param>
    public DefaultSchemaResolver(
        IEnumerable<ISchemaProvider> providers,
        ISchemaCache? cache = null,
        ILogger<DefaultSchemaResolver>? logger = null,
        ContractValidationMetrics? metrics = null)
    {
        _providers = providers?.OrderByDescending(p => p.Priority).ToList()
            ?? throw new ArgumentNullException(nameof(providers));
        _cache = cache;
        _logger = logger ?? NullLogger<DefaultSchemaResolver>.Instance;
        _metrics = metrics;
    }

    /// <inheritdoc />
    public async ValueTask<JsonSchemaContract?> ResolveSchemaAsync(
        Type type,
        SchemaContext context,
        CancellationToken cancellationToken = default)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "SchemaResolver.Resolve",
            ActivityKind.Internal);

        activity?.SetTag("type_name", type.Name);
        activity?.SetTag("is_request", context.IsRequest);

        var cacheKey = GenerateCacheKey(type, context);

        // Check cache first
        if (_cache != null)
        {
            var cachedSchema = TryGetFromCache(cacheKey);
            if (cachedSchema != null)
            {
                _logger.LogDebug(
                    ValidationEventIds.SchemaCacheHit,
                    "Schema found in cache for type: {TypeName}",
                    type.Name);

                activity?.SetTag("cache_hit", true);
                return cachedSchema;
            }

            activity?.SetTag("cache_hit", false);
        }

        // Get or create a semaphore for this cache key to handle concurrent access
        var semaphore = _semaphores.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check the cache after acquiring the semaphore
            // This ensures that if another thread already resolved and cached the schema,
            // we'll get it from the cache instead of doing the work again
            if (_cache != null)
            {
                var cachedSchema = TryGetFromCache(cacheKey);
                if (cachedSchema != null)
                {
                    _logger.LogDebug(
                        ValidationEventIds.SchemaCacheHit,
                        "Schema found in cache for type: {TypeName} (after acquiring semaphore)",
                        type.Name);

                    activity?.SetTag("cache_hit", true);
                    return cachedSchema;
                }
            }

            // If still not in cache, do the resolution work
            var result = await ResolveSchemaInternalAsync(type, context, cacheKey, cancellationToken);

            activity?.SetTag("success", result != null);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Internal method to resolve schema and handle caching.
    /// </summary>
    private async Task<JsonSchemaContract?> ResolveSchemaInternalAsync(
        Type type,
        SchemaContext context,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        using var activity = ContractValidationActivitySource.Instance.StartActivity(
            "SchemaResolver.ResolveInternal",
            ActivityKind.Internal);

        activity?.SetTag("type_name", type.Name);
        activity?.SetTag("is_request", context.IsRequest);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug(
                ValidationEventIds.SchemaResolutionStarted,
                "Starting schema resolution for type: {TypeName}, IsRequest: {IsRequest}",
                type.Name,
                context.IsRequest);

            // Check cache again inside the task in case it was populated while waiting to be scheduled
            if (_cache != null)
            {
                var cachedSchema = TryGetFromCache(cacheKey);
                if (cachedSchema != null)
                {
                    stopwatch.Stop();

                    _logger.LogDebug(
                        ValidationEventIds.SchemaCacheHit,
                        "Schema found in cache for type: {TypeName} in {Duration}ms",
                        type.Name,
                        stopwatch.ElapsedMilliseconds);

                    _metrics?.RecordSchemaResolution(
                        type.Name,
                        "cache",
                        success: true,
                        stopwatch.Elapsed.TotalMilliseconds);

                    return cachedSchema;
                }

                _logger.LogDebug(
                    ValidationEventIds.SchemaCacheMiss,
                    "Schema not found in cache for type: {TypeName}",
                    type.Name);
            }

            // Try each provider in priority order
            _logger.LogDebug(
                ValidationEventIds.SchemaDiscoveryStarted,
                "Attempting schema discovery for type: {TypeName} using {ProviderCount} providers",
                type.Name,
                _providers.Count());

            foreach (var provider in _providers)
            {
                var providerStopwatch = Stopwatch.StartNew();
                try
                {
                    var schema = await provider.TryGetSchemaAsync(type, context, cancellationToken);
                    providerStopwatch.Stop();

                    if (schema != null)
                    {
                        stopwatch.Stop();

                        _logger.LogInformation(
                            ValidationEventIds.SchemaResolutionCompleted,
                            "Schema resolved for type {TypeName} using provider {ProviderType} in {Duration}ms",
                            type.Name,
                            provider.GetType().Name,
                            stopwatch.ElapsedMilliseconds);

                        _metrics?.RecordSchemaResolution(
                            type.Name,
                            provider.GetType().Name,
                            success: true,
                            stopwatch.Elapsed.TotalMilliseconds);

                        activity?.SetTag("provider_type", provider.GetType().Name);
                        activity?.SetTag("success", true);

                        // Cache the resolved schema
                        if (_cache != null && !string.IsNullOrWhiteSpace(schema.Schema))
                        {
                            CacheSchema(cacheKey, schema);
                        }

                        return schema;
                    }

                    _logger.LogDebug(
                        "Provider {ProviderType} did not find schema for type {TypeName} (took {Duration}ms)",
                        provider.GetType().Name,
                        type.Name,
                        providerStopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    providerStopwatch.Stop();

                    _logger.LogWarning(
                        ValidationEventIds.SchemaResolutionFailed,
                        ex,
                        "Provider {ProviderType} failed to resolve schema for type {TypeName} after {Duration}ms",
                        provider.GetType().Name,
                        type.Name,
                        providerStopwatch.ElapsedMilliseconds);

                    _metrics?.RecordSchemaResolution(
                        type.Name,
                        provider.GetType().Name,
                        success: false,
                        providerStopwatch.Elapsed.TotalMilliseconds);
                }
            }

            stopwatch.Stop();

            _logger.LogWarning(
                ValidationEventIds.SchemaResolutionFailed,
                "No schema found for type {TypeName} after trying all {ProviderCount} providers in {Duration}ms",
                type.Name,
                _providers.Count(),
                stopwatch.ElapsedMilliseconds);

            _metrics?.RecordSchemaResolution(
                type.Name,
                null,
                success: false,
                stopwatch.Elapsed.TotalMilliseconds);

            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ValidationEventIds.SchemaResolutionFailed,
                ex,
                "Schema resolution failed for type {TypeName} after {Duration}ms",
                type.Name,
                stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            throw;
        }
    }

    /// <inheritdoc />
    public void InvalidateSchema(Type type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (_cache == null)
        {
            return;
        }

        // Invalidate both request and response schemas for the type
        var requestKey = GenerateCacheKey(type, new SchemaContext { RequestType = type, IsRequest = true });
        var responseKey = GenerateCacheKey(type, new SchemaContext { RequestType = type, IsRequest = false });

        _cache.Remove(requestKey);
        _cache.Remove(responseKey);
        _originalSchemaStrings.TryRemove(requestKey, out _);
        _originalSchemaStrings.TryRemove(responseKey, out _);

        _logger.LogDebug("Invalidated cached schemas for type: {TypeName}", type.Name);
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        if (_cache == null)
        {
            return;
        }

        _cache.Clear();
        _originalSchemaStrings.Clear();
        _logger.LogInformation("Invalidated all cached schemas");
    }

    /// <summary>
    /// Generates a cache key for a type and context.
    /// </summary>
    private static string GenerateCacheKey(Type type, SchemaContext context)
    {
        var key = $"{type.FullName}:{context.IsRequest}";
        if (!string.IsNullOrWhiteSpace(context.SchemaVersion))
        {
            key += $":{context.SchemaVersion}";
        }
        return key;
    }

    /// <summary>
    /// Tries to get a schema from the cache.
    /// </summary>
    private JsonSchemaContract? TryGetFromCache(string cacheKey)
    {
        // Always call cache.Get() first to ensure metrics are tracked properly
        var cachedJsonSchema = _cache?.Get(cacheKey);

        // If we have the original schema string, use it for better fidelity
        if (_originalSchemaStrings.TryGetValue(cacheKey, out var originalSchemaString))
        {
            return new JsonSchemaContract { Schema = originalSchemaString };
        }

        // Otherwise, try to serialize the JsonSchema object if it exists in cache
        if (cachedJsonSchema != null)
        {
            try
            {
                // Attempt to serialize the JsonSchema back to string
                // Using a safe approach with proper options for JsonSchema.Net
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                // To prevent exceptions in System.Text.Json serialization of JsonSchema
                // we need to make sure the serialization is robust
                var schemaString = System.Text.Json.JsonSerializer.Serialize(cachedJsonSchema, options);

                // Normalize line endings to LF (\n) for consistency across platforms
                schemaString = schemaString.Replace("\r\n", "\n").Replace("\r", "\n");

                return new JsonSchemaContract { Schema = schemaString };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to serialize JsonSchema back to string for cache key: {CacheKey}. " +
                    "This may indicate JsonSchema.Net version incompatibility with System.Text.Json",
                    cacheKey);

                // Since serialization failed, we can't return a valid schema
                // This indicates an underlying problem with the serialization setup
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Caches a schema.
    /// </summary>
    private void CacheSchema(string cacheKey, JsonSchemaContract schema)
    {
        try
        {
            // Parse the schema string and cache the Json.Schema.JsonSchema object
            var jsonSchema = Json.Schema.JsonSchema.FromText(schema.Schema);
            _cache?.Set(cacheKey, jsonSchema);
            
            // Also store the original string to preserve exact formatting when returning from cache
            _originalSchemaStrings[cacheKey] = schema.Schema;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache schema for key: {CacheKey}", cacheKey);
        }
    }
}
