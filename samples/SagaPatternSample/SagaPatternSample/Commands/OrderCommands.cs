using Relay.Core;

namespace SagaPatternSample.Commands;

public class CreateOrderCommand : IRequest<string>
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class ReserveInventoryCommand : IRequest<bool>
{
    public string OrderId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class ProcessPaymentCommand : IRequest<bool>
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ShipOrderCommand : IRequest<bool>
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}

public class SendNotificationCommand : IRequest<bool>
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
