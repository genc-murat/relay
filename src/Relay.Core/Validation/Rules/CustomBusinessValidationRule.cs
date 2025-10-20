using Relay.Core.Validation.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Custom business validation rule that demonstrates complex business logic validation.
/// This rule validates a complete business object with multiple interdependent validations.
/// </summary>
public class CustomBusinessValidationRule : IValidationRule<BusinessValidationRequest>
{
    private readonly IBusinessRulesEngine _rulesEngine;

    public CustomBusinessValidationRule(IBusinessRulesEngine rulesEngine)
    {
        _rulesEngine = rulesEngine ?? throw new ArgumentNullException(nameof(rulesEngine));
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validate business rules
        var businessErrors = await _rulesEngine.ValidateBusinessRulesAsync(request, cancellationToken);
        errors.AddRange(businessErrors);

        // Cross-field validations
        if (request.Amount > 0 && string.IsNullOrEmpty(request.PaymentMethod))
        {
            errors.Add("Payment method is required when amount is specified.");
        }

        if (request.IsRecurring && request.EndDate <= request.StartDate)
        {
            errors.Add("End date must be after start date for recurring transactions.");
        }

        if (request.UserType == UserType.Premium && request.Amount < 100)
        {
            errors.Add("Premium users must have minimum transaction amount of $100.");
        }

        // Risk assessment
        var riskScore = await CalculateRiskScoreAsync(request, cancellationToken);
        if (riskScore > 0.8)
        {
            errors.Add("Transaction has high risk score and requires additional verification.");
        }

        return errors;
    }

    private async ValueTask<double> CalculateRiskScoreAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken)
    {
        // Simulate complex risk calculation
        await Task.Delay(50, cancellationToken); // Simulate processing time

        var riskScore = 0.0;

        // Amount-based risk
        if (request.Amount > 10000) riskScore += 0.3;
        else if (request.Amount > 1000) riskScore += 0.1;

        // User type risk
        if (request.UserType == UserType.New) riskScore += 0.2;
        else if (request.UserType == UserType.Suspicious) riskScore += 0.5;

        // Location risk (simplified)
        if (request.CountryCode != "US") riskScore += 0.1;

        // Time-based risk
        var now = DateTime.UtcNow;
        if (now.Hour < 6 || now.Hour > 22) riskScore += 0.1; // Unusual hours

        return Math.Min(riskScore, 1.0); // Cap at 1.0
    }
}
