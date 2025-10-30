namespace Relay.MessageBroker.DistributedTracing;

/// <summary>
/// Configuration options for distributed tracing.
/// </summary>
public sealed class DistributedTracingOptions
{
    /// <summary>
    /// Gets or sets whether distributed tracing is enabled.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name for tracing.
    /// </summary>
    public string ServiceName { get; set; } = "Relay.MessageBroker";

    /// <summary>
    /// Gets or sets the sampling rate (0.0 to 1.0).
    /// 0.0 = no sampling, 1.0 = sample all traces.
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the OTLP exporter configuration.
    /// </summary>
    public OtlpExporterOptions? OtlpExporter { get; set; }

    /// <summary>
    /// Gets or sets the Jaeger exporter configuration.
    /// </summary>
    public JaegerExporterOptions? JaegerExporter { get; set; }

    /// <summary>
    /// Gets or sets the Zipkin exporter configuration.
    /// </summary>
    public ZipkinExporterOptions? ZipkinExporter { get; set; }
}

/// <summary>
/// Configuration options for OTLP exporter.
/// </summary>
public sealed class OtlpExporterOptions
{
    /// <summary>
    /// Gets or sets whether the OTLP exporter is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the OTLP endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:4317";

    /// <summary>
    /// Gets or sets the protocol (grpc or http/protobuf).
    /// </summary>
    public string Protocol { get; set; } = "grpc";

    /// <summary>
    /// Gets or sets custom headers for the exporter.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// Configuration options for Jaeger exporter.
/// </summary>
public sealed class JaegerExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Jaeger exporter is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Jaeger agent host.
    /// </summary>
    public string AgentHost { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the Jaeger agent port.
    /// </summary>
    public int AgentPort { get; set; } = 6831;

    /// <summary>
    /// Gets or sets the maximum packet size.
    /// </summary>
    public int? MaxPacketSize { get; set; }
}

/// <summary>
/// Configuration options for Zipkin exporter.
/// </summary>
public sealed class ZipkinExporterOptions
{
    /// <summary>
    /// Gets or sets whether the Zipkin exporter is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the Zipkin endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:9411/api/v2/spans";

    /// <summary>
    /// Gets or sets whether to use short trace IDs.
    /// </summary>
    public bool UseShortTraceIds { get; set; }
}
