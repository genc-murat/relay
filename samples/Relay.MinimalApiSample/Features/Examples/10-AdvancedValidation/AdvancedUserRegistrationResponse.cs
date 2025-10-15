namespace Relay.MinimalApiSample.Features.Examples.AdvancedValidation;

/// <summary>
/// Response for advanced user registration with validation results.
/// </summary>
public record AdvancedUserRegistrationResponse(
    Guid UserId,
    string Username,
    string Email,
    int Age,
    DateTime RegisteredAt,
    string Message,
    ValidationMetrics Metrics
);

/// <summary>
/// Validation metrics showing AI optimization performance.
/// </summary>
public record ValidationMetrics(
    bool WasBatched,
    bool WasCached,
    double ProcessingTimeMs,
    double ConfidenceScore,
    string OptimizationStrategy,
    string PerformanceGain
);