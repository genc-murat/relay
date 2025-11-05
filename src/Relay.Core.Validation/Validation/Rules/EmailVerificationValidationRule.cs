using Relay.Core.Validation.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
