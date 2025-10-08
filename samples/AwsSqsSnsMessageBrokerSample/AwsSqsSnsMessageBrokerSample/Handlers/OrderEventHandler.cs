using AwsSqsSnsMessageBrokerSample.Events;
using Relay.MessageBroker;

namespace AwsSqsSnsMessageBrokerSample.Handlers;

public class OrderEventHandler
{
    private readonly ILogger<OrderEventHandler> _logger;
    private readonly IMessageBroker _messageBroker;

    public OrderEventHandler(
        ILogger<OrderEventHandler> logger,
        IMessageBroker messageBroker)
    {
        _logger = logger;
        _messageBroker = messageBroker;
    }

    public async Task HandleOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing OrderCreatedEvent: OrderId={OrderId}, CustomerId={CustomerId}, Amount={Amount}",
            orderEvent.OrderId, orderEvent.CustomerId, orderEvent.Amount);

        try
        {
            // Simulate order processing
            await Task.Delay(1000, cancellationToken);

            _logger.LogInformation("Order {OrderId} processed successfully", orderEvent.OrderId);

            // Publish OrderProcessedEvent
            await _messageBroker.PublishAsync(
                new OrderProcessedEvent
                {
                    OrderId = orderEvent.OrderId,
                    Status = "Processed",
                    ProcessedAt = DateTime.UtcNow
                },
                null,
                cancellationToken);

            _logger.LogInformation("OrderProcessedEvent published for order {OrderId}", orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order {OrderId}", orderEvent.OrderId);
            throw;
        }
    }

    public Task HandleOrderProcessedAsync(OrderProcessedEvent orderEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Order {OrderId} completed with status: {Status}",
            orderEvent.OrderId, orderEvent.Status);

        return Task.CompletedTask;
    }
}
