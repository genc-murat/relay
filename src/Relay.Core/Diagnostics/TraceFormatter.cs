using System;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Utilities for formatting request traces for display and logging
/// </summary>
public static class TraceFormatter
{
    /// <summary>
    /// Formats a request trace as a human-readable string
    /// </summary>
    /// <param name="trace">The trace to format</param>
    /// <param name="includeMetadata">Whether to include metadata in the output</param>
    /// <returns>Formatted trace string</returns>
    public static string FormatTrace(RequestTrace trace, bool includeMetadata = false)
    {
        if (trace == null)
            return "No trace available";

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"Request Trace: {trace.RequestType.Name}");
        sb.AppendLine($"Request ID: {trace.RequestId}");
        sb.AppendLine($"Correlation ID: {trace.CorrelationId}");
        sb.AppendLine($"Start Time: {trace.StartTime:yyyy-MM-dd HH:mm:ss.fff}");

        if (trace.IsCompleted)
        {
            sb.AppendLine($"End Time: {trace.EndTime:yyyy-MM-dd HH:mm:ss.fff}");
            sb.AppendLine($"Total Duration: {trace.TotalDuration?.TotalMilliseconds:F2}ms");
            sb.AppendLine($"Status: {(trace.IsSuccessful ? "Success" : "Failed")}");
        }
        else
        {
            sb.AppendLine("Status: In Progress");
        }

        if (trace.Exception != null)
        {
            sb.AppendLine($"Exception: {trace.Exception.GetType().Name} - {trace.Exception.Message}");
        }

        // Steps
        if (trace.Steps.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Execution Steps:");

            var totalTime = TimeSpan.Zero;
            for (int i = 0; i < trace.Steps.Count; i++)
            {
                var step = trace.Steps[i];
                totalTime += step.Duration;

                sb.AppendLine($"  {i + 1}. [{step.Category}] {step.Name}");
                sb.AppendLine($"     Duration: {step.Duration.TotalMilliseconds:F2}ms");
                sb.AppendLine($"     Timestamp: {step.Timestamp:HH:mm:ss.fff}");

                if (!string.IsNullOrEmpty(step.HandlerType))
                {
                    sb.AppendLine($"     Handler: {step.HandlerType}");
                }

                if (step.Exception != null)
                {
                    sb.AppendLine($"     Exception: {step.Exception.GetType().Name} - {step.Exception.Message}");
                }

                if (includeMetadata && step.Metadata != null)
                {
                    sb.AppendLine($"     Metadata: {FormatMetadata(step.Metadata)}");
                }

                if (i < trace.Steps.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine($"Total Step Time: {totalTime.TotalMilliseconds:F2}ms");
        }

        // Metadata
        if (includeMetadata && trace.Metadata.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Request Metadata:");
            foreach (var kvp in trace.Metadata)
            {
                sb.AppendLine($"  {kvp.Key}: {FormatMetadata(kvp.Value)}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a request trace as JSON
    /// </summary>
    /// <param name="trace">The trace to format</param>
    /// <param name="indented">Whether to use indented JSON formatting</param>
    /// <returns>JSON representation of the trace</returns>
    public static string FormatTraceAsJson(RequestTrace trace, bool indented = true)
    {
        if (trace == null)
            return "null";

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Create a serializable version of the trace
        var serializableTrace = new
        {
            requestId = trace.RequestId,
            requestType = trace.RequestType.Name,
            requestTypeFullName = trace.RequestType.FullName,
            startTime = trace.StartTime,
            endTime = trace.EndTime,
            totalDurationMs = trace.TotalDuration?.TotalMilliseconds,
            isCompleted = trace.IsCompleted,
            isSuccessful = trace.IsSuccessful,
            correlationId = trace.CorrelationId,
            exception = trace.Exception != null ? new
            {
                type = trace.Exception.GetType().Name,
                message = trace.Exception.Message,
                stackTrace = trace.Exception.StackTrace
            } : null,
            steps = trace.Steps.Select(s => new
            {
                name = s.Name,
                timestamp = s.Timestamp,
                durationMs = s.Duration.TotalMilliseconds,
                category = s.Category,
                handlerType = s.HandlerType,
                isSuccessful = s.IsSuccessful,
                exception = s.Exception != null ? new
                {
                    type = s.Exception.GetType().Name,
                    message = s.Exception.Message
                } : null,
                metadata = s.Metadata
            }).ToArray(),
            metadata = trace.Metadata
        };

        return JsonSerializer.Serialize(serializableTrace, options);
    }

    /// <summary>
    /// Creates a summary of multiple traces
    /// </summary>
    /// <param name="traces">The traces to summarize</param>
    /// <returns>Summary string</returns>
    public static string FormatTraceSummary(System.Collections.Generic.IEnumerable<RequestTrace> traces)
    {
        var traceList = traces.ToList();
        if (!traceList.Any())
            return "No traces available";

        var sb = new StringBuilder();

        var totalTraces = traceList.Count;
        var completedTraces = traceList.Count(t => t.IsCompleted);
        var successfulTraces = traceList.Count(t => t.IsSuccessful);
        var failedTraces = traceList.Count(t => t.IsCompleted && !t.IsSuccessful);

        sb.AppendLine($"Trace Summary ({totalTraces} traces):");
        sb.AppendLine($"  Completed: {completedTraces}");
        sb.AppendLine($"  Successful: {successfulTraces}");
        sb.AppendLine($"  Failed: {failedTraces}");
        sb.AppendLine($"  In Progress: {totalTraces - completedTraces}");

        if (completedTraces > 0)
        {
            var completedList = traceList.Where(t => t.IsCompleted && t.TotalDuration.HasValue).ToList();
            if (completedList.Any())
            {
                var avgDuration = completedList.Average(t => t.TotalDuration!.Value.TotalMilliseconds);
                var minDuration = completedList.Min(t => t.TotalDuration!.Value.TotalMilliseconds);
                var maxDuration = completedList.Max(t => t.TotalDuration!.Value.TotalMilliseconds);

                sb.AppendLine();
                sb.AppendLine("Performance Summary:");
                sb.AppendLine($"  Average Duration: {avgDuration:F2}ms");
                sb.AppendLine($"  Min Duration: {minDuration:F2}ms");
                sb.AppendLine($"  Max Duration: {maxDuration:F2}ms");
            }
        }

        // Request type breakdown
        var requestTypes = traceList.GroupBy(t => t.RequestType.Name)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        if (requestTypes.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Request Types:");
            foreach (var rt in requestTypes)
            {
                sb.AppendLine($"  {rt.Type}: {rt.Count}");
            }
        }

        return sb.ToString();
    }

    private static string FormatMetadata(object? metadata)
    {
        if (metadata == null)
            return "null";

        if (metadata is string str)
            return str;

        try
        {
            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return metadata.ToString() ?? "null";
        }
    }
}