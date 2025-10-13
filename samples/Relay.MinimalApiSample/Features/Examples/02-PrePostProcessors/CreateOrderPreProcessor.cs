using Relay.Core.Pipeline.Interfaces;

namespace Relay.MinimalApiSample.Features.Examples.PrePostProcessors;

/// <summary>
/// Pre-processor: Runs BEFORE the handler
/// Used for: validation, logging, data enrichment
/// </summary>
public class CreateOrderPreProcessor : IRequestPreProcessor<CreateOrderRequest>
{
    private readonly ILogger<CreateOrderPreProcessor> _logger;

    public CreateOrderPreProcessor(ILogger<CreateOrderPreProcessor> logger)
    {
        _logger = logger;
    }

    public ValueTask ProcessAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "PRE-PROCESSING: Order for customer {CustomerId}",
            request.CustomerId);

        _logger.LogInformation(
            "PRE-PROCESSING: Validating stock for {ItemCount} items",
            request.Items.Length);

        // Simulate stock check
        _logger.LogInformation("PRE-PROCESSING: Stock validation passed");

        // Simulate price calculation
        _logger.LogInformation(
            "PRE-PROCESSING: Price calculated: {Amount}",
            request.TotalAmount);

        return ValueTask.CompletedTask;
    }
}
