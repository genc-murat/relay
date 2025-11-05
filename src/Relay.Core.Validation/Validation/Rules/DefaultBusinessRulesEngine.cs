using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Default implementation of business rules engine.
/// </summary>
public class DefaultBusinessRulesEngine : IBusinessRulesEngine
{
    private readonly ILogger<DefaultBusinessRulesEngine> _logger;

    public DefaultBusinessRulesEngine(ILogger<DefaultBusinessRulesEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<IEnumerable<string>> ValidateBusinessRulesAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Business rule 1: Amount limits
        if (request.Amount <= 0)
        {
            errors.Add("Transaction amount must be greater than zero.");
        }
        else if (request.Amount > 100000)
        {
            errors.Add("Transaction amount exceeds maximum limit of $100,000.");
        }

        // Business rule 2: User transaction limits
        var maxTransactions = request.UserType switch
        {
            UserType.New => 5,
            UserType.Regular => 50,
            UserType.Premium => 500,
            UserType.Suspicious => 1,
            UserType.Blocked => 0,
            _ => 10
        };

        if (request.UserTransactionCount >= maxTransactions)
        {
            errors.Add($"User has exceeded maximum transaction limit of {maxTransactions}.");
        }

        // Business rule 3: Business category restrictions
        if (request.BusinessCategory == "HighRisk" && request.UserType != UserType.Premium)
        {
            errors.Add("High-risk business category requires premium user status.");
        }

        // Business rule 4: Geographic restrictions
        var restrictedCountries = new[] { "CU", "IR", "KP", "SY" }; // Example restricted countries
        if (restrictedCountries.Contains(request.CountryCode))
        {
            errors.Add("Transactions from this country are not currently supported.");
        }

        // Business rule 5: Time-based restrictions
        if (request.StartDate.Date < DateTime.UtcNow.Date.AddDays(-30))
        {
            errors.Add("Transaction start date cannot be more than 30 days in the past.");
        }

        if (request.EndDate.HasValue && request.EndDate.Value > DateTime.UtcNow.AddYears(2))
        {
            errors.Add("Transaction end date cannot be more than 2 years in the future.");
        }

        // Simulate async processing
        await Task.Delay(25, cancellationToken);

        _logger.LogInformation(
            "Validated business rules for transaction: Amount={Amount}, UserType={UserType}, Errors={ErrorCount}",
            request.Amount,
            request.UserType,
            errors.Count);

        return errors;
    }
}
