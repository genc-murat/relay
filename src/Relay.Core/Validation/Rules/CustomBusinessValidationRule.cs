using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Core;
using Relay.Core.Validation.Interfaces;

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

/// <summary>
/// User type enumeration.
/// </summary>
public enum UserType
{
    New,
    Regular,
    Premium,
    Suspicious,
    Blocked
}

/// <summary>
/// Business rules engine interface.
/// </summary>
public interface IBusinessRulesEngine
{
    ValueTask<IEnumerable<string>> ValidateBusinessRulesAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default);
}

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

/// <summary>
/// Cached business rules engine for performance.
/// </summary>
public class CachedBusinessRulesEngine : IBusinessRulesEngine
{
    private readonly IBusinessRulesEngine _innerEngine;
    private readonly ICache _cache;

    public CachedBusinessRulesEngine(IBusinessRulesEngine innerEngine, ICache cache)
    {
        _innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async ValueTask<IEnumerable<string>> ValidateBusinessRulesAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Create cache key based on request properties that affect validation
        var cacheKey = $"business_rules_{request.UserType}_{request.CountryCode}_{request.BusinessCategory}";

        // Try cache first
        var cached = await _cache.GetAsync<IEnumerable<string>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        // Execute validation
        var errors = await _innerEngine.ValidateBusinessRulesAsync(request, cancellationToken);

        // Cache results for 10 minutes (business rules don't change frequently)
        await _cache.SetAsync(cacheKey, errors, TimeSpan.FromMinutes(10), cancellationToken);

        return errors;
    }
}

/// <summary>
/// Simple cache interface (same as before).
/// </summary>
public interface ICache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
}