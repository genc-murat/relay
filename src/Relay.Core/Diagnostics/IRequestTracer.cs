using System;
using System.Collections.Generic;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Interface for request tracing functionality
/// </summary>
public interface IRequestTracer
{
    /// <summary>
    /// Starts tracing a new request
    /// </summary>
    /// <typeparam name="TRequest">The type of request being traced</typeparam>
    /// <param name="request">The request instance</param>
    /// <param name="correlationId">Optional correlation ID for distributed tracing</param>
    /// <returns>The created request trace</returns>
    RequestTrace StartTrace<TRequest>(TRequest request, string? correlationId = null);

    /// <summary>
    /// Gets the current active trace for the current execution context
    /// </summary>
    /// <returns>The active trace, or null if no trace is active</returns>
    RequestTrace? GetCurrentTrace();

    /// <summary>
    /// Adds a step to the current trace
    /// </summary>
    /// <param name="stepName">Name of the step</param>
    /// <param name="duration">How long the step took</param>
    /// <param name="category">Category of the step (Handler, Pipeline, etc.)</param>
    /// <param name="metadata">Optional metadata about the step</param>
    void AddStep(string stepName, TimeSpan duration, string category = "Unknown", object? metadata = null);

    /// <summary>
    /// Adds a step to the current trace with handler information
    /// </summary>
    /// <param name="stepName">Name of the step</param>
    /// <param name="duration">How long the step took</param>
    /// <param name="handlerType">Type of handler that executed</param>
    /// <param name="category">Category of the step</param>
    /// <param name="metadata">Optional metadata about the step</param>
    void AddHandlerStep(string stepName, TimeSpan duration, Type handlerType, string category = "Handler", object? metadata = null);

    /// <summary>
    /// Records an exception in the current trace
    /// </summary>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="stepName">Name of the step where the exception occurred</param>
    void RecordException(Exception exception, string? stepName = null);

    /// <summary>
    /// Completes the current trace
    /// </summary>
    /// <param name="success">Whether the request completed successfully</param>
    void CompleteTrace(bool success = true);

    /// <summary>
    /// Gets all completed traces within the specified time window
    /// </summary>
    /// <param name="since">Only return traces newer than this time</param>
    /// <returns>Collection of completed traces</returns>
    IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null);

    /// <summary>
    /// Clears all completed traces
    /// </summary>
    void ClearTraces();

    /// <summary>
    /// Gets the number of active traces
    /// </summary>
    int ActiveTraceCount { get; }

    /// <summary>
    /// Gets the number of completed traces
    /// </summary>
    int CompletedTraceCount { get; }

    /// <summary>
    /// Whether tracing is currently enabled
    /// </summary>
    bool IsEnabled { get; set; }
}