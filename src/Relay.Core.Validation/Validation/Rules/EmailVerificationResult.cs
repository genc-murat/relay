namespace Relay.Core.Validation.Rules;

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
