using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Configuration;

namespace Relay.Core.Authorization
{
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
        private readonly string _handlerKey;

        public AuthorizationPipelineBehavior(
            IAuthorizationService authorizationService,
            IAuthorizationContext authorizationContext,
            ILogger<AuthorizationPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<RelayOptions> options)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _authorizationContext = authorizationContext ?? throw new ArgumentNullException(nameof(authorizationContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Get authorization configuration
            var authorizationOptions = GetAuthorizationOptions();
            var authorizeAttributes = typeof(TRequest).GetCustomAttributes<AuthorizeAttribute>(true).ToArray();

            // Check if authorization is enabled for this request
            if (!IsAuthorizationEnabled(authorizationOptions, authorizeAttributes))
            {
                return await next();
            }

            // Add request-specific information to the authorization context
            AddRequestInfoToContext(request);

            // Check authorization
            if (!await _authorizationService.AuthorizeAsync(_authorizationContext, cancellationToken))
            {
                _logger.LogWarning("Authorization failed for request: {RequestType}", typeof(TRequest).Name);

                if (authorizationOptions.ThrowOnAuthorizationFailure)
                {
                    throw new AuthorizationException($"Authorization failed for request: {typeof(TRequest).Name}");
                }

                // If not throwing, we might want to return a default response or handle it differently
                // For now, we'll just continue to the next handler
            }

            return await next();
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

            // In a real implementation, you would add more request-specific information here
        }
    }
}