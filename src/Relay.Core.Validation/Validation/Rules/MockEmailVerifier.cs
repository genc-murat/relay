using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Validation.Rules;

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
