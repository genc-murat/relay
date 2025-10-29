using System;
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

        var stopwatch = Stopwatch.StartNew();
        var cacheKey = GenerateCacheKey(type, context);

        try
        {
            _logger.LogDebug(
                ValidationEventIds.SchemaResolutionStarted,
                "Starting schema resolution for type: {TypeName}, IsRequest: {IsRequest}",
                type.Name,
                context.IsRequest);

            // Try to get from cache first
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

                    activity?.SetTag("cache_hit", true);

                    return cachedSchema;
                }

                _logger.LogDebug(
                    ValidationEventIds.SchemaCacheMiss,
                    "Schema not found in cache for type: {TypeName}",
                    type.Name);

                activity?.SetTag("cache_hit", false);
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

            activity?.SetTag("success", false);

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
        try
        {
            var cachedJsonSchema = _cache?.Get(cacheKey);
            if (cachedJsonSchema != null)
            {
                // Convert Json.Schema.JsonSchema back to JsonSchemaContract
                // Note: The cache stores JsonSchema objects, but we need to return JsonSchemaContract
                // For now, we'll return null and let the provider load it
                // This will be improved when we integrate the cache more tightly
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve schema from cache for key: {CacheKey}", cacheKey);
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
            // Parse the schema and cache it
            // Note: This requires converting JsonSchemaContract to Json.Schema.JsonSchema
            // For now, we'll skip caching and improve this in a future iteration
            _logger.LogDebug("Schema caching will be improved in future iteration");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache schema for key: {CacheKey}", cacheKey);
        }
    }
}
