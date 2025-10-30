using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.DistributedTracing;

/// <summary>
/// Decorator that adds distributed tracing to message broker operations.
/// </summary>
public sealed class DistributedTracingMessageBrokerDecorator : IMessageBroker
{
    private readonly IMessageBroker _inner;
    private readonly DistributedTracingOptions _options;
    private readonly ILogger<DistributedTracingMessageBrokerDecorator> _logger;
    private readonly ActivitySource _activitySource;

    public DistributedTracingMessageBrokerDecorator(
        IMessageBroker inner,
        IOptions<DistributedTracingOptions> options,
        ILogger<DistributedTracingMessageBrokerDecorator> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = MessageBrokerActivitySource.Instance;
    }

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableTracing)
        {
            await _inner.PublishAsync(message, options, cancellationToken);
            return;
        }

        using var activity = _activitySource.StartActivity(
            $"{typeof(TMessage).Name} {MessageBrokerActivitySource.Operations.Publish}",
            ActivityKind.Producer);

        if (activity != null)
        {
            // Set span attributes
            activity.SetTag(MessageBrokerActivitySource.AttributeNames.MessageType, typeof(TMessage).Name);
            activity.SetTag(MessageBrokerActivitySource.AttributeNames.Operation, MessageBrokerActivitySource.Operations.Publish);
            activity.SetTag(MessageBrokerActivitySource.AttributeNames.System, GetBrokerType());

            if (options?.RoutingKey != null)
            {
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.RoutingKey, options.RoutingKey);
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.Destination, options.RoutingKey);
            }

            if (options?.Exchange != null)
            {
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.Exchange, options.Exchange);
            }

            // Inject trace context into message headers
            options ??= new PublishOptions();
            options.Headers ??= new Dictionary<string, object>();
            W3CTraceContextPropagator.Inject(options.Headers, activity);

            try
            {
                var startTime = DateTimeOffset.UtcNow;
                await _inner.PublishAsync(message, options, cancellationToken);
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.ProcessingDuration, duration);
                activity.SetStatus(ActivityStatusCode.Ok);
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("exception.type", ex.GetType().FullName);
                activity.SetTag("exception.message", ex.Message);
                activity.SetTag("exception.stacktrace", ex.StackTrace);
                throw;
            }
        }
        else
        {
            await _inner.PublishAsync(message, options, cancellationToken);
        }
    }

    public async ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableTracing)
        {
            await _inner.SubscribeAsync(handler, options, cancellationToken);
            return;
        }

        // Wrap the handler to add tracing
        async ValueTask TracedHandler(TMessage message, MessageContext context, CancellationToken ct)
        {
            // Extract trace context from message headers
            var parentContext = W3CTraceContextPropagator.Extract(context.Headers);
            var traceState = W3CTraceContextPropagator.ExtractTraceState(context.Headers);

            ActivityContext activityContext = default;
            if (parentContext.HasValue)
            {
                activityContext = new ActivityContext(
                    parentContext.Value.TraceId,
                    parentContext.Value.SpanId,
                    parentContext.Value.TraceFlags,
                    traceState);
            }

            using var activity = _activitySource.StartActivity(
                $"{typeof(TMessage).Name} {MessageBrokerActivitySource.Operations.Consume}",
                ActivityKind.Consumer,
                activityContext);

            if (activity != null)
            {
                // Set span attributes
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.MessageType, typeof(TMessage).Name);
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.Operation, MessageBrokerActivitySource.Operations.Consume);
                activity.SetTag(MessageBrokerActivitySource.AttributeNames.System, GetBrokerType());

                if (context.CorrelationId != null)
                {
                    activity.SetTag(MessageBrokerActivitySource.AttributeNames.CorrelationId, context.CorrelationId);
                }

                if (context.RoutingKey != null)
                {
                    activity.SetTag(MessageBrokerActivitySource.AttributeNames.RoutingKey, context.RoutingKey);
                    activity.SetTag(MessageBrokerActivitySource.AttributeNames.Destination, context.RoutingKey);
                }

                if (context.Exchange != null)
                {
                    activity.SetTag(MessageBrokerActivitySource.AttributeNames.Exchange, context.Exchange);
                }

                try
                {
                    var startTime = DateTimeOffset.UtcNow;
                    await handler(message, context, ct);
                    var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    
                    activity.SetTag(MessageBrokerActivitySource.AttributeNames.ProcessingDuration, duration);
                    activity.SetStatus(ActivityStatusCode.Ok);
                }
                catch (Exception ex)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.SetTag("exception.type", ex.GetType().FullName);
                    activity.SetTag("exception.message", ex.Message);
                    activity.SetTag("exception.stacktrace", ex.StackTrace);
                    throw;
                }
            }
            else
            {
                await handler(message, context, ct);
            }
        }

        await _inner.SubscribeAsync<TMessage>(TracedHandler, options, cancellationToken);
    }

    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _inner.StartAsync(cancellationToken);
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _inner.StopAsync(cancellationToken);
    }

    private string GetBrokerType()
    {
        var innerType = _inner.GetType().Name;
        return innerType.Replace("MessageBroker", "").ToLowerInvariant();
    }
}
