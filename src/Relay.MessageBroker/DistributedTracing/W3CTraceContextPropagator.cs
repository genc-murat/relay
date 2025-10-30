using System.Diagnostics;

namespace Relay.MessageBroker.DistributedTracing;

/// <summary>
/// Implements W3C Trace Context propagation for message headers.
/// </summary>
public static class W3CTraceContextPropagator
{
    private const string TraceParentHeaderName = "traceparent";
    private const string TraceStateHeaderName = "tracestate";

    /// <summary>
    /// Injects the current trace context into message headers.
    /// </summary>
    /// <param name="headers">The message headers dictionary.</param>
    /// <param name="activity">The current activity (span).</param>
    public static void Inject(Dictionary<string, object> headers, Activity? activity)
    {
        if (activity == null)
            return;

        // Format: version-traceId-spanId-traceFlags
        // Example: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
        var traceParent = $"00-{activity.TraceId.ToHexString()}-{activity.SpanId.ToHexString()}-{(activity.Recorded ? "01" : "00")}";
        headers[TraceParentHeaderName] = traceParent;

        // Add tracestate if present
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            headers[TraceStateHeaderName] = activity.TraceStateString;
        }
    }

    /// <summary>
    /// Extracts trace context from message headers.
    /// </summary>
    /// <param name="headers">The message headers dictionary.</param>
    /// <returns>A tuple containing the trace ID, span ID, and trace flags, or null if not found.</returns>
    public static (ActivityTraceId TraceId, ActivitySpanId SpanId, ActivityTraceFlags TraceFlags)? Extract(Dictionary<string, object>? headers)
    {
        if (headers == null || !headers.TryGetValue(TraceParentHeaderName, out var traceParentObj))
            return null;

        var traceParent = traceParentObj?.ToString();
        if (string.IsNullOrEmpty(traceParent))
            return null;

        // Parse traceparent: version-traceId-spanId-traceFlags
        var parts = traceParent.Split('-');
        if (parts.Length != 4)
            return null;

        try
        {
            var traceId = ActivityTraceId.CreateFromString(parts[1].AsSpan());
            var spanId = ActivitySpanId.CreateFromString(parts[2].AsSpan());
            var traceFlags = parts[3] == "01" ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;

            return (traceId, spanId, traceFlags);
        }
        catch
        {
            // Invalid trace context format
            return null;
        }
    }

    /// <summary>
    /// Extracts the tracestate from message headers.
    /// </summary>
    /// <param name="headers">The message headers dictionary.</param>
    /// <returns>The tracestate string, or null if not found.</returns>
    public static string? ExtractTraceState(Dictionary<string, object>? headers)
    {
        if (headers == null || !headers.TryGetValue(TraceStateHeaderName, out var traceStateObj))
            return null;

        return traceStateObj?.ToString();
    }
}
