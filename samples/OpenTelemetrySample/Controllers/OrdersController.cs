using Microsoft.AspNetCore.Mvc;
using OpenTelemetrySample.Commands;
using Relay.Core.Mediator;
using System.Diagnostics;

namespace OpenTelemetrySample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;
    private static readonly ActivitySource ActivitySource = new("Relay.API");

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new order - demonstrates full tracing through multiple handlers and services
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        using var activity = ActivitySource.StartActivity("POST /api/orders", ActivityKind.Server);
        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.route", "/api/orders");
        activity?.SetTag("customer.id", command.CustomerId);

        try
        {
            _logger.LogInformation("Received order creation request");

            var result = await _mediator.Send(command);

            activity?.SetTag("order.id", result.OrderId);
            activity?.SetTag("order.status", result.Status);
            activity?.SetTag("order.total", result.Total);

            if (result.Status == "Confirmed")
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Ok(result);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.Status);
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Error processing order");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Simulates a slow operation for performance testing
    /// </summary>
    [HttpGet("slow")]
    public async Task<ActionResult<string>> SlowOperation()
    {
        using var activity = ActivitySource.StartActivity("SlowOperation", ActivityKind.Internal);
        
        activity?.AddEvent(new ActivityEvent("Starting slow operation"));
        await Task.Delay(2000);
        activity?.AddEvent(new ActivityEvent("Slow operation completed"));

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Ok("Operation completed after 2 seconds");
    }

    /// <summary>
    /// Simulates an error for error tracking
    /// </summary>
    [HttpGet("error")]
    public ActionResult<string> ErrorOperation()
    {
        using var activity = ActivitySource.StartActivity("ErrorOperation", ActivityKind.Internal);
        
        try
        {
            throw new InvalidOperationException("This is a simulated error for testing");
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Simulated error occurred");
            throw;
        }
    }

    /// <summary>
    /// Demonstrates manual span creation and custom metrics
    /// </summary>
    [HttpPost("complex")]
    public async Task<ActionResult<string>> ComplexOperation()
    {
        using var activity = ActivitySource.StartActivity("ComplexOperation", ActivityKind.Internal);

        // Step 1
        using (var step1 = ActivitySource.StartActivity("Step1-DataValidation", ActivityKind.Internal))
        {
            step1?.AddEvent(new ActivityEvent("Validating data"));
            await Task.Delay(100);
            step1?.SetStatus(ActivityStatusCode.Ok);
        }

        // Step 2
        using (var step2 = ActivitySource.StartActivity("Step2-BusinessLogic", ActivityKind.Internal))
        {
            step2?.AddEvent(new ActivityEvent("Executing business logic"));
            await Task.Delay(200);
            step2?.SetTag("processed.items", 42);
            step2?.SetStatus(ActivityStatusCode.Ok);
        }

        // Step 3
        using (var step3 = ActivitySource.StartActivity("Step3-DataPersistence", ActivityKind.Internal))
        {
            step3?.AddEvent(new ActivityEvent("Saving to database"));
            await Task.Delay(150);
            step3?.SetStatus(ActivityStatusCode.Ok);
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
        return Ok("Complex operation completed with detailed tracing");
    }
}
