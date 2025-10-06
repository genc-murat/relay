using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Relay.Core.DistributedTracing;

/// <summary>
/// OpenTelemetry implementation of IDistributedTracingProvider.
/// </summary>
public class OpenTelemetryTracingProvider : IDistributedTracingProvider
{
    private readonly TracerProvider? _tracerProvider;
    private readonly string _serviceName;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryTracingProvider"/> class.
    /// </summary>
    /// <param name="tracerProvider">The OpenTelemetry tracer provider.</param>
    /// <param name="serviceName">The name of the service.</param>
    public OpenTelemetryTracingProvider(TracerProvider? tracerProvider = null, string serviceName = "Relay")
    {
        _tracerProvider = tracerProvider;
        _serviceName = serviceName;
    }

    /// <inheritdoc />
    public Activity? StartActivity(string operationName, Type requestType, string? correlationId = null, IDictionary<string, object?>? tags = null)
    {
        var activityName = $"{_serviceName}.{operationName}";
        var activity = Activity.Current?.Source.StartActivity(activityName, ActivityKind.Server)
                      ?? new ActivitySource(_serviceName).StartActivity(activityName, ActivityKind.Server);

        if (activity != null)
        {
            activity.SetTag("request.type", requestType.FullName ?? requestType.Name);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                activity.SetTag("correlation.id", correlationId);
            }

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }

        return activity;
    }

    /// <inheritdoc />
    public void AddActivityTags(IDictionary<string, object?> tags)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }
    }

    /// <inheritdoc />
    public void RecordException(Exception exception, bool escaped = false)
    {
        var activity = Activity.Current;
        if (activity != null && exception != null)
        {
            // Use the new recommended method instead of obsolete RecordException
            activity.AddException(exception);
        }
    }

    /// <inheritdoc />
    public void SetActivityStatus(ActivityStatusCode status, string? description = null)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(status, description);
        }
    }

    /// <inheritdoc />
    public string? GetCurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }

    /// <inheritdoc />
    public string? GetCurrentSpanId()
    {
        return Activity.Current?.SpanId.ToString();
    }
}