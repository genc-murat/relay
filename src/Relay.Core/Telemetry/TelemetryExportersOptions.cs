using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Configuration for telemetry exporters.
/// </summary>
public sealed class TelemetryExportersOptions
{
    /// <summary>
    /// Gets or sets whether to enable console exporter (for development).
    /// </summary>
    public bool EnableConsole { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable OTLP exporter.
    /// </summary>
    public bool EnableOtlp { get; set; } = true;

    /// <summary>
    /// Gets or sets the OTLP endpoint.
    /// </summary>
    public string? OtlpEndpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the OTLP protocol (grpc or http/protobuf).
    /// </summary>
    public string OtlpProtocol { get; set; } = "grpc";

    /// <summary>
    /// Gets or sets OTLP headers.
    /// </summary>
    public Dictionary<string, string> OtlpHeaders { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable Jaeger exporter.
    /// </summary>
    public bool EnableJaeger { get; set; } = false;

    /// <summary>
    /// Gets or sets the Jaeger agent host.
    /// </summary>
    public string? JaegerAgentHost { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the Jaeger agent port.
    /// </summary>
    public int JaegerAgentPort { get; set; } = 6831;

    /// <summary>
    /// Gets or sets whether to enable Zipkin exporter.
    /// </summary>
    public bool EnableZipkin { get; set; } = false;

    /// <summary>
    /// Gets or sets the Zipkin endpoint.
    /// </summary>
    public string? ZipkinEndpoint { get; set; } = "http://localhost:9411/api/v2/spans";

    /// <summary>
    /// Gets or sets whether to enable Prometheus exporter.
    /// </summary>
    public bool EnablePrometheus { get; set; } = false;

    /// <summary>
    /// Gets or sets the Prometheus scrape endpoint path.
    /// </summary>
    public string PrometheusEndpoint { get; set; } = "/metrics";

    /// <summary>
    /// Gets or sets whether to enable Azure Monitor exporter.
    /// </summary>
    public bool EnableAzureMonitor { get; set; } = false;

    /// <summary>
    /// Gets or sets the Azure Monitor connection string.
    /// </summary>
    public string? AzureMonitorConnectionString { get; set; }

    /// <summary>
    /// Gets or sets whether to enable AWS X-Ray exporter.
    /// </summary>
    public bool EnableAwsXRay { get; set; } = false;

    /// <summary>
    /// Gets or sets the AWS X-Ray daemon endpoint.
    /// </summary>
    public string? AwsXRayEndpoint { get; set; } = "127.0.0.1:2000";
}