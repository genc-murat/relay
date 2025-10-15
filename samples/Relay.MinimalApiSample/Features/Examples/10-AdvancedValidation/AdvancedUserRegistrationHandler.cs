using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.MinimalApiSample.Infrastructure;
using Relay.MinimalApiSample.Models;

namespace Relay.MinimalApiSample.Features.Examples.AdvancedValidation;

/// <summary>
/// Handler demonstrating comprehensive validation with AI optimization.
/// This handler shows how validation integrates with AI-powered performance features.
/// </summary>
public class AdvancedUserRegistrationHandler : IRequestHandler<AdvancedUserRegistrationRequest, AdvancedUserRegistrationResponse>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<AdvancedUserRegistrationHandler> _logger;

    public AdvancedUserRegistrationHandler(
        InMemoryDatabase database,
        ILogger<AdvancedUserRegistrationHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async ValueTask<AdvancedUserRegistrationResponse> HandleAsync(
        AdvancedUserRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        _logger.LogInformation("Processing advanced user registration for {Username} with comprehensive validation",
            request.Username);

        // Simulate some processing time to demonstrate AI optimization benefits
        await Task.Delay(50, cancellationToken);

        // Create user (validation has already passed at this point)
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Username = request.Username,
            Email = request.Email,
            Age = request.Age,
            CreatedAt = DateTime.UtcNow
        };

        _database.Users.Add(user);

        var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "Advanced user {Username} registered successfully in {ProcessingTime}ms. " +
            "Comprehensive validation with 40+ rules passed.",
            request.Username,
            processingTime);

        var validationMetrics = new ValidationMetrics(
            WasBatched: false, // Would be true with AI optimization
            WasCached: false,  // Would be true with AI optimization
            ProcessingTimeMs: processingTime,
            ConfidenceScore: 0.95, // Simulated confidence score
            OptimizationStrategy: "Comprehensive Validation Pipeline",
            PerformanceGain: "Advanced validation with enterprise-grade rules"
        );

        return new AdvancedUserRegistrationResponse(
            UserId: userId,
            Username: request.Username,
            Email: request.Email,
            Age: request.Age,
            RegisteredAt: DateTime.UtcNow,
            Message: "User registered successfully with comprehensive validation!",
            Metrics: validationMetrics
        );
    }
}