# Advanced Validation Features

This document describes advanced validation features in Relay that go beyond simple attribute-based validation, including external dependencies, custom business logic, and enterprise-grade validation patterns.

## Overview

While Relay provides 78+ built-in validation rules for common scenarios, advanced use cases often require:

- **External Dependencies**: Database lookups, API calls, external services
- **Complex Business Logic**: Multi-field validations, risk assessments, business rules
- **Performance Optimizations**: Caching, circuit breakers, async processing
- **Enterprise Patterns**: Circuit breakers, fallbacks, monitoring

## Features

### 1. Database-Backed Validation

Validation rules that check against database state for uniqueness, existence, or business constraints.

#### Unique Username Validation

```csharp
public class UniqueUsernameValidationRule : IValidationRule<string>
{
    private readonly IUsernameUniquenessChecker _checker;

    public UniqueUsernameValidationRule(IUsernameUniquenessChecker checker)
    {
        _checker = checker;
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(string username, CancellationToken ct)
    {
        var isUnique = await _checker.IsUsernameUniqueAsync(username, ct);
        return isUnique ? Array.Empty<string>() : new[] { "Username already taken" };
    }
}

// Usage
[UniqueUsername]
public string Username { get; set; }
```

#### Implementation Options

- **Direct Database**: Query database directly
- **Cached**: Use cache for performance
- **Circuit Breaker**: Handle database failures gracefully

### 2. API-Based Validation

Validation rules that call external APIs for verification.

#### Email Verification

```csharp
public class EmailVerificationValidationRule : IValidationRule<string>
{
    private readonly IEmailVerifier _verifier;

    public EmailVerificationValidationRule(IEmailVerifier verifier)
    {
        _verifier = verifier;
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(string email, CancellationToken ct)
    {
        var result = await _verifier.VerifyEmailAsync(email, ct);

        var errors = new List<string>();
        if (!result.IsValid) errors.Add("Invalid email");
        if (result.IsDisposable) errors.Add("Disposable emails not allowed");
        if (result.RiskScore > 0.7) errors.Add("High-risk email");

        return errors;
    }
}
```

#### External Services Integration

- **Email Verification**: Abstract API, Hunter.io, NeverBounce
- **Address Validation**: Google Maps API, USPS API
- **Phone Verification**: Twilio, Nexmo
- **Identity Verification**: Government APIs, credit bureaus

### 3. Complex Business Validation

Validation that involves multiple fields and complex business logic.

#### Business Rules Engine

```csharp
public class CustomBusinessValidationRule : IValidationRule<BusinessValidationRequest>
{
    private readonly IBusinessRulesEngine _rulesEngine;

    public CustomBusinessValidationRule(IBusinessRulesEngine rulesEngine)
    {
        _rulesEngine = rulesEngine;
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(
        BusinessValidationRequest request, CancellationToken ct)
    {
        // Cross-field validation
        if (request.Amount > 0 && string.IsNullOrEmpty(request.PaymentMethod))
            return new[] { "Payment method required" };

        // Business rules validation
        var businessErrors = await _rulesEngine.ValidateBusinessRulesAsync(request, ct);

        // Risk assessment
        var riskScore = await CalculateRiskScoreAsync(request, ct);
        if (riskScore > 0.8)
            businessErrors = businessErrors.Append("High risk transaction");

        return businessErrors;
    }
}
```

## Enterprise Patterns

### Circuit Breaker Pattern

Prevent cascading failures when external services are down.

```csharp
public class CircuitBreakerEmailVerifier : IEmailVerifier
{
    private readonly IEmailVerifier _innerVerifier;
    private int _failureCount;
    private DateTime _lastFailureTime;
    private readonly TimeSpan _timeoutPeriod = TimeSpan.FromMinutes(5);

    public async ValueTask<EmailVerificationResult> VerifyEmailAsync(string email, CancellationToken ct)
    {
        if (IsCircuitBreakerOpen())
        {
            // Return safe default when circuit is open
            return new EmailVerificationResult { IsValid = true, RiskScore = 0.5 };
        }

        try
        {
            var result = await _innerVerifier.VerifyEmailAsync(email, ct);
            _failureCount = 0; // Reset on success
            return result;
        }
        catch
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            throw;
        }
    }

    private bool IsCircuitBreakerOpen() =>
        _failureCount >= 3 && DateTime.UtcNow - _lastFailureTime < _timeoutPeriod;
}
```

### Caching Strategy

Improve performance by caching validation results.

```csharp
public class CachedUsernameUniquenessChecker : IUsernameUniquenessChecker
{
    private readonly IUsernameUniquenessChecker _innerChecker;
    private readonly ICache _cache;

    public async ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken ct)
    {
        var cacheKey = $"username_unique_{username}";
        var cached = await _cache.GetAsync<bool?>(cacheKey, ct);

        if (cached.HasValue)
            return cached.Value;

        var isUnique = await _innerChecker.IsUsernameUniqueAsync(username, ct);
        await _cache.SetAsync(cacheKey, isUnique, TimeSpan.FromMinutes(5), ct);

        return isUnique;
    }
}
```

### Fallback Strategies

Handle failures gracefully with fallback validation.

```csharp
public async ValueTask<IEnumerable<string>> ValidateWithFallbackAsync(
    string value, CancellationToken ct)
{
    try
    {
        // Try primary validation (e.g., external API)
        return await _primaryValidator.ValidateAsync(value, ct);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Primary validation failed, using fallback");

        // Fallback to simpler validation
        return await _fallbackValidator.ValidateAsync(value, ct);
    }
}
```

## Configuration

### Service Registration

```csharp
builder.Services.AddTransient<IUsernameUniquenessChecker, CachedUsernameUniquenessChecker>();
builder.Services.AddTransient<IEmailVerifier, CircuitBreakerEmailVerifier>();
builder.Services.AddTransient<IBusinessRulesEngine, CachedBusinessRulesEngine>();

// Register validation rules
builder.Services.AddTransient<IValidationRule<string>, UniqueUsernameValidationRule>();
builder.Services.AddTransient<IValidationRule<string>, EmailVerificationValidationRule>();
builder.Services.AddTransient<IValidationRule<BusinessValidationRequest>, CustomBusinessValidationRule>();
```

### Configuration Options

```csharp
builder.Services.AddRelayValidation(options =>
{
    options.EnableExternalValidation = true;
    options.ExternalValidationTimeout = TimeSpan.FromSeconds(10);
    options.CacheValidationResults = true;
    options.ValidationCacheDuration = TimeSpan.FromMinutes(5);
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 3;
    options.CircuitBreakerTimeout = TimeSpan.FromMinutes(5);
});
```

## Performance Considerations

### Async Processing
All advanced validation is async to prevent blocking the request pipeline.

### Caching Strategy
- **Validation Results**: Cache successful validation results
- **External Data**: Cache external API responses
- **Business Rules**: Cache stable business rule evaluations

### Monitoring
```csharp
public class ValidationMetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;

        // Track external validation calls
        if (request is IExternalValidationRequest)
        {
            _metrics.IncrementExternalValidationCount();
        }

        var response = await next();

        var duration = DateTime.UtcNow - startTime;
        _metrics.RecordValidationDuration(duration);

        return response;
    }
}
```

## Best Practices

### 1. Dependency Injection
Always inject dependencies rather than creating them internally.

### 2. Error Handling
Handle external service failures gracefully with fallbacks.

### 3. Performance
Use caching and circuit breakers for external dependencies.

### 4. Testing
Mock external dependencies for unit tests.

### 5. Monitoring
Monitor validation performance and failure rates.

### 6. Security
Validate inputs before calling external services.

## Examples

See the following files for complete implementations:
- `UniqueUsernameValidationRule.cs` - Database-backed uniqueness validation
- `EmailVerificationValidationRule.cs` - API-based email verification
- `CustomBusinessValidationRule.cs` - Complex business logic validation

## Migration from Simple Validation

If you're migrating from simple attribute validation:

1. **Identify Complex Rules**: Find validation that requires external data
2. **Create Custom Rules**: Implement `IValidationRule<T>` for complex logic
3. **Add Dependencies**: Inject required services (database, HTTP clients, etc.)
4. **Configure Caching**: Add caching for performance
5. **Add Circuit Breakers**: Handle external service failures
6. **Update Tests**: Mock external dependencies in tests