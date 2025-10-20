using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Circuit breaker implementation for email verification.
/// Prevents cascading failures when the external service is down.
/// </summary>
public class CircuitBreakerEmailVerifier : IEmailVerifier
{
    private readonly IEmailVerifier _innerVerifier;
    private readonly ILogger<CircuitBreakerEmailVerifier> _logger;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private readonly TimeSpan _timeoutPeriod = TimeSpan.FromMinutes(5);
    private readonly int _failureThreshold = 3;

    public CircuitBreakerEmailVerifier(IEmailVerifier innerVerifier, ILogger<CircuitBreakerEmailVerifier> logger)
    {
        _innerVerifier = innerVerifier ?? throw new ArgumentNullException(nameof(innerVerifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<EmailVerificationResult> VerifyEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Check if circuit breaker is open
        if (IsCircuitBreakerOpen())
        {
            _logger.LogWarning("Circuit breaker is open, skipping email verification for {Email}", email);
            return new EmailVerificationResult
            {
                IsValid = true, // Allow through when circuit is open
                IsDisposable = false,
                RiskScore = 0.5,
                Domain = "circuit-breaker-open"
            };
        }

        try
        {
            var result = await _innerVerifier.VerifyEmailAsync(email, cancellationToken);

            // Reset failure count on success
            _failureCount = 0;

            return result;
        }
        catch (Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            _logger.LogError(ex, "Email verification failed for {Email}, failure count: {Count}", email, _failureCount);

            // Return safe default
            return new EmailVerificationResult
            {
                IsValid = true,
                IsDisposable = false,
                RiskScore = 0.5,
                Domain = "verification-failed"
            };
        }
    }

    private bool IsCircuitBreakerOpen()
    {
        if (_failureCount < _failureThreshold)
        {
            return false;
        }

        // Check if timeout period has passed
        return DateTime.UtcNow - _lastFailureTime < _timeoutPeriod;
    }
}