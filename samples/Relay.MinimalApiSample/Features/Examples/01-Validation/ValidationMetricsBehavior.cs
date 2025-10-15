using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

/// <summary>
/// Pipeline behavior that tracks validation performance metrics.
/// Demonstrates how validation integrates with pipeline behaviors.
/// </summary>
public class ValidationMetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<ValidationMetricsBehavior<TRequest, TResponse>> _logger;

    public ValidationMetricsBehavior(ILogger<ValidationMetricsBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var requestType = typeof(TRequest).Name;

        _logger.LogInformation("Starting validation pipeline for {RequestType}", requestType);

        // Execute the request (validation happens here)
        var response = await next();

        var totalTime = DateTime.UtcNow - startTime;

        // Log validation metrics
        _logger.LogInformation(
            "Validation pipeline completed for {RequestType} in {TotalTime}ms. " +
            "Validation passed.",
            requestType,
            totalTime.TotalMilliseconds);

        return response;
    }
}