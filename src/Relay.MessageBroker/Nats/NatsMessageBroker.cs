using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Relay.MessageBroker.Nats;

/// <summary>
/// NATS implementation of message broker.
/// </summary>
public sealed class NatsMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly MessageBrokerOptions _options;
    private readonly ILogger<NatsMessageBroker>? _logger;
    private readonly Dictionary<Type, List<Func<object, MessageContext, CancellationToken, ValueTask>>> _handlers = new();
    private NatsConnection? _connection;
    private readonly List<IAsyncDisposable> _subscriptions = new();
    private bool _isStarted;

    public NatsMessageBroker(
        MessageBrokerOptions options,
        ILogger<NatsMessageBroker>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;
        
        if (_options.Nats == null)
            throw new InvalidOperationException("NATS options are required.");
        
        if (_options.Nats.Servers == null || _options.Nats.Servers.Length == 0)
            throw new InvalidOperationException("At least one NATS server URL is required.");
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (message == null) throw new ArgumentNullException(nameof(message));

        try
        {
            _connection ??= await CreateConnectionAsync(cancellationToken);

            var subject = options?.RoutingKey ?? GetSubjectName<TMessage>();
            var messageBody = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(messageBody);

            var headers = new NatsHeaders();
            headers.Add("MessageType", typeof(TMessage).FullName ?? typeof(TMessage).Name);
            
            if (options?.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    headers.Add(header.Key, header.Value?.ToString() ?? string.Empty);
                }
            }

            await _connection.PublishAsync(subject, data, headers: headers, cancellationToken: cancellationToken);
            _logger?.LogDebug("Published message {MessageType} to NATS subject {Subject}", 
                typeof(TMessage).Name, subject);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error publishing message {MessageType}", typeof(TMessage).Name);
            throw;
        }
    }

    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var messageType = typeof(TMessage);
        
        if (!_handlers.ContainsKey(messageType))
        {
            _handlers[messageType] = new List<Func<object, MessageContext, CancellationToken, ValueTask>>();
        }

        _handlers[messageType].Add(async (msg, ctx, ct) => await handler((TMessage)msg, ctx, ct));
        
        _logger?.LogDebug("Subscribed to message type {MessageType}", typeof(TMessage).Name);

        return ValueTask.CompletedTask;
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isStarted) return;

        try
        {
            _connection ??= await CreateConnectionAsync(cancellationToken);
            await StartCoreSubscriptionsAsync(cancellationToken);

            _isStarted = true;
            _logger?.LogInformation("NATS message broker started");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting NATS message broker");
            throw;
        }
    }

    public async ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isStarted) return;

        try
        {
            foreach (var subscription in _subscriptions)
            {
                await subscription.DisposeAsync();
            }
            _subscriptions.Clear();

            _isStarted = false;
            _logger?.LogInformation("NATS message broker stopped");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping NATS message broker");
            throw;
        }
    }

    private async Task<NatsConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var authOpts = (!string.IsNullOrWhiteSpace(_options.Nats!.Username) && 
            !string.IsNullOrWhiteSpace(_options.Nats.Password))
            ? new NatsAuthOpts
            {
                Username = _options.Nats.Username,
                Password = _options.Nats.Password
            }
            : NatsAuthOpts.Default;

        var opts = new NatsOpts
        {
            Url = _options.Nats!.Servers[0],
            Name = _options.Nats.ClientName ?? _options.Nats.Name ?? "relay-nats-client",
            MaxReconnectRetry = _options.Nats.MaxReconnects,
            ReconnectWaitMin = TimeSpan.FromSeconds(1),
            ReconnectWaitMax = TimeSpan.FromSeconds(10),
            AuthOpts = authOpts
        };

        var connection = new NatsConnection(opts);
        await connection.ConnectAsync();
        
        _logger?.LogInformation("Connected to NATS server");
        
        return connection;
    }

    private async Task StartCoreSubscriptionsAsync(CancellationToken cancellationToken)
    {
        foreach (var handlerType in _handlers.Keys)
        {
            var subject = GetSubjectName(handlerType);

            var subscription = await _connection!.SubscribeCoreAsync<byte[]>(subject, cancellationToken: cancellationToken);
            
            var consumeTask = Task.Run(async () =>
            {
                await foreach (var msg in subscription.Msgs.ReadAllAsync(cancellationToken))
                {
                    await ProcessNatsMessageAsync(msg.Subject, msg.Data, msg.Headers, cancellationToken);
                }
            }, cancellationToken);

            _logger?.LogInformation("Started Core subscription for subject {Subject}", subject);
        }
    }

    private async Task ProcessNatsMessageAsync(
        string subject, 
        byte[]? data, 
        NatsHeaders? headers,
        CancellationToken cancellationToken)
    {
        try
        {
            if (data == null || data.Length == 0)
            {
                _logger?.LogWarning("Received empty message from subject {Subject}", subject);
                return;
            }

            var messageTypeHeader = headers?.TryGetValue("MessageType", out var typeValues) == true 
                ? typeValues.FirstOrDefault() 
                : null;

            var type = !string.IsNullOrWhiteSpace(messageTypeHeader) 
                ? Type.GetType(messageTypeHeader) 
                : null;

            if (type == null || !_handlers.ContainsKey(type))
            {
                _logger?.LogWarning("No handler found for message type {MessageType} from subject {Subject}", 
                    messageTypeHeader, subject);
                return;
            }

            var messageBody = Encoding.UTF8.GetString(data);
            var message = JsonSerializer.Deserialize(messageBody, type);

            if (message == null)
            {
                _logger?.LogWarning("Failed to deserialize message of type {MessageType}", messageTypeHeader);
                return;
            }

            var context = new MessageContext
            {
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow,
                RoutingKey = subject,
                Headers = headers?.ToDictionary(kvp => kvp.Key, kvp => (object)string.Join(",", kvp.Value))
            };

            var handlers = _handlers[type];
            foreach (var handler in handlers)
            {
                await handler(message, context, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error processing NATS message from subject {Subject}", subject);
        }
    }

    private string GetSubjectName<TMessage>()
    {
        return GetSubjectName(typeof(TMessage));
    }

    private string GetSubjectName(Type type)
    {
        var streamPrefix = !string.IsNullOrWhiteSpace(_options.Nats!.StreamName) 
            ? $"{_options.Nats.StreamName}." 
            : "relay.";
        
        return $"{streamPrefix}{type.Name}";
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
