using AwsSqsSnsMessageBrokerSample.Events;
using Microsoft.AspNetCore.Mvc;
using Relay.MessageBroker;

namespace AwsSqsSnsMessageBrokerSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IMessageBroker _messageBroker;
    private static readonly Dictionary<string, OrderCreatedEvent> Orders = new();

    public OrdersController(
        ILogger<OrdersController> logger,
        IMessageBroker messageBroker)
    {
        _logger = logger;
        _messageBroker = messageBroker;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var orderId = $"ORD-{Guid.NewGuid():N}";

        var orderEvent = new OrderCreatedEvent
        {
            OrderId = orderId,
            CustomerId = request.CustomerId,
            Amount = request.Amount,
            Items = request.Items,
            CreatedAt = DateTime.UtcNow
        };

        Orders[orderId] = orderEvent;

        _logger.LogInformation("Creating order {OrderId} for customer {CustomerId}", 
            orderId, request.CustomerId);

        await _messageBroker.PublishAsync(orderEvent);

        _logger.LogInformation("Order {OrderId} created and event published", orderId);

        return CreatedAtAction(
            nameof(GetOrder),
            new { id = orderId },
            new { OrderId = orderId, Status = "Created" });
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(string id)
    {
        if (Orders.TryGetValue(id, out var order))
        {
            return Ok(order);
        }

        return NotFound();
    }

    [HttpGet]
    public IActionResult GetAllOrders()
    {
        return Ok(Orders.Values);
    }
}

public record CreateOrderRequest(
    string CustomerId,
    decimal Amount,
    List<string> Items);
