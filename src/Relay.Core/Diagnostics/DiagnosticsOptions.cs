using System;

namespace Relay.Core.Diagnostics;

/// <summary>
/// Configuration options for Relay diagnostics
/// </summary>
public class DiagnosticsOptions
{
    /// <summary>
    /// Whether request tracing is enabled
    /// </summary>
    public bool EnableRequestTracing { get; set; } = false;
    
    /// <summary>
    /// Whether performance metrics collection is enabled
    /// </summary>
    public bool EnablePerformanceMetrics { get; set; } = false;
    
    /// <summary>
    /// Maximum number of completed traces to retain in memory
    /// </summary>
    public int TraceBufferSize { get; set; } = 1000;
    
    /// <summary>
    /// How long to retain performance metrics
    /// </summary>
    public TimeSpan MetricsRetentionPeriod { get; set; } = TimeSpan.FromHours(1);
    
    /// <summary>
    /// Whether to include request/response data in traces (may contain sensitive information)
    /// </summary>
    public bool IncludeRequestData { get; set; } = false;
    
    /// <summary>
    /// Whether to include exception stack traces in traces
    /// </summary>
    public bool IncludeStackTraces { get; set; } = true;
    
    /// <summary>
    /// Whether to enable diagnostic endpoints for runtime inspection
    /// </summary>
    public bool EnableDiagnosticEndpoints { get; set; } = false;
    
    /// <summary>
    /// Base path for diagnostic endpoints (e.g., "/relay")
    /// </summary>
    public string DiagnosticEndpointBasePath { get; set; } = "/relay";
    
    /// <summary>
    /// Whether diagnostic endpoints require authentication
    /// </summary>
    public bool RequireAuthentication { get; set; } = true;
    
    /// <summary>
    /// Minimum duration threshold for recording trace steps (to reduce noise)
    /// </summary>
    public TimeSpan MinimumStepDuration { get; set; } = TimeSpan.Zero;
    
    /// <summary>
    /// Whether to automatically clear old traces based on retention period
    /// </summary>
    public bool AutoClearOldTraces { get; set; } = true;
    
    /// <summary>
    /// How often to clean up old traces and metrics
    /// </summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}