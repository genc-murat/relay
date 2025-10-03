using Relay.Core;

namespace OpenTelemetrySample.Commands;

public class CreateOrderCommand : IRequest<OrderResponse>
{
    public string CustomerId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class ProcessPaymentCommand : IRequest<PaymentResponse>
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class SendEmailCommand : IRequest<bool>
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public record OrderResponse(string OrderId, string Status, decimal Total);
public record PaymentResponse(string TransactionId, bool Success, string Message);
