using Microsoft.AspNetCore.Mvc;
using Relay.MessageBroker.Saga;
using SagaPatternSample.Models;
using SagaPatternSample.Sagas;

namespace SagaPatternSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderSaga _orderSaga;
    private readonly ISagaPersistence<OrderSagaData> _sagaPersistence;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderSaga orderSaga,
        ISagaPersistence<OrderSagaData> sagaPersistence,
        ILogger<OrdersController> logger)
    {
        _orderSaga = orderSaga;
        _sagaPersistence = sagaPersistence;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order using saga pattern (success scenario)
    /// </summary>
    [HttpPost("create")]
    public async Task<ActionResult<OrderResult>> CreateOrder([FromBody] OrderDto orderDto)
    {
        try
        {
            _logger.LogInformation("Received order creation request for customer {CustomerId}", 
                orderDto.CustomerId);

            var sagaData = new OrderSagaData
            {
                OrderId = orderDto.OrderId,
                CustomerId = orderDto.CustomerId,
                Items = orderDto.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
                TotalAmount = orderDto.TotalAmount,
                CorrelationId = orderDto.OrderId.ToString()
            };

            var result = await _orderSaga.ExecuteAsync(sagaData, CancellationToken.None);

            // Save saga state
            await _sagaPersistence.SaveAsync(result.Data, CancellationToken.None);

            return Ok(new OrderResult
            {
                OrderId = orderDto.OrderId,
                Success = result.IsSuccess,
                Message = result.IsSuccess 
                    ? "Order created successfully" 
                    : $"Order creation failed: {result.Exception?.Message}",
                SagaStatus = result.Data.State.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new OrderResult
            {
                OrderId = orderDto.OrderId,
                Success = false,
                Message = $"Internal server error: {ex.Message}",
                SagaStatus = "Error"
            });
        }
    }

    /// <summary>
    /// Creates an order that will fail at inventory step (compensation demo)
    /// </summary>
    [HttpPost("create-inventory-fail")]
    public async Task<ActionResult<OrderResult>> CreateOrderInventoryFail()
    {
        var sagaData = new OrderSagaData
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "CUST-001",
            Items = new List<OrderItem>
            {
                new() { ProductId = "OUT_OF_STOCK", Quantity = 1, Price = 100 }
            },
            TotalAmount = 100,
            CorrelationId = Guid.NewGuid().ToString()
        };

        var result = await _orderSaga.ExecuteAsync(sagaData, CancellationToken.None);
        await _sagaPersistence.SaveAsync(result.Data, CancellationToken.None);

        return Ok(new OrderResult
        {
            OrderId = sagaData.OrderId,
            Success = result.IsSuccess,
            Message = "Inventory reservation failed - compensation triggered",
            SagaStatus = result.Data.State.ToString()
        });
    }

    /// <summary>
    /// Creates an order that will fail at payment step (compensation demo)
    /// </summary>
    [HttpPost("create-payment-fail")]
    public async Task<ActionResult<OrderResult>> CreateOrderPaymentFail()
    {
        var sagaData = new OrderSagaData
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "INSUFFICIENT_FUNDS",
            Items = new List<OrderItem>
            {
                new() { ProductId = "PROD-001", Quantity = 1, Price = 100 }
            },
            TotalAmount = 100,
            CorrelationId = Guid.NewGuid().ToString()
        };

        var result = await _orderSaga.ExecuteAsync(sagaData, CancellationToken.None);
        await _sagaPersistence.SaveAsync(result.Data, CancellationToken.None);

        return Ok(new OrderResult
        {
            OrderId = sagaData.OrderId,
            Success = result.IsSuccess,
            Message = "Payment failed - compensation triggered",
            SagaStatus = result.Data.State.ToString()
        });
    }

    /// <summary>
    /// Gets the status of a saga by saga ID
    /// </summary>
    [HttpGet("{sagaId}/status")]
    public async Task<ActionResult<OrderSagaData>> GetSagaStatus(Guid sagaId)
    {
        try
        {
            var state = await _sagaPersistence.GetByIdAsync(sagaId);
            
            if (state == null)
            {
                return NotFound($"Saga with ID {sagaId} not found");
            }

            return Ok(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting saga status");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
