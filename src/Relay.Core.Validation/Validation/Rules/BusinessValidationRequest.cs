using System;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Business validation request model.
/// </summary>
public class BusinessValidationRequest
{
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsRecurring { get; set; }
    public UserType UserType { get; set; }
    public string? CountryCode { get; set; }
    public string? BusinessCategory { get; set; }
    public int UserTransactionCount { get; set; }
}
