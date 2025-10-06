using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Relay.Core.DistributedTracing;

/// <summary>
/// Interface for distributed tracing providers.
/// </summary>
public interface IDistributedTracingProvider
{
    /// <summary>
    /// Starts a new activity for distributed tracing.
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="requestType">The type of the request.</param>
    /// <param name="correlationId">The correlation ID for the operation.</param>
    /// <param name="tags">Additional tags for the activity.</param>
    /// <returns>The created activity, or null if tracing is not enabled.</returns>
    Activity? StartActivity(string operationName, Type requestType, string? correlationId = null, IDictionary<string, object?>? tags = null);

    /// <summary>
    /// Adds tags to the current activity.
    /// </summary>
    /// <param name="tags">The tags to add.</param>
    void AddActivityTags(IDictionary<string, object?> tags);

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="escaped">Whether the exception was escaped.</param>
    void RecordException(Exception exception, bool escaped = false);

    /// <summary>
    /// Sets the status of the current activity.
    /// </summary>
    /// <param name="status">The status to set.</param>
    /// <param name="description">Optional description for the status.</param>
    void SetActivityStatus(ActivityStatusCode status, string? description = null);

    /// <summary>
    /// Gets the current trace ID.
    /// </summary>
    string? GetCurrentTraceId();

    /// <summary>
    /// Gets the current span ID.
    /// </summary>
    string? GetCurrentSpanId();
}