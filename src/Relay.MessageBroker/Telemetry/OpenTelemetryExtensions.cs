using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Relay.MessageBroker.Telemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry with Relay Message Broker.
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds Relay Message Broker telemetry instrumentation to OpenTelemetry tracing.
    /// </summary>
    /// <param name="builder">The TracerProviderBuilder.</param>
    /// <param name="configure">Optional configuration action for telemetry options.</param>
    /// <returns>The TracerProviderBuilder for chaining.</returns>
    public static TracerProviderBuilder AddRelayMessageBrokerInstrumentation(
        this TracerProviderBuilder builder,
        Action<TelemetryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new TelemetryOptions();
        configure?.Invoke(options);

        return builder
            .AddSource(MessageBrokerTelemetry.ActivitySourceName)
            .ConfigureResource(resource =>
            {
                resource.AddService(
                    serviceName: options.ServiceName,
                    serviceVersion: options.ServiceVersion,
                    serviceNamespace: options.ServiceNamespace);

                foreach (var (key, value) in options.ResourceAttributes)
                {
                    resource.AddAttributes(new[] { new KeyValuePair<string, object>(key, value) });
                }
            });
    }

    /// <summary>
    /// Creates an activity for message publishing.
    /// </summary>
    /// <param name="destination">The message destination (topic, queue, etc.).</param>
    /// <param name="messagingSystem">The messaging system name (e.g., "rabbitmq", "kafka").</param>
    /// <returns>An Activity instance or null if tracing is disabled.</returns>
    public static Activity? StartPublishActivity(string destination, string messagingSystem)
    {
        var activity = MessageBrokerTelemetry.ActivitySource.StartActivity(
            $"{destination} publish",
            ActivityKind.Producer);

        if (activity != null)
        {
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingDestination, destination);
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingOperation, "publish");
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingDestinationKind, "topic");
        }

        return activity;
    }

    /// <summary>
    /// Creates an activity for message processing.
    /// </summary>
    /// <param name="destination">The message destination (topic, queue, etc.).</param>
    /// <param name="messagingSystem">The messaging system name (e.g., "rabbitmq", "kafka").</param>
    /// <returns>An Activity instance or null if tracing is disabled.</returns>
    public static Activity? StartProcessActivity(string destination, string messagingSystem)
    {
        var activity = MessageBrokerTelemetry.ActivitySource.StartActivity(
            $"{destination} process",
            ActivityKind.Consumer);

        if (activity != null)
        {
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingDestination, destination);
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingOperation, "process");
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingDestinationKind, "topic");
        }

        return activity;
    }

    /// <summary>
    /// Adds message attributes to the current activity.
    /// </summary>
    /// <param name="activity">The activity to add attributes to.</param>
    /// <param name="messageType">The message type name.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="payloadSize">The payload size in bytes.</param>
    public static void AddMessageAttributes(
        this Activity? activity,
        string messageType,
        string? messageId = null,
        int? payloadSize = null)
    {
        if (activity == null) return;

        activity.SetTag(MessageBrokerTelemetry.Attributes.MessageType, messageType);
        
        if (messageId != null)
        {
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingMessageId, messageId);
        }

        if (payloadSize.HasValue)
        {
            activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingPayloadSize, payloadSize.Value);
        }
    }

    /// <summary>
    /// Adds compression attributes to the current activity.
    /// </summary>
    /// <param name="activity">The activity to add attributes to.</param>
    /// <param name="algorithm">The compression algorithm used.</param>
    /// <param name="originalSize">The original payload size in bytes.</param>
    /// <param name="compressedSize">The compressed payload size in bytes.</param>
    public static void AddCompressionAttributes(
        this Activity? activity,
        string algorithm,
        int originalSize,
        int compressedSize)
    {
        if (activity == null) return;

        activity.SetTag(MessageBrokerTelemetry.Attributes.MessageCompressed, true);
        activity.SetTag(MessageBrokerTelemetry.Attributes.MessageCompressionAlgorithm, algorithm);
        activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingPayloadSize, originalSize);
        activity.SetTag(MessageBrokerTelemetry.Attributes.MessagingPayloadCompressedSize, compressedSize);

        var ratio = originalSize > 0 ? (double)compressedSize / originalSize : 1.0;
        activity.SetTag(MessageBrokerTelemetry.Attributes.MessageCompressionRatio, ratio);
    }

    /// <summary>
    /// Adds circuit breaker attributes to the current activity.
    /// </summary>
    /// <param name="activity">The activity to add attributes to.</param>
    /// <param name="circuitBreakerName">The circuit breaker name.</param>
    /// <param name="state">The circuit breaker state.</param>
    public static void AddCircuitBreakerAttributes(
        this Activity? activity,
        string circuitBreakerName,
        string state)
    {
        if (activity == null) return;

        activity.SetTag(MessageBrokerTelemetry.Attributes.CircuitBreakerName, circuitBreakerName);
        activity.SetTag(MessageBrokerTelemetry.Attributes.CircuitBreakerState, state);
    }

    /// <summary>
    /// Records an error on the current activity.
    /// </summary>
    /// <param name="activity">The activity to record the error on.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="captureStackTrace">Whether to capture the stack trace.</param>
    public static void RecordError(
        this Activity? activity,
        Exception exception,
        bool captureStackTrace = true)
    {
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.SetTag(MessageBrokerTelemetry.Attributes.ErrorType, exception.GetType().FullName);
        activity.SetTag(MessageBrokerTelemetry.Attributes.ErrorMessage, exception.Message);

        if (captureStackTrace && exception.StackTrace != null)
        {
            activity.SetTag(MessageBrokerTelemetry.Attributes.ErrorStackTrace, exception.StackTrace);
        }

        activity.AddEvent(new ActivityEvent(
            MessageBrokerTelemetry.Events.MessageFailed,
            tags: new ActivityTagsCollection
            {
                { MessageBrokerTelemetry.Attributes.ErrorType, exception.GetType().FullName },
                { MessageBrokerTelemetry.Attributes.ErrorMessage, exception.Message }
            }));
    }

    /// <summary>
    /// Adds an event to the current activity.
    /// </summary>
    /// <param name="activity">The activity to add the event to.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="attributes">Optional event attributes.</param>
    public static void AddMessageEvent(
        this Activity? activity,
        string eventName,
        Dictionary<string, object?>? attributes = null)
    {
        if (activity == null) return;

        var tags = new ActivityTagsCollection();
        if (attributes != null)
        {
            foreach (var (key, value) in attributes)
            {
                if (value != null)
                {
                    tags.Add(key, value);
                }
            }
        }

        activity.AddEvent(new ActivityEvent(eventName, tags: tags));
    }
}
