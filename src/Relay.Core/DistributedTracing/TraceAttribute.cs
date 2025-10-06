using System;

namespace Relay.Core.DistributedTracing;

/// <summary>
/// Attribute to mark handlers or requests that should have distributed tracing applied.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class TraceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to trace the request.
    /// </summary>
    public bool TraceRequest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to trace the response.
    /// </summary>
    public bool TraceResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets the operation name for tracing.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceAttribute"/> class.
    /// </summary>
    public TraceAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceAttribute"/> class.
    /// </summary>
    /// <param name="traceRequest">Whether to trace the request.</param>
    /// <param name="traceResponse">Whether to trace the response.</param>
    public TraceAttribute(bool traceRequest, bool traceResponse)
    {
        TraceRequest = traceRequest;
        TraceResponse = traceResponse;
    }
}