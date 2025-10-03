using Relay.MessageBroker.Saga;

namespace SagaPatternSample.Sagas;

public class OrderSagaData : SagaDataBase
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    
    // Step results
    public Guid? ReservationId { get; set; }
    public string? PaymentTransactionId { get; set; }
    public string? ShipmentId { get; set; }
}

public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
