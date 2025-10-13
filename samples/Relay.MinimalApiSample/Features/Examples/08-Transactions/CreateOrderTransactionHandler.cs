using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.Transactions;

public class CreateOrderTransactionHandler
    : IRequestHandler<CreateOrderTransactionRequest, OrderTransactionResult>
{
    private readonly ILogger<CreateOrderTransactionHandler> _logger;

    public CreateOrderTransactionHandler(ILogger<CreateOrderTransactionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<OrderTransactionResult> HandleAsync(
        CreateOrderTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "TRANSACTION: Creating order for customer {CustomerId}",
            request.CustomerId);

        // Step 1: Create order (would normally be database insert)
        var orderId = Guid.NewGuid();
        _logger.LogInformation("TRANSACTION: Order {OrderId} created", orderId);

        // Step 2: Update inventory (would normally be database update)
        _logger.LogInformation(
            "TRANSACTION: Reducing stock for product {ProductId} by {Quantity}",
            request.ProductId,
            request.Quantity);

        // Step 3: Create payment record (would normally be database insert)
        _logger.LogInformation("TRANSACTION: Payment record created");

        // If any step fails, ALL changes will be rolled back automatically
        // If all succeed, changes are committed automatically

        _logger.LogInformation(
            "TRANSACTION: All steps completed successfully for order {OrderId}",
            orderId);

        var result = new OrderTransactionResult(
            orderId,
            "Success",
            "Order created with transaction guarantee"
        );

        return ValueTask.FromResult(result);
    }
}
