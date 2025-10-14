using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Telemetry;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Authorization;

/// <summary>
/// A pipeline behavior that implements authorization for requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class AuthorizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IAuthorizationContext _authorizationContext;
    private readonly ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IOptions<RelayOptions> _options;
    private readonly ITelemetryProvider? _telemetryProvider;
    private readonly string _handlerKey;

    // Cache for authorization attributes to avoid reflection on every request
    private static readonly ConcurrentDictionary<Type, AuthorizeAttribute[]> _attributeCache = new();

    public AuthorizationPipelineBehavior(
        IAuthorizationService authorizationService,
        IAuthorizationContext authorizationContext,
        ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> logger,
        IOptions<RelayOptions> options,
        ITelemetryProvider? telemetryProvider = null)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _authorizationContext = authorizationContext ?? throw new ArgumentNullException(nameof(authorizationContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _telemetryProvider = telemetryProvider;
        _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        // Get authorization configuration
        var authorizationOptions = GetAuthorizationOptions();
        var authorizeAttributes = GetCachedAuthorizeAttributes();

        // Check if authorization is enabled for this request
        if (!IsAuthorizationEnabled(authorizationOptions, authorizeAttributes))
        {
            return await next();
        }

        var stopwatch = Stopwatch.StartNew();
        var correlationId = _telemetryProvider?.GetCorrelationId();
        using var activity = _telemetryProvider?.StartActivity("Authorization", typeof(TRequest), correlationId);

        var isAuthorized = false;
        Exception? authException = null;

        try
        {
            // Add request-specific information to the authorization context
            AddRequestInfoToContext(request);

            // Check authorization
            isAuthorized = await _authorizationService.AuthorizeAsync(_authorizationContext, cancellationToken);

            if (!isAuthorized)
            {
                var userName = GetUserNameFromContext();

                _logger.LogWarning(
                    "Authorization failed for request: {RequestType}, CorrelationId: {CorrelationId}, User: {User}",
                    typeof(TRequest).Name,
                    correlationId,
                    userName);

                activity?.SetTag("authorization.result", "denied");
                activity?.SetTag("authorization.user", userName);

                if (authorizationOptions.ThrowOnAuthorizationFailure)
                {
                    throw new AuthorizationException($"Authorization failed for request: {typeof(TRequest).Name}");
                }

                // Return default response if authorization fails and not throwing
                return GetDefaultResponse();
            }

            var successUserName = GetUserNameFromContext();

            activity?.SetTag("authorization.result", "granted");
            activity?.SetTag("authorization.user", successUserName);

            _logger.LogDebug(
                "Authorization successful for request: {RequestType}, CorrelationId: {CorrelationId}",
                typeof(TRequest).Name,
                correlationId);

            return await next();
        }
        catch (Exception ex) when (ex is not AuthorizationException)
        {
            authException = ex;
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            RecordAuthorizationMetrics(stopwatch.Elapsed, isAuthorized, authException);
        }
    }

    private AuthorizationOptions GetAuthorizationOptions()
    {
        // Check for handler-specific overrides
        if (_options.Value.AuthorizationOverrides.TryGetValue(_handlerKey, out var handlerOptions))
        {
            return handlerOptions;
        }

        // Return default options
        return _options.Value.DefaultAuthorizationOptions;
    }

    private static bool IsAuthorizationEnabled(AuthorizationOptions authorizationOptions, AuthorizeAttribute[] authorizeAttributes)
    {
        // If authorization is explicitly disabled globally, return false
        if (!authorizationOptions.EnableAutomaticAuthorization && authorizeAttributes.Length == 0)
        {
            return false;
        }

        // If authorization is enabled globally or explicitly enabled with AuthorizeAttribute, return true
        return authorizationOptions.EnableAutomaticAuthorization || authorizeAttributes.Length > 0;
    }

    private void AddRequestInfoToContext(TRequest request)
    {
        // Add request type information to the context
        _authorizationContext.Properties["RequestType"] = typeof(TRequest).FullName ?? typeof(TRequest).Name;

        // Add correlation ID if available
        var correlationId = _telemetryProvider?.GetCorrelationId();
        if (!string.IsNullOrEmpty(correlationId))
        {
            _authorizationContext.Properties["CorrelationId"] = correlationId;
        }

        // In a real implementation, you would add more request-specific information here
    }

    private AuthorizeAttribute[] GetCachedAuthorizeAttributes()
    {
        return _attributeCache.GetOrAdd(
            typeof(TRequest),
            type => type.GetCustomAttributes<AuthorizeAttribute>(true).ToArray());
    }

    private TResponse GetDefaultResponse()
    {
        // For value types and non-nullable reference types, return default
        // This handles cases where authorization fails but we don't want to throw
        var responseType = typeof(TResponse);

        // If the response type is a Task or ValueTask, we need to handle it specially
        if (responseType.IsGenericType)
        {
            var genericType = responseType.GetGenericTypeDefinition();
            if (genericType == typeof(Task<>))
            {
                var innerType = responseType.GetGenericArguments()[0];
                var defaultValue = GetDefaultValue(innerType);

                // Use reflection to call Task.FromResult<T>(defaultValue)
                var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
                return (TResponse)fromResultMethod.Invoke(null, new[] { defaultValue })!;
            }
            else if (genericType == typeof(ValueTask<>))
            {
                var innerType = responseType.GetGenericArguments()[0];
                var defaultValue = GetDefaultValue(innerType);

                // Create ValueTask<T> with the default value using the result constructor
                var valueTaskType = typeof(ValueTask<>).MakeGenericType(innerType);
                var constructor = valueTaskType.GetConstructor(new[] { innerType });
                return (TResponse)constructor!.Invoke(new[] { defaultValue })!;
            }
        }

        return (TResponse)GetDefaultValue(responseType)!;
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private void RecordAuthorizationMetrics(TimeSpan duration, bool isAuthorized, Exception? exception)
    {
        if (_telemetryProvider?.MetricsProvider == null)
        {
            return;
        }

        try
        {
            var metrics = new HandlerExecutionMetrics
            {
                OperationId = _telemetryProvider.GetCorrelationId() ?? Guid.NewGuid().ToString(),
                RequestType = typeof(TRequest),
                ResponseType = typeof(TResponse),
                HandlerName = "AuthorizationPipelineBehavior",
                Duration = duration,
                Success = isAuthorized && exception == null,
                Exception = exception,
                Timestamp = DateTimeOffset.UtcNow,
                Properties = new()
                {
                    ["AuthorizationResult"] = isAuthorized,
                    ["RequestTypeName"] = typeof(TRequest).Name,
                    ["User"] = GetUserNameFromContext()
                }
            };

            _telemetryProvider.MetricsProvider.RecordHandlerExecution(metrics);
        }
        catch (Exception ex)
        {
            // Don't fail the request if metrics recording fails
            _logger.LogWarning(ex, "Failed to record authorization metrics for request: {RequestType}", typeof(TRequest).Name);
        }
    }

    private string GetUserNameFromContext()
    {
        // Try to get username from claims
        var nameClaim = _authorizationContext.UserClaims?.FirstOrDefault(c =>
            c.Type == ClaimTypes.Name ||
            c.Type == ClaimTypes.NameIdentifier ||
            c.Type == "name" ||
            c.Type == "preferred_username");

        return nameClaim?.Value ?? "Anonymous";
    }
}