using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Security;

/// <summary>
/// Advanced security pipeline behavior with multi-layer protection.
/// </summary>
public class SecurityPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<SecurityPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly ISecurityContext _securityContext;
    private readonly IRequestAuditor _auditor;
    private readonly IRateLimiter? _rateLimiter;

    public SecurityPipelineBehavior(
        ILogger<SecurityPipelineBehavior<TRequest, TResponse>> logger,
        ISecurityContext securityContext,
        IRequestAuditor auditor,
        IRateLimiter? rateLimiter = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityContext = securityContext ?? throw new ArgumentNullException(nameof(securityContext));
        _auditor = auditor ?? throw new ArgumentNullException(nameof(auditor));
        _rateLimiter = rateLimiter;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest).Name;
        var userId = _securityContext.UserId;

        // Data sanitization
        SanitizeRequest(request);

        // Permission validation
        await ValidatePermissions(request, requestType);

        // Rate limiting per user
        await ValidateUserRateLimit(userId, requestType);

        // Audit logging
        await _auditor.LogRequestAsync(userId, requestType, request, cancellationToken);

        try
        {
            var response = await next();
            
            // Sanitize response data
            SanitizeResponse(response);
            
            // Log successful access
            await _auditor.LogSuccessAsync(userId, requestType, cancellationToken);
            
            return response;
        }
        catch (Exception ex)
        {
            // Log security-related failures
            await _auditor.LogFailureAsync(userId, requestType, ex, cancellationToken);
            throw;
        }
    }

    private void SanitizeRequest(TRequest request)
    {
        // Implement data sanitization logic
        // Remove/mask sensitive data, validate input formats, etc.
    }

    private void SanitizeResponse(TResponse response)
    {
        // Implement response sanitization
        // Remove sensitive fields based on user permissions
    }

    private ValueTask ValidatePermissions(TRequest request, string requestType)
    {
        var requiredPermissions = GetRequiredPermissions(requestType);
        if (requiredPermissions.Any() && !_securityContext.HasPermissions(requiredPermissions))
        {
            _logger.LogWarning("Permission denied for user {UserId} on {RequestType}", 
                _securityContext.UserId, requestType);
            throw new InsufficientPermissionsException(requestType, requiredPermissions);
        }

        return ValueTask.CompletedTask;
    }

    private async ValueTask ValidateUserRateLimit(string userId, string requestType)
    {
        if (_rateLimiter == null)
            return;

        var rateLimitKey = $"{userId}:{requestType}";
        var isAllowed = await _rateLimiter.CheckRateLimitAsync(rateLimitKey);

        if (!isAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId} on {RequestType}", userId, requestType);
            throw new RateLimitExceededException(userId, requestType);
        }
    }

    private IEnumerable<string> GetRequiredPermissions(string requestType)
    {
        // Return required permissions for the request type
        // This could be configured via attributes or configuration
        return Enumerable.Empty<string>();
    }
}