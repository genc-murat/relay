using Relay.Core.Pipeline.Interfaces;

namespace Relay.MinimalApiSample.Features.Examples.PrePostProcessors;

/// <summary>
/// Post-processor: Runs AFTER successful handler execution
/// Used for: audit logging, notifications, cleanup
/// </summary>
public class CreateOrderPostProcessor : IRequestPostProcessor<CreateOrderRequest, CreateOrderResponse>
{
    private readonly ILogger<CreateOrderPostProcessor> _logger;

    public CreateOrderPostProcessor(ILogger<CreateOrderPostProcessor> logger)
    {
        _logger = logger;
    }

    public ValueTask ProcessAsync(
        CreateOrderRequest request,
        CreateOrderResponse response,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "POST-PROCESSING: Order {OrderId} created for customer {CustomerId}",
            response.OrderId,
            response.CustomerId);

        // Simulate audit log
        _logger.LogInformation(
            "POST-PROCESSING: Audit log created for order {OrderId}",
            response.OrderId);

        // Simulate email notification
        _logger.LogInformation(
            "POST-PROCESSING: Sending confirmation email to customer {CustomerId}",
            response.CustomerId);

        // Simulate analytics tracking
        _logger.LogInformation(
            "POST-PROCESSING: Analytics event tracked for order {OrderId}",
            response.OrderId);

        return ValueTask.CompletedTask;
    }
}
