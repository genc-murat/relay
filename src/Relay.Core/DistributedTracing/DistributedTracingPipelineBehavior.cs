using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.DistributedTracing;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.DistributedTracing;

/// <summary>
/// A pipeline behavior that implements distributed tracing for requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class DistributedTracingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IDistributedTracingProvider _tracingProvider;
    private readonly ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IOptions<RelayOptions> _options;
    private readonly string _handlerKey;

    public DistributedTracingPipelineBehavior(
        IDistributedTracingProvider tracingProvider,
        ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<RelayOptions> options)
    {
        _tracingProvider = tracingProvider ?? throw new ArgumentNullException(nameof(tracingProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Get distributed tracing configuration
        var tracingOptions = GetDistributedTracingOptions();
        var traceAttribute = typeof(TRequest).GetCustomAttribute<TraceAttribute>();

        // Check if distributed tracing is enabled for this request
        if (!IsDistributedTracingEnabled(tracingOptions, traceAttribute))
        {
            return await next();
        }

        // Get tracing parameters
        var (traceRequest, traceResponse, operationName) = GetTracingParameters(tracingOptions, traceAttribute);

        // Start tracing activity
        var tags = new Dictionary<string, object?>
        {
            ["request.type"] = typeof(TRequest).FullName ?? typeof(TRequest).Name,
            ["handler.key"] = _handlerKey
        };

        using var activity = _tracingProvider.StartActivity(operationName, typeof(TRequest), null, tags);

        if (activity == null)
        {
            // Tracing is not enabled, just proceed
            return await next();
        }

        try
        {
            _logger.LogDebug("Starting distributed trace for {OperationName}", operationName);

            // Add request information to trace if enabled
            if (traceRequest)
            {
                AddRequestInfoToTrace(request);
            }

            // Execute the handler
            var response = await next();

            // Add response information to trace if enabled
            if (traceResponse)
            {
                AddResponseInfoToTrace(response);
            }

            // Set activity status to OK
            _tracingProvider.SetActivityStatus(ActivityStatusCode.Ok);

            _logger.LogDebug("Completed distributed trace for {OperationName}", operationName);

            return response;
        }
        catch (Exception ex)
        {
            // Record exception in trace
            if (tracingOptions.RecordExceptions)
            {
                _tracingProvider.RecordException(ex);
                _tracingProvider.SetActivityStatus(ActivityStatusCode.Error, ex.Message);
            }

            _logger.LogError(ex, "Distributed trace failed for {OperationName}", operationName);

            throw;
        }
    }

    private DistributedTracingOptions GetDistributedTracingOptions()
    {
        // Check for handler-specific overrides
        if (_options.Value.DistributedTracingOverrides.TryGetValue(_handlerKey, out var handlerOptions))
        {
            return handlerOptions;
        }

        // Return default options
        return _options.Value.DefaultDistributedTracingOptions;
    }

    private static bool IsDistributedTracingEnabled(DistributedTracingOptions tracingOptions, TraceAttribute? traceAttribute)
    {
        // If distributed tracing is explicitly disabled globally, return false
        if (!tracingOptions.EnableAutomaticDistributedTracing && traceAttribute == null)
        {
            return false;
        }

        // If distributed tracing is enabled globally or explicitly enabled with TraceAttribute, return true
        return tracingOptions.EnableAutomaticDistributedTracing || traceAttribute != null;
    }

    private static (bool traceRequest, bool traceResponse, string operationName) GetTracingParameters(
        DistributedTracingOptions tracingOptions, TraceAttribute? traceAttribute)
    {
        if (traceAttribute != null)
        {
            var operationName = traceAttribute.OperationName ?? $"Process {typeof(TRequest).Name}";
            return (traceAttribute.TraceRequest, traceAttribute.TraceResponse, operationName);
        }

        var defaultOperationName = $"Process {typeof(TRequest).Name}";
        return (tracingOptions.TraceRequests, tracingOptions.TraceResponses, defaultOperationName);
    }

    /// <summary>
    /// Adds detailed request information to the distributed trace.
    /// Extracts and serializes request properties, fields, and metadata.
    /// </summary>
    private void AddRequestInfoToTrace(TRequest request)
    {
        try
        {
            if (request == null)
            {
                _tracingProvider.AddActivityTags(new Dictionary<string, object?>
                {
                    ["request.value"] = "null"
                });
                return;
            }

            var tags = new Dictionary<string, object?>
            {
                ["request.type"] = typeof(TRequest).Name,
                ["request.type.fullname"] = typeof(TRequest).FullName
            };

            // Extract public properties
            var properties = ExtractObjectProperties(request);
            if (properties.Count > 0)
            {
                tags["request.properties"] = SerializeMetadata(properties);
            }

            // Extract public fields
            var fields = ExtractObjectFields(request);
            if (fields.Count > 0)
            {
                tags["request.fields"] = SerializeMetadata(fields);
            }

            // Add custom attributes if present
            var customAttributes = ExtractCustomAttributes(typeof(TRequest));
            if (customAttributes.Count > 0)
            {
                tags["request.attributes"] = SerializeMetadata(customAttributes);
            }

            // Add size information for large objects
            tags["request.has_value"] = true;

            _tracingProvider.AddActivityTags(tags);

            _logger.LogDebug("Added request info to trace: {RequestType}", typeof(TRequest).Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error adding request info to trace");
            // Continue tracing even if metadata extraction fails
            _tracingProvider.AddActivityTags(new Dictionary<string, object?>
            {
                ["request.info.extraction.error"] = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Adds detailed response information to the distributed trace.
    /// Extracts and serializes response properties, fields, and status information.
    /// </summary>
    private void AddResponseInfoToTrace(TResponse response)
    {
        try
        {
            if (response == null)
            {
                _tracingProvider.AddActivityTags(new Dictionary<string, object?>
                {
                    ["response.value"] = "null"
                });
                return;
            }

            var tags = new Dictionary<string, object?>
            {
                ["response.type"] = typeof(TResponse).Name,
                ["response.type.fullname"] = typeof(TResponse).FullName
            };

            // Extract public properties
            var properties = ExtractObjectProperties(response);
            if (properties.Count > 0)
            {
                tags["response.properties"] = SerializeMetadata(properties);
            }

            // Extract public fields
            var fields = ExtractObjectFields(response);
            if (fields.Count > 0)
            {
                tags["response.fields"] = SerializeMetadata(fields);
            }

            // Try to extract status information (common pattern)
            ExtractStatusInformation(response, tags);

            // Add custom attributes if present
            var customAttributes = ExtractCustomAttributes(typeof(TResponse));
            if (customAttributes.Count > 0)
            {
                tags["response.attributes"] = SerializeMetadata(customAttributes);
            }

            tags["response.has_value"] = true;

            _tracingProvider.AddActivityTags(tags);

            _logger.LogDebug("Added response info to trace: {ResponseType}", typeof(TResponse).Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error adding response info to trace");
            // Continue tracing even if metadata extraction fails
            _tracingProvider.AddActivityTags(new Dictionary<string, object?>
            {
                ["response.info.extraction.error"] = ex.GetType().Name
            });
        }
    }

    /// <summary>
    /// Extracts public properties from an object for tracing.
    /// </summary>
    private Dictionary<string, object?> ExtractObjectProperties<T>(T? obj)
    {
        var result = new Dictionary<string, object?>();

        if (obj == null)
            return result;

        try
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                try
                {
                    // Skip indexer properties
                    if (property.GetIndexParameters().Length > 0)
                        continue;

                    var value = property.GetValue(obj);

                    // Safely convert value to traceable format
                    result[property.Name] = SafeValueForTracing(value, property.PropertyType);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error extracting property {PropertyName}", property.Name);
                    result[property.Name] = $"<error: {ex.GetType().Name}>";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting object properties");
        }

        return result;
    }

    /// <summary>
    /// Extracts public fields from an object for tracing.
    /// </summary>
    private Dictionary<string, object?> ExtractObjectFields<T>(T? obj)
    {
        var result = new Dictionary<string, object?>();

        if (obj == null)
            return result;

        try
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    var value = field.GetValue(obj);
                    result[field.Name] = SafeValueForTracing(value, field.FieldType);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error extracting field {FieldName}", field.Name);
                    result[field.Name] = $"<error: {ex.GetType().Name}>";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting object fields");
        }

        return result;
    }

    /// <summary>
    /// Extracts custom attributes from a type for tracing.
    /// </summary>
    private Dictionary<string, object?> ExtractCustomAttributes(Type type)
    {
        var result = new Dictionary<string, object?>();

        try
        {
            var attributes = type.GetCustomAttributes(false);

            foreach (var attribute in attributes)
            {
                var attrName = attribute.GetType().Name;
                result[attrName] = attribute.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error extracting custom attributes");
        }

        return result;
    }

    /// <summary>
    /// Extracts common status information from response objects.
    /// Looks for common properties like Status, StatusCode, IsSuccess, etc.
    /// </summary>
    private void ExtractStatusInformation<T>(T? response, Dictionary<string, object?> tags)
    {
        if (response == null)
            return;

        var type = typeof(T);
        var statusProperties = new[]
        {
            "Status", "StatusCode", "Code",
            "IsSuccess", "Success", "IsSuccessful",
            "IsError", "Error", "ErrorMessage",
            "Message"
        };

        foreach (var propName in statusProperties)
        {
            try
            {
                var property = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property != null && property.CanRead)
                {
                    var value = property.GetValue(response);
                    if (value != null)
                    {
                        tags[$"response.{propName.ToLowerInvariant()}"] = SafeValueForTracing(value, property.PropertyType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error extracting status property {PropertyName}", propName);
            }
        }
    }

    /// <summary>
    /// Converts a value to a safe format for tracing.
    /// Handles primitives, strings, and complex types with serialization.
    /// </summary>
    private object? SafeValueForTracing(object? value, Type valueType)
    {
        if (value == null)
            return null;

        // Primitive types and strings are safe to include
        if (valueType.IsPrimitive || valueType == typeof(string) || valueType == typeof(decimal))
        {
            return value;
        }

        // Handle enums
        if (valueType.IsEnum)
        {
            return value.ToString();
        }

        // Handle DateTime and other common value types
        if (valueType == typeof(DateTime) || valueType == typeof(DateTimeOffset) ||
            valueType == typeof(TimeSpan) || valueType == typeof(Guid))
        {
            return value.ToString();
        }

        // Handle nullable types
        if (Nullable.GetUnderlyingType(valueType) is Type underlyingType)
        {
            return SafeValueForTracing(value, underlyingType);
        }

        // For complex objects, try JSON serialization with truncation
        try
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Truncate very large JSON strings
            const int maxLength = 500;
            if (json.Length > maxLength)
            {
                return json.Substring(0, maxLength) + "... <truncated>";
            }

            return json;
        }
        catch
        {
            // If serialization fails, fall back to ToString
            return value.ToString();
        }
    }

    /// <summary>
    /// Serializes metadata dictionary to a safe string format for tracing.
    /// </summary>
    private string SerializeMetadata(Dictionary<string, object?> metadata)
    {
        try
        {
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // Truncate very large metadata
            const int maxLength = 1000;
            if (json.Length > maxLength)
            {
                return json.Substring(0, maxLength) + "... <truncated>";
            }

            return json;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error serializing metadata for trace");
            return $"<serialization error: {ex.GetType().Name}>";
        }
    }
}