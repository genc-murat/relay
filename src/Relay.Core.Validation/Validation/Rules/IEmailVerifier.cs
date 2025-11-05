using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

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
