namespace Relay.Core.Telemetry;

/// <summary>
/// Trace context propagation formats.
/// </summary>
public enum TraceContextFormat
{
    /// <summary>
    /// W3C Trace Context (recommended).
    /// </summary>
    W3C,

    /// <summary>
    /// B3 propagation format (Zipkin).
    /// </summary>
    B3,

    /// <summary>
    /// Jaeger propagation format.
    /// </summary>
    Jaeger,

    /// <summary>
    /// AWS X-Ray propagation format.
    /// </summary>
    AWSXRay
}
