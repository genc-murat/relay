
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using Relay.MessageBroker.Compression;

namespace Relay.MessageBroker.Nats;

/// <summary>
/// NATS implementation of message broker.
/// </summary>
public sealed class NatsMessageBroker : BaseMessageBroker
{
    private NatsConnection? _connection;
    private readonly List<CancellationTokenSource> _natsSubscriptions = new();

    public NatsMessageBroker(
        IOptions<MessageBrokerOptions> options,
        ILogger<NatsMessageBroker> logger,
        IMessageCompressor? compressor = null)
        : base(options, logger, compressor)
    {
        if (_options.Nats == null)
            throw new InvalidOperationException("NATS options are required.");
        
        if (_options.Nats.Servers == null || _options.Nats.Servers.Length == 0)
            throw new InvalidOperationException("At least one NATS server URL is required.");
    }

    protected override async ValueTask PublishInternalAsync<TMessage>(
        TMessage message,
        byte[] serializedMessage,
        PublishOptions? options,
        CancellationToken cancellationToken)
    {
        _connection ??= await CreateConnectionAsync(cancellationToken);

        var subject = options?.RoutingKey ?? GetSubjectName<TMessage>();

        var headers = new NatsHeaders();
        headers.Add("MessageType", typeof(TMessage).FullName ?? typeof(TMessage).Name);
        
        if (options?.Headers != null)
        {
            foreach (var header in options.Headers)
            {
                headers.Add(header.Key, header.Value?.ToString() ?? string.Empty);
            }
        }

        await _connection.PublishAsync(subject, serializedMessage, headers: headers, cancellationToken: cancellationToken);
        _logger.LogDebug("Published message {MessageType} to NATS subject {Subject}", 
            typeof(TMessage).Name, subject);
    }

    protected override async ValueTask SubscribeInternalAsync(
        Type messageType,
        SubscriptionInfo subscriptionInfo,
        CancellationToken cancellationToken)
    {
        _connection ??= await CreateConnectionAsync(cancellationToken);
        
        var subject = GetSubjectName(messageType);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        _ = Task.Run(async () =>
        {
            await foreach (var msg in _connection.SubscribeAsync<byte[]>(subject, cancellationToken: cts.Token))
            {
                try
                {
                    var message = JsonSerializer.Deserialize(msg.Data, messageType);
                    if (message != null)
                    {
                        var context = new MessageContext
                        {
                            MessageId = Guid.NewGuid().ToString(),
                            Timestamp = DateTimeOffset.UtcNow,
                            RoutingKey = subject,
                            Headers = msg.Headers?.ToDictionary(kvp => kvp.Key, kvp => (object)string.Join(",", kvp.Value))
                        };
                        
                        await ProcessMessageAsync(message, messageType, context, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing NATS message on subject {Subject}", subject);
                }
            }
        }, cancellationToken);
        
        _natsSubscriptions.Add(cts);
    }

    protected override async ValueTask StartInternalAsync(CancellationToken cancellationToken = default)
    {
        _connection ??= await CreateConnectionAsync(cancellationToken);
        _logger.LogInformation("NATS message broker started");
    }

    protected override async ValueTask StopInternalAsync(CancellationToken cancellationToken = default)
    {
        foreach (var cts in _natsSubscriptions)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _natsSubscriptions.Clear();
        
        _logger.LogInformation("NATS message broker stopped");
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

    protected override async ValueTask DisposeInternalAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}