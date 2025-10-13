using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.PrePostProcessors;

public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, CreateOrderResponse>
{
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(ILogger<CreateOrderHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<CreateOrderResponse> HandleAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing order for customer {CustomerId} with amount {Amount}",
            request.CustomerId,
            request.TotalAmount);

        // Order processing logic
        var orderId = Guid.NewGuid();

        var response = new CreateOrderResponse(
            orderId,
            request.CustomerId,
            request.TotalAmount,
            DateTime.UtcNow,
            "Confirmed"
        );

        _logger.LogInformation("Order {OrderId} created successfully", orderId);

        return ValueTask.FromResult(response);
    }
}
