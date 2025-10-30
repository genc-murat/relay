using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Decorator that adds authentication and authorization to an IMessageBroker implementation.
/// </summary>
public sealed class SecurityMessageBrokerDecorator : IMessageBroker, IAsyncDisposable
{
    private readonly IMessageBroker _innerBroker;
    private readonly IMessageAuthenticator _authenticator;
    private readonly AuthenticationOptions _authOptions;
    private readonly ILogger<SecurityMessageBrokerDecorator> _logger;
    private readonly SecurityEventLogger _securityEventLogger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityMessageBrokerDecorator"/> class.
    /// </summary>
    /// <param name="innerBroker">The inner message broker to decorate.</param>
    /// <param name="authenticator">The message authenticator.</param>
    /// <param name="authOptions">The authentication options.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="securityEventLogger">The security event logger.</param>
    public SecurityMessageBrokerDecorator(
        IMessageBroker innerBroker,
        IMessageAuthenticator authenticator,
        IOptions<AuthenticationOptions> authOptions,
        ILogger<SecurityMessageBrokerDecorator> logger,
        SecurityEventLogger securityEventLogger)
    {
        _innerBroker = innerBroker ?? throw new ArgumentNullException(nameof(innerBroker));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
        _authOptions = authOptions?.Value ?? throw new ArgumentNullException(nameof(authOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityEventLogger = securityEventLogger ?? throw new ArgumentNullException(nameof(securityEventLogger));

        _authOptions.Validate();

        _logger.LogInformation(
            "SecurityMessageBrokerDecorator initialized. Authentication enabled: {Enabled}",
            _authOptions.EnableAuthentication);
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If authentication is disabled, publish directly
        if (!_authOptions.EnableAuthentication)
        {
            await _innerBroker.PublishAsync(message, options, cancellationToken);
            return;
        }

        try
        {
            // Extract token from headers
            var token = ExtractTokenFromHeaders(options?.Headers);

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Publish operation rejected: No authentication token provided");
                _securityEventLogger.LogUnauthorizedAccess(
                    "publish",
                    "No authentication token provided",
                    Array.Empty<string>(),
                    typeof(TMessage).Name);
                throw new AuthenticationException("Authentication token is required for publish operations");
            }

            // Validate token
            var isValid = await _authenticator.ValidateTokenAsync(token, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("Publish operation rejected: Invalid authentication token");
                _securityEventLogger.LogUnauthorizedAccess(
                    "publish",
                    "Invalid authentication token",
                    Array.Empty<string>(),
                    typeof(TMessage).Name);
                throw new AuthenticationException("Invalid authentication token");
            }

            // Authorize operation
            var isAuthorized = await _authenticator.AuthorizeAsync(token, "publish", cancellationToken);
            if (!isAuthorized)
            {
                _logger.LogWarning(
                    "Publish operation rejected: Insufficient permissions for message type {MessageType}",
                    typeof(TMessage).Name);
                throw new AuthenticationException(
                    $"Insufficient permissions to publish messages of type {typeof(TMessage).Name}");
            }

            _logger.LogDebug(
                "Publish operation authorized for message type {MessageType}",
                typeof(TMessage).Name);

            // Publish the message
            await _innerBroker.PublishAsync(message, options, cancellationToken);
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error during secure publish of message type {MessageType}",
                typeof(TMessage).Name);
            throw new AuthenticationException(
                $"Failed to securely publish message of type {typeof(TMessage).Name}",
                ex);
        }
    }

    /// <inheritdoc/>
    public ValueTask SubscribeAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler,
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If authentication is disabled, subscribe directly
        if (!_authOptions.EnableAuthentication)
        {
            return _innerBroker.SubscribeAsync(handler, options, cancellationToken);
        }

        // Wrap the handler to validate authentication and authorization
        async ValueTask SecureHandler(TMessage message, MessageContext context, CancellationToken ct)
        {
            try
            {
                // Extract token from message context headers
                var token = ExtractTokenFromHeaders(context.Headers);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Subscribe handler rejected: No authentication token provided");
                    _securityEventLogger.LogUnauthorizedAccess(
                        "subscribe",
                        "No authentication token provided",
                        Array.Empty<string>(),
                        typeof(TMessage).Name);
                    throw new AuthenticationException("Authentication token is required for message consumption");
                }

                // Validate token
                var isValid = await _authenticator.ValidateTokenAsync(token, ct);
                if (!isValid)
                {
                    _logger.LogWarning("Subscribe handler rejected: Invalid authentication token");
                    _securityEventLogger.LogUnauthorizedAccess(
                        "subscribe",
                        "Invalid authentication token",
                        Array.Empty<string>(),
                        typeof(TMessage).Name);
                    throw new AuthenticationException("Invalid authentication token");
                }

                // Authorize operation
                var isAuthorized = await _authenticator.AuthorizeAsync(token, "subscribe", ct);
                if (!isAuthorized)
                {
                    _logger.LogWarning(
                        "Subscribe handler rejected: Insufficient permissions for message type {MessageType}",
                        typeof(TMessage).Name);
                    throw new AuthenticationException(
                        $"Insufficient permissions to consume messages of type {typeof(TMessage).Name}");
                }

                _logger.LogDebug(
                    "Subscribe handler authorized for message type {MessageType}",
                    typeof(TMessage).Name);

                // Call the original handler
                await handler(message, context, ct);
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(
                    ex,
                    "Authentication/authorization failed for message type {MessageType}",
                    typeof(TMessage).Name);

                // Reject the message (don't requeue)
                if (context.Reject != null)
                {
                    await context.Reject(false);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during secure message consumption of type {MessageType}",
                    typeof(TMessage).Name);
                throw;
            }
        }

        // Subscribe with the secure handler
        return _innerBroker.SubscribeAsync<TMessage>(SecureHandler, options, cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StartAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken cancellationToken = default)
    {
        return _innerBroker.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Extracts the authentication token from message headers.
    /// </summary>
    /// <param name="headers">The message headers.</param>
    /// <returns>The authentication token, or null if not found.</returns>
    private string? ExtractTokenFromHeaders(Dictionary<string, object>? headers)
    {
        if (headers == null)
        {
            return null;
        }

        // Try to get token from Authorization header
        if (headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader?.ToString();
            if (!string.IsNullOrWhiteSpace(authValue))
            {
                // Remove "Bearer " prefix if present
                if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return authValue.Substring(7);
                }
                return authValue;
            }
        }

        // Try to get token from X-Auth-Token header
        if (headers.TryGetValue("X-Auth-Token", out var tokenHeader))
        {
            return tokenHeader?.ToString();
        }

        return null;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _logger.LogInformation("Disposing SecurityMessageBrokerDecorator");

        // Dispose inner broker if it's disposable
        if (_innerBroker is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _logger.LogInformation("SecurityMessageBrokerDecorator disposed");
    }
}
