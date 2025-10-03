using SagaPatternSample.Commands;

namespace SagaPatternSample.Services;

public class InventoryService
{
    private readonly ILogger<InventoryService> _logger;
    private readonly Dictionary<string, List<OrderItem>> _reservations = new();

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ReserveInventory(string orderId, List<OrderItem> items)
    {
        await Task.Delay(50); // Simulate database operation
        
        // Simulate inventory check - fail for specific products
        if (items.Any(i => i.ProductId == "OUT_OF_STOCK"))
        {
            _logger.LogWarning("Product OUT_OF_STOCK is not available");
            return false;
        }
        
        _reservations[orderId] = items;
        _logger.LogInformation("Reserved {Count} items for order {OrderId}", items.Count, orderId);
        return true;
    }

    public async Task<bool> ReleaseInventory(string orderId)
    {
        await Task.Delay(50); // Simulate database operation
        
        if (_reservations.Remove(orderId))
        {
            _logger.LogInformation("Released inventory for order {OrderId}", orderId);
            return true;
        }
        
        _logger.LogWarning("No reservation found for order {OrderId}", orderId);
        return false;
    }
}

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly Dictionary<string, decimal> _payments = new();

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ProcessPayment(string customerId, decimal amount)
    {
        await Task.Delay(100); // Simulate payment gateway call
        
        // Simulate payment failure for specific customers
        if (customerId == "INSUFFICIENT_FUNDS")
        {
            _logger.LogWarning("Insufficient funds for customer {CustomerId}", customerId);
            return false;
        }
        
        _payments[customerId] = amount;
        _logger.LogInformation("Processed payment of {Amount} for customer {CustomerId}", amount, customerId);
        return true;
    }

    public async Task<bool> RefundPayment(string customerId, decimal amount)
    {
        await Task.Delay(100); // Simulate payment gateway call
        
        if (_payments.ContainsKey(customerId))
        {
            _payments.Remove(customerId);
            _logger.LogInformation("Refunded payment of {Amount} for customer {CustomerId}", amount, customerId);
            return true;
        }
        
        _logger.LogWarning("No payment found for customer {CustomerId}", customerId);
        return false;
    }
}

public class ShippingService
{
    private readonly ILogger<ShippingService> _logger;
    private readonly HashSet<string> _shippedOrders = new();

    public ShippingService(ILogger<ShippingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ShipOrder(string orderId)
    {
        await Task.Delay(50); // Simulate shipping provider API call
        
        // Simulate shipping failure for specific orders
        if (orderId == "SHIP_FAIL")
        {
            _logger.LogWarning("Shipping failed for order {OrderId}", orderId);
            return false;
        }
        
        _shippedOrders.Add(orderId);
        _logger.LogInformation("Shipped order {OrderId}", orderId);
        return true;
    }

    public async Task<bool> CancelShipment(string orderId)
    {
        await Task.Delay(50); // Simulate shipping provider API call
        
        if (_shippedOrders.Remove(orderId))
        {
            _logger.LogInformation("Cancelled shipment for order {OrderId}", orderId);
            return true;
        }
        
        _logger.LogWarning("No shipment found for order {OrderId}", orderId);
        return false;
    }
}
