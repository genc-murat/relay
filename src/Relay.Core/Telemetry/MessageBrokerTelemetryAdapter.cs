using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.Telemetry;

/// <summary>
/// Adapter that wraps UnifiedTelemetryProvider for MessageBroker compatibility.
/// </summary>
public sealed class MessageBrokerTelemetryAdapter
{
    private readonly RelayTelemetryProvider _provider;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBrokerTelemetryAdapter"/> class.
    /// </summary>
    /// <param name="options">The telemetry options.</param>
    /// <param name="logger">The logger.</param>
    public MessageBrokerTelemetryAdapter(IOptions<RelayTelemetryOptions> options, ILogger? logger = null)
    {
        _provider = new RelayTelemetryProvider(options);
        _logger = logger;
    }

    /// <summary>
    /// Records a message published event.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="messageSize">The message size in bytes.</param>
    /// <param name="compressed">Whether the message was compressed.</param>
    public void RecordMessagePublished(string messageType, long messageSize, bool compressed)
    {
        using var activity = _provider.StartActivity("MessageBroker.MessagePublished", typeof(object));
        activity?.SetTag("message.type", messageType);
        activity?.SetTag("message.size_bytes", messageSize);
        activity?.SetTag("message.compressed", compressed);
        
        // Use the provider's internal metrics through reflection or create a simple implementation
        _logger?.LogDebug("Message published: {MessageType}, Size: {MessageSize} bytes, Compressed: {Compressed}", 
            messageType, messageSize, compressed);
    }

    /// <summary>
    /// Records a message received event.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="messageSize">The message size in bytes.</param>
    /// <param name="compressed">Whether the message was compressed.</param>
    public void RecordMessageReceived(string messageType, long messageSize, bool compressed)
    {
        using var activity = _provider.StartActivity("MessageBroker.MessageReceived", typeof(object));
        activity?.SetTag("message.type", messageType);
        activity?.SetTag("message.size_bytes", messageSize);
        activity?.SetTag("message.compressed", compressed);
        
        _logger?.LogDebug("Message received: {MessageType}, Size: {MessageSize} bytes, Compressed: {Compressed}", 
            messageType, messageSize, compressed);
    }

    /// <summary>
    /// Records a message processing duration.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <param name="duration">The processing duration.</param>
    public void RecordProcessingDuration(string messageType, TimeSpan duration)
    {
        _logger?.LogDebug("Message processing duration: {MessageType}, Duration: {Duration}ms", 
            messageType, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Records an error event.
    /// </summary>
    /// <param name="errorType">The error type.</param>
    /// <param name="message">The error message.</param>
    public void RecordError(string errorType, string message)
    {
        using var activity = _provider.StartActivity("MessageBroker.Error", typeof(object));
        activity?.SetTag("error.type", errorType);
        activity?.SetTag("error.message", message);
        activity?.SetStatus(ActivityStatusCode.Error, message);
        
        _logger?.LogError("MessageBroker error: {ErrorType} - {Message}", errorType, message);
    }
}