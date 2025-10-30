using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scrutor;
using OpenTelemetry.Exporter;

namespace Relay.MessageBroker.DistributedTracing;

/// <summary>
/// Extension methods for configuring distributed tracing in the message broker.
/// </summary>
public static class DistributedTracingServiceCollectionExtensions
{
    /// <summary>
    /// Adds distributed tracing to the message broker.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerDistributedTracing(
        this IServiceCollection services,
        Action<DistributedTracingOptions>? configure = null)
    {
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<DistributedTracingOptions>(options => { });
        }

        // Decorate IMessageBroker with distributed tracing
        services.Decorate<IMessageBroker, DistributedTracingMessageBrokerDecorator>();

        return services;
    }

    /// <summary>
    /// Adds OpenTelemetry tracing with message broker instrumentation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMessageBrokerOpenTelemetry(
        this IServiceCollection services,
        Action<DistributedTracingOptions>? configure = null)
    {
        // Add distributed tracing decorator
        services.AddMessageBrokerDistributedTracing(configure);

        // Get or create options
        var options = new DistributedTracingOptions();
        configure?.Invoke(options);

        // Add OpenTelemetry tracing
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: MessageBrokerActivitySource.Version);
            })
            .WithTracing(tracing =>
            {
                // Add message broker activity source
                tracing.AddSource(MessageBrokerActivitySource.SourceName);

                // Configure sampling
                if (options.SamplingRate < 1.0)
                {
                    tracing.SetSampler(new TraceIdRatioBasedSampler(options.SamplingRate));
                }

                // Add exporters based on configuration
                ConfigureExporters(tracing, options);
            });

        return services;
    }

    private static void ConfigureExporters(TracerProviderBuilder tracing, DistributedTracingOptions options)
    {
        // OTLP Exporter
        if (options.OtlpExporter?.Enabled == true)
        {
            tracing.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(options.OtlpExporter.Endpoint);
                
                if (options.OtlpExporter.Protocol.Equals("http/protobuf", StringComparison.OrdinalIgnoreCase))
                {
                    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                }
                else
                {
                    otlpOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                }

                if (options.OtlpExporter.Headers != null)
                {
                    otlpOptions.Headers = string.Join(",", 
                        options.OtlpExporter.Headers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
                }
            });
        }

        // Jaeger Exporter
        if (options.JaegerExporter?.Enabled == true)
        {
            tracing.AddJaegerExporter(jaegerOptions =>
            {
                jaegerOptions.AgentHost = options.JaegerExporter.AgentHost;
                jaegerOptions.AgentPort = options.JaegerExporter.AgentPort;
                
                if (options.JaegerExporter.MaxPacketSize.HasValue)
                {
                    jaegerOptions.MaxPayloadSizeInBytes = options.JaegerExporter.MaxPacketSize.Value;
                }
            });
        }

        // Zipkin Exporter
        if (options.ZipkinExporter?.Enabled == true)
        {
            tracing.AddZipkinExporter(zipkinOptions =>
            {
                zipkinOptions.Endpoint = new Uri(options.ZipkinExporter.Endpoint);
                zipkinOptions.UseShortTraceIds = options.ZipkinExporter.UseShortTraceIds;
            });
        }

        // If no exporters are configured, traces will still be collected
        // but not exported (useful for testing or when using in-memory exporter)
    }
}
