using Relay.Core;
using SagaPatternSample.Commands;
using SagaPatternSample.Services;

namespace SagaPatternSample.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, string>
{
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(ILogger<CreateOrderCommandHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<string> HandleAsync(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order {OrderId} for customer {CustomerId}", 
            request.OrderId, request.CustomerId);
        
        // Simulate order creation
        await Task.Delay(100, cancellationToken);
        
        return request.OrderId;
    }
}

public class ReserveInventoryCommandHandler : IRequestHandler<ReserveInventoryCommand, bool>
{
    private readonly ILogger<ReserveInventoryCommandHandler> _logger;
    private readonly InventoryService _inventoryService;

    public ReserveInventoryCommandHandler(
        ILogger<ReserveInventoryCommandHandler> logger,
        InventoryService inventoryService)
    {
        _logger = logger;
        _inventoryService = inventoryService;
    }

    public async ValueTask<bool> HandleAsync(ReserveInventoryCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reserving inventory for order {OrderId}", request.OrderId);
        
        var result = await _inventoryService.ReserveInventory(request.OrderId, request.Items);
        
        if (result)
        {
            _logger.LogInformation("Inventory reserved successfully for order {OrderId}", request.OrderId);
        }
        else
        {
            _logger.LogWarning("Failed to reserve inventory for order {OrderId}", request.OrderId);
        }
        
        return result;
    }
}

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, bool>
{
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;
    private readonly PaymentService _paymentService;

    public ProcessPaymentCommandHandler(
        ILogger<ProcessPaymentCommandHandler> logger,
        PaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }

    public async ValueTask<bool> HandleAsync(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}", 
            request.OrderId, request.Amount);
        
        var result = await _paymentService.ProcessPayment(request.CustomerId, request.Amount);
        
        if (result)
        {
            _logger.LogInformation("Payment processed successfully for order {OrderId}", request.OrderId);
        }
        else
        {
            _logger.LogWarning("Payment failed for order {OrderId}", request.OrderId);
        }
        
        return result;
    }
}

public class ShipOrderCommandHandler : IRequestHandler<ShipOrderCommand, bool>
{
    private readonly ILogger<ShipOrderCommandHandler> _logger;
    private readonly ShippingService _shippingService;

    public ShipOrderCommandHandler(
        ILogger<ShipOrderCommandHandler> logger,
        ShippingService shippingService)
    {
        _logger = logger;
        _shippingService = shippingService;
    }

    public async ValueTask<bool> HandleAsync(ShipOrderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shipping order {OrderId}", request.OrderId);
        
        var result = await _shippingService.ShipOrder(request.OrderId);
        
        if (result)
        {
            _logger.LogInformation("Order {OrderId} shipped successfully", request.OrderId);
        }
        else
        {
            _logger.LogWarning("Failed to ship order {OrderId}", request.OrderId);
        }
        
        return result;
    }
}

public class SendNotificationCommandHandler : IRequestHandler<SendNotificationCommand, bool>
{
    private readonly ILogger<SendNotificationCommandHandler> _logger;

    public SendNotificationCommandHandler(ILogger<SendNotificationCommandHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> HandleAsync(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sending notification to customer for order {OrderId}: {Message}", 
            request.OrderId, request.Message);
        
        // Simulate notification sending
        await Task.Delay(100, cancellationToken);
        
        return true;
    }
}
