using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.Core.Telemetry;

/// <summary>
/// Unified telemetry provider that supports all Relay components
/// </summary>
public class UnifiedTelemetryProvider : ITelemetryProvider
{
    private readonly UnifiedTelemetryOptions _options;
    private readonly ILogger<UnifiedTelemetryProvider>? _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    private readonly AsyncLocal<string?> _correlationIdContext = new();

    // Metrics
    private readonly Counter<long> _handlersExecutedCounter;
    private readonly Counter<long> _handlersSucceededCounter;
    private readonly Counter<long> _handlersFailedCounter;
    private readonly Counter<long> _notificationsPublishedCounter;
    private readonly Counter<long> _streamingOperationsCounter;
    private readonly Histogram<double> _handlerDurationHistogram;
    private readonly Histogram<double> _notificationDurationHistogram;
    private readonly Histogram<double> _streamingDurationHistogram;

    // Message broker metrics
    private readonly Counter<long> _messagesPublishedCounter;
    private readonly Counter<long> _messagesReceivedCounter;
    private readonly Counter<long> _messagesProcessedCounter;
    private readonly Counter<long> _messagesFailedCounter;
    private readonly Histogram<double> _messagePublishDurationHistogram;
    private readonly Histogram<double> _messageProcessDurationHistogram;
    private readonly Histogram<long> _messagePayloadSizeHistogram;

    private readonly IMetricsProvider? _metricsProvider;

    public UnifiedTelemetryProvider(
        IOptions<UnifiedTelemetryOptions> options,
        ILogger<UnifiedTelemetryProvider>? logger = null,
        IMetricsProvider? metricsProvider = null)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        _metricsProvider = metricsProvider;

        // Initialize activity source
        _activitySource = new ActivitySource(
            $"{UnifiedTelemetryConstants.ActivitySourceName}.{_options.Component}",
            UnifiedTelemetryConstants.ActivitySourceVersion);

        // Initialize meter
        _meter = new Meter(
            $"{UnifiedTelemetryConstants.MeterName}.{_options.Component}",
            UnifiedTelemetryConstants.MeterVersion);

        // Initialize core metrics
        _handlersExecutedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.HandlersExecuted,
            description: "Number of handlers executed");

        _handlersSucceededCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.HandlersSucceeded,
            description: "Number of handlers that succeeded");

        _handlersFailedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.HandlersFailed,
            description: "Number of handlers that failed");

        _notificationsPublishedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.NotificationsPublished,
            description: "Number of notifications published");

        _streamingOperationsCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.StreamingOperations,
            description: "Number of streaming operations");

        _handlerDurationHistogram = _meter.CreateHistogram<double>(
            UnifiedTelemetryConstants.Metrics.HandlerDuration,
            description: "Duration of handler execution in seconds");

        _notificationDurationHistogram = _meter.CreateHistogram<double>(
            UnifiedTelemetryConstants.Metrics.NotificationDuration,
            description: "Duration of notification publishing in seconds");

        _streamingDurationHistogram = _meter.CreateHistogram<double>(
            UnifiedTelemetryConstants.Metrics.StreamingDuration,
            description: "Duration of streaming operations in seconds");

        // Initialize message broker metrics
        _messagesPublishedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.MessagesPublished,
            description: "Number of messages published");

        _messagesReceivedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.MessagesReceived,
            description: "Number of messages received");

        _messagesProcessedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.MessagesProcessed,
            description: "Number of messages processed");

        _messagesFailedCounter = _meter.CreateCounter<long>(
            UnifiedTelemetryConstants.Metrics.MessagesFailed,
            description: "Number of messages failed to process");

        _messagePublishDurationHistogram = _meter.CreateHistogram<double>(
            UnifiedTelemetryConstants.Metrics.MessagePublishDuration,
            description: "Duration of message publish operations in seconds");

        _messageProcessDurationHistogram = _meter.CreateHistogram<double>(
            UnifiedTelemetryConstants.Metrics.MessageProcessDuration,
            description: "Duration of message processing operations in seconds");

        _messagePayloadSizeHistogram = _meter.CreateHistogram<long>(
            UnifiedTelemetryConstants.Metrics.MessagePayloadSize,
            description: "Size of message payloads in bytes");
    }

    public IMetricsProvider? MetricsProvider => _metricsProvider;

    public Activity? StartActivity(string operationName, Type requestType, string? correlationId = null)
    {
        if (!_options.EnableTracing)
            return null;

        var activity = _activitySource.StartActivity(operationName);

        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Component, _options.Component);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.OperationType, operationName);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.RequestType, requestType.FullName);

            if (correlationId != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.CorrelationId, correlationId);
                SetCorrelationId(correlationId);
            }

            _logger?.LogDebug("Started activity {ActivityId} for {RequestType} in component {Component}",
                activity.Id, requestType.Name, _options.Component);
        }

        return activity;
    }

    public void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        // Record metrics
        var tagList = new TagList
        {
            { UnifiedTelemetryConstants.Attributes.Component, _options.Component },
            { UnifiedTelemetryConstants.Attributes.RequestType, requestType.Name },
            { UnifiedTelemetryConstants.Attributes.HandlerName, handlerName },
            { UnifiedTelemetryConstants.Attributes.Success, success }
        };

        _handlersExecutedCounter.Add(1, tagList);
        
        if (success)
        {
            _handlersSucceededCounter.Add(1, tagList);
        }
        else
        {
            _handlersFailedCounter.Add(1, tagList);
        }

        _handlerDurationHistogram.Record(duration.TotalSeconds, tagList);

        // Update activity
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ResponseType, responseType?.FullName);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Duration, duration.TotalMilliseconds);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Success, success);

            if (exception != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionMessage, exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics with metrics provider
        _metricsProvider?.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = operationId,
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Handler execution completed: {RequestType} -> {ResponseType} in {Duration}ms (Success: {Success})",
            requestType.Name, responseType?.Name, duration.TotalMilliseconds, success);
    }

    public void RecordNotificationPublish(Type notificationType, int handlerCount, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        // Record metrics
        var tagList = new TagList
        {
            { UnifiedTelemetryConstants.Attributes.Component, _options.Component },
            { UnifiedTelemetryConstants.Attributes.NotificationType, notificationType.Name },
            { UnifiedTelemetryConstants.Attributes.HandlerCount, handlerCount },
            { UnifiedTelemetryConstants.Attributes.Success, success }
        };

        _notificationsPublishedCounter.Add(1, tagList);
        _notificationDurationHistogram.Record(duration.TotalSeconds, tagList);

        // Update activity
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Duration, duration.TotalMilliseconds);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Success, success);

            if (exception != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionMessage, exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics with metrics provider
        _metricsProvider?.RecordNotificationPublish(new NotificationPublishMetrics
        {
            OperationId = operationId,
            NotificationType = notificationType,
            HandlerCount = handlerCount,
            Duration = duration,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Notification published: {NotificationType} to {HandlerCount} handlers in {Duration}ms (Success: {Success})",
            notificationType.Name, handlerCount, duration.TotalMilliseconds, success);
    }

    public void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemCount, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;
        var operationId = activity?.Id ?? Guid.NewGuid().ToString();

        // Record metrics
        var tagList = new TagList
        {
            { UnifiedTelemetryConstants.Attributes.Component, _options.Component },
            { UnifiedTelemetryConstants.Attributes.RequestType, requestType.Name },
            { UnifiedTelemetryConstants.Attributes.ResponseType, responseType.Name },
            { UnifiedTelemetryConstants.Attributes.HandlerName, handlerName },
            { UnifiedTelemetryConstants.Attributes.ItemCount, itemCount },
            { UnifiedTelemetryConstants.Attributes.Success, success }
        };

        _streamingOperationsCounter.Add(1, tagList);
        _streamingDurationHistogram.Record(duration.TotalSeconds, tagList);

        // Update activity
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Duration, duration.TotalMilliseconds);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Success, success);

            if (exception != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionMessage, exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        // Record detailed metrics with metrics provider
        _metricsProvider?.RecordStreamingOperation(new StreamingOperationMetrics
        {
            OperationId = operationId,
            RequestType = requestType,
            ResponseType = responseType,
            HandlerName = handlerName,
            Duration = duration,
            ItemCount = itemCount,
            Success = success,
            Exception = exception
        });

        _logger?.LogDebug("Streaming operation completed: {RequestType} -> {ResponseType} ({ItemCount} items) in {Duration}ms (Success: {Success})",
            requestType.Name, responseType.Name, itemCount, duration.TotalMilliseconds, success);
    }

    /// <summary>
    /// Records message broker specific metrics
    /// </summary>
    public void RecordMessagePublished(Type messageType, long payloadSize, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;

        // Record metrics
        var tagList = new TagList
        {
            { UnifiedTelemetryConstants.Attributes.Component, _options.Component },
            { UnifiedTelemetryConstants.Attributes.MessageType, messageType.Name },
            { UnifiedTelemetryConstants.Attributes.Success, success }
        };

        _messagesPublishedCounter.Add(1, tagList);
        _messagePublishDurationHistogram.Record(duration.TotalSeconds, tagList);
        _messagePayloadSizeHistogram.Record(payloadSize, tagList);

        // Update activity
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize, payloadSize);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Duration, duration.TotalMilliseconds);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Success, success);

            if (exception != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionMessage, exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        _logger?.LogDebug("Message published: {MessageType} ({PayloadSize} bytes) in {Duration}ms (Success: {Success})",
            messageType.Name, payloadSize, duration.TotalMilliseconds, success);
    }

    /// <summary>
    /// Records message broker processing metrics
    /// </summary>
    public void RecordMessageProcessed(Type messageType, TimeSpan duration, bool success, Exception? exception = null)
    {
        var activity = Activity.Current;

        // Record metrics
        var tagList = new TagList
        {
            { UnifiedTelemetryConstants.Attributes.Component, _options.Component },
            { UnifiedTelemetryConstants.Attributes.MessageType, messageType.Name },
            { UnifiedTelemetryConstants.Attributes.Success, success }
        };

        _messagesReceivedCounter.Add(1, tagList);

        if (success)
        {
            _messagesProcessedCounter.Add(1, tagList);
        }
        else
        {
            _messagesFailedCounter.Add(1, tagList);
        }

        _messageProcessDurationHistogram.Record(duration.TotalSeconds, tagList);

        // Update activity
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Duration, duration.TotalMilliseconds);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.Success, success);

            if (exception != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ExceptionMessage, exception.Message);
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            }
            else if (success)
            {
                activity.SetStatus(ActivityStatusCode.Ok);
            }
        }

        _logger?.LogDebug("Message processed: {MessageType} in {Duration}ms (Success: {Success})",
            messageType.Name, duration.TotalMilliseconds, success);
    }

    public void SetCorrelationId(string correlationId)
    {
        _correlationIdContext.Value = correlationId;

        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.CorrelationId, correlationId);
        }
    }

    public string? GetCorrelationId()
    {
        return _correlationIdContext.Value ?? Activity.Current?.GetTagItem(UnifiedTelemetryConstants.Attributes.CorrelationId)?.ToString();
    }
}