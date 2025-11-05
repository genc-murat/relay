using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a performance profiling report with multiple export formats.
/// </summary>
public class ProfileReport
{
    /// <summary>
    /// Gets the profile session this report is based on.
    /// </summary>
    public ProfileSession Session { get; }

    /// <summary>
    /// Gets the thresholds used for this report.
    /// </summary>
    public PerformanceThresholds Thresholds { get; }

    /// <summary>
    /// Gets the warnings generated during report creation.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileReport"/> class.
    /// </summary>
    /// <param name="session">The profile session.</param>
    /// <param name="thresholds">The performance thresholds.</param>
    public ProfileReport(ProfileSession session, PerformanceThresholds? thresholds = null)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Thresholds = thresholds ?? new PerformanceThresholds();
        Warnings = GenerateWarnings();
    }

    /// <summary>
    /// Exports the report to console format.
    /// </summary>
    /// <returns>The console-formatted report.</returns>
    public string ToConsole()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Performance Profile Report: {Session.SessionName}");
        sb.AppendLine($"Session Duration: {Session.Duration.TotalMilliseconds:F2} ms");
        sb.AppendLine($"Total Memory Used: {FormatBytes(Session.TotalMemoryUsed)}");
        sb.AppendLine($"Total Allocations: {Session.TotalAllocations:N0}");
        sb.AppendLine($"Operations Count: {Session.Operations.Count}");
        sb.AppendLine($"Average Operation Duration: {Session.AverageOperationDuration.TotalMilliseconds:F2} ms");
        sb.AppendLine();

        if (Session.Operations.Any())
        {
            sb.AppendLine("Operation Details:");
            sb.AppendLine("--------------------------------------------------------------------------------");
            sb.AppendLine("Operation Name                    | Duration (ms) | Memory (bytes) | Allocations");
            sb.AppendLine("--------------------------------------------------------------------------------");

            foreach (var op in Session.Operations.OrderByDescending(o => o.Duration))
            {
                sb.AppendLine($"{op.OperationName,-35} | {op.Duration.TotalMilliseconds,12:F2} | {op.MemoryUsed,14:N0} | {op.Allocations,11:N0}");
            }
        }

        if (Warnings.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Warnings:");
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"  - {warning}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports the report to JSON format.
    /// </summary>
    /// <returns>The JSON-formatted report.</returns>
    public string ToJson()
    {
        var data = new
        {
            SessionName = Session.SessionName,
            StartTime = Session.StartTime,
            EndTime = Session.EndTime,
            DurationMs = Session.Duration.TotalMilliseconds,
            TotalMemoryUsed = Session.TotalMemoryUsed,
            TotalAllocations = Session.TotalAllocations,
            OperationCount = Session.Operations.Count,
            AverageOperationDurationMs = Session.AverageOperationDuration.TotalMilliseconds,
            Operations = Session.Operations.Select(op => new
            {
                OperationName = op.OperationName,
                DurationMs = op.Duration.TotalMilliseconds,
                MemoryUsed = op.MemoryUsed,
                Allocations = op.Allocations,
                StartTime = op.StartTime,
                EndTime = op.EndTime,
                MemoryPerMs = op.MemoryPerMs,
                AllocationsPerMs = op.AllocationsPerMs
            }),
            Warnings = Warnings
        };

        return JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    /// <summary>
    /// Exports the report to CSV format.
    /// </summary>
    /// <returns>The CSV-formatted report.</returns>
    public string ToCsv()
    {
        var sb = new StringBuilder();

        // Session summary
        sb.AppendLine("Session Summary");
        sb.AppendLine($"Session Name,{Session.SessionName}");
        sb.AppendLine($"Start Time,{Session.StartTime:O}");
        sb.AppendLine($"End Time,{Session.EndTime:O}");
        sb.AppendLine($"Duration (ms),{Session.Duration.TotalMilliseconds:F2}");
        sb.AppendLine($"Total Memory Used,{Session.TotalMemoryUsed}");
        sb.AppendLine($"Total Allocations,{Session.TotalAllocations}");
        sb.AppendLine($"Operation Count,{Session.Operations.Count}");
        sb.AppendLine($"Average Operation Duration (ms),{Session.AverageOperationDuration.TotalMilliseconds:F2}");
        sb.AppendLine();

        // Operations
        sb.AppendLine("Operations");
        sb.AppendLine("Operation Name,Duration (ms),Memory Used,Allocations,Start Time,End Time,Memory/ms,Allocations/ms");

        foreach (var op in Session.Operations)
        {
            sb.AppendLine($"{EscapeCsv(op.OperationName)},{op.Duration.TotalMilliseconds:F2},{op.MemoryUsed},{op.Allocations},{op.StartTime:O},{op.EndTime:O},{op.MemoryPerMs:F2},{op.AllocationsPerMs:F2}");
        }

        // Warnings
        if (Warnings.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Warnings");
            foreach (var warning in Warnings)
            {
                sb.AppendLine($"\"{warning.Replace("\"", "\"\"")}\"");
            }
        }

        return sb.ToString();
    }

    private IReadOnlyList<string> GenerateWarnings()
    {
        var warnings = new List<string>();

        if (Thresholds.MaxDuration.HasValue && Session.Duration > Thresholds.MaxDuration.Value)
        {
            warnings.Add($"Session duration ({Session.Duration.TotalMilliseconds:F2} ms) exceeds threshold ({Thresholds.MaxDuration.Value.TotalMilliseconds:F2} ms)");
        }

        if (Thresholds.MaxMemory.HasValue && Session.TotalMemoryUsed > Thresholds.MaxMemory.Value)
        {
            warnings.Add($"Total memory usage ({FormatBytes(Session.TotalMemoryUsed)}) exceeds threshold ({FormatBytes(Thresholds.MaxMemory.Value)})");
        }

        if (Thresholds.MaxAllocations.HasValue && Session.TotalAllocations > Thresholds.MaxAllocations.Value)
        {
            warnings.Add($"Total allocations ({Session.TotalAllocations:N0}) exceed threshold ({Thresholds.MaxAllocations.Value:N0})");
        }

        foreach (var op in Session.Operations)
        {
            if (Thresholds.MaxOperationDuration.HasValue && op.Duration > Thresholds.MaxOperationDuration.Value)
            {
                warnings.Add($"Operation '{op.OperationName}' duration ({op.Duration.TotalMilliseconds:F2} ms) exceeds threshold ({Thresholds.MaxOperationDuration.Value.TotalMilliseconds:F2} ms)");
            }

            if (Thresholds.MaxOperationMemory.HasValue && op.MemoryUsed > Thresholds.MaxOperationMemory.Value)
            {
                warnings.Add($"Operation '{op.OperationName}' memory usage ({FormatBytes(op.MemoryUsed)}) exceeds threshold ({FormatBytes(Thresholds.MaxOperationMemory.Value)})");
            }
        }

        return warnings;
    }

    private static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{bytes / (double)GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / (double)MB:F2} MB";
        if (bytes >= KB)
            return $"{bytes / (double)KB:F2} KB";

        return $"{bytes} bytes";
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}

/// <summary>
/// Defines performance thresholds for profiling reports.
/// </summary>
public class PerformanceThresholds
{
    /// <summary>
    /// Gets or sets the maximum allowed session duration.
    /// </summary>
    public TimeSpan? MaxDuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed memory usage for the session.
    /// </summary>
    public long? MaxMemory { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed allocations for the session.
    /// </summary>
    public long? MaxAllocations { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed duration for individual operations.
    /// </summary>
    public TimeSpan? MaxOperationDuration { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed memory usage for individual operations.
    /// </summary>
    public long? MaxOperationMemory { get; set; }
}