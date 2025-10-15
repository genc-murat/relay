using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Core;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Advanced validation rule that verifies email deliverability via external API.
/// Demonstrates validation rules with external dependencies (API calls).
/// </summary>
public class EmailVerificationValidationRule : IValidationRule<string>
{
    private readonly IEmailVerifier _emailVerifier;

    public EmailVerificationValidationRule(IEmailVerifier emailVerifier)
    {
        _emailVerifier = emailVerifier ?? throw new ArgumentNullException(nameof(emailVerifier));
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request))
        {
            return errors; // Let other rules handle empty validation
        }

        try
        {
            var result = await _emailVerifier.VerifyEmailAsync(request, cancellationToken);

            if (!result.IsValid)
            {
                errors.Add("Email address appears to be invalid or undeliverable.");
            }

            if (result.IsDisposable)
            {
                errors.Add("Disposable email addresses are not allowed.");
            }

            if (result.RiskScore > 0.7) // High risk
            {
                errors.Add("Email address has a high risk score. Please use a different email.");
            }
        }
        catch (Exception)
        {
            // Log the error but don't fail validation - allow registration to proceed
            // In production, you might want to have a fallback behavior
            errors.Add("Unable to verify email address. Please try again later.");
        }

        return errors;
    }
}

/// <summary>
/// Result of email verification.
/// </summary>
public class EmailVerificationResult
{
    public bool IsValid { get; set; }
    public bool IsDisposable { get; set; }
    public double RiskScore { get; set; } // 0.0 to 1.0
    public string? Domain { get; set; }
    public string? MxRecords { get; set; }
}

/// <summary>
/// Interface for email verification services.
/// </summary>
public interface IEmailVerifier
{
    /// <summary>
    /// Verifies an email address for deliverability and validity.
    /// </summary>
    /// <param name="email">The email address to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    ValueTask<EmailVerificationResult> VerifyEmailAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation using a mock email verification service.
/// In production, you would integrate with services like:
/// - Abstract API
/// - Hunter.io
/// - NeverBounce
/// - Mailgun's email validation
/// </summary>
public class MockEmailVerifier : IEmailVerifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MockEmailVerifier> _logger;

    public MockEmailVerifier(HttpClient httpClient, ILogger<MockEmailVerifier> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<EmailVerificationResult> VerifyEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, you would call an external API
            // For demonstration, we'll simulate an API call with mock logic

            // Simulate network delay
            await Task.Delay(100, cancellationToken);

            var parts = email.Split('@');
            if (parts.Length != 2)
            {
                return new EmailVerificationResult { IsValid = false };
            }

            var localPart = parts[0];
            var domain = parts[1];

            // Mock validation logic
            var disposableDomains = new[] { "10minutemail.com", "guerrillamail.com", "temp-mail.org" };
            var isDisposable = disposableDomains.Contains(domain.ToLowerInvariant());

            var riskyDomains = new[] { "suspicious.com", "spamdomain.net" };
            var isRisky = riskyDomains.Contains(domain.ToLowerInvariant());

            var riskScore = isDisposable ? 0.9 : (isRisky ? 0.8 : 0.1);

            // Basic format validation
            var isValid = email.Contains('@') &&
                         localPart.Length > 0 &&
                         domain.Contains('.') &&
                         !email.Contains(' ');

            return new EmailVerificationResult
            {
                IsValid = isValid,
                IsDisposable = isDisposable,
                RiskScore = riskScore,
                Domain = domain,
                MxRecords = "mock.mx.records"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify email {Email}", email);
            // Return a safe default that allows the email through
            return new EmailVerificationResult
            {
                IsValid = true,
                IsDisposable = false,
                RiskScore = 0.5,
                Domain = "unknown"
            };
        }
    }
}

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