using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Unified configuration options for all Relay telemetry and logging
/// </summary>
public sealed class RelayTelemetryOptions
{
    /// <summary>
    /// Gets or sets whether telemetry is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable distributed tracing.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable metrics collection.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for telemetry.
    /// </summary>
    public string ServiceName { get; set; } = "Relay";

    /// <summary>
    /// Gets or sets the service version.
    /// </summary>
    public string? ServiceVersion { get; set; }

    /// <summary>
    /// Gets or sets the service namespace.
    /// </summary>
    public string? ServiceNamespace { get; set; }

    /// <summary>
    /// Gets or sets the component name (Core, MessageBroker, etc.)
    /// </summary>
    public string Component { get; set; } = RelayTelemetryConstants.Components.Core;

    /// <summary>
    /// Gets or sets whether to capture message payloads in traces.
    /// Warning: May expose sensitive data.
    /// </summary>
    public bool CaptureMessagePayloads { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum payload size to capture (in bytes).
    /// </summary>
    public int MaxPayloadSizeBytes { get; set; } = 1024;

    /// <summary>
    /// Gets or sets whether to capture message headers.
    /// </summary>
    public bool CaptureMessageHeaders { get; set; } = true;

    /// <summary>
    /// Gets or sets the header keys to exclude from capture.
    /// </summary>
    public List<string> ExcludedHeaderKeys { get; set; } = new()
    {
        "Authorization",
        "Password",
        "Secret",
        "Token",
        "ApiKey",
        "X-API-Key"
    };

    /// <summary>
    /// Gets or sets whether to capture stack traces on errors.
    /// </summary>
    public bool CaptureStackTraces { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to propagate trace context.
    /// </summary>
    public bool PropagateTraceContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the trace context propagation format.
    /// </summary>
    public TraceContextFormat TraceContextFormat { get; set; } = TraceContextFormat.W3C;

    /// <summary>
    /// Gets or sets custom resource attributes.
    /// </summary>
    public Dictionary<string, object> ResourceAttributes { get; set; } = new();

    /// <summary>
    /// Gets or sets the sampling rate (0.0 to 1.0).
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets whether to use batch processing for telemetry data.
    /// </summary>
    public bool UseBatchProcessing { get; set; } = true;

    /// <summary>
    /// Gets or sets the batch size for telemetry processing.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the batch timeout.
    /// </summary>
    public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the OpenTelemetry exporters configuration.
    /// </summary>
    public TelemetryExportersOptions Exporters { get; set; } = new();

    /// <summary>
    /// Gets or sets metrics provider specific options.
    /// </summary>
    public MetricsProviderOptions Metrics { get; set; } = new();
}
