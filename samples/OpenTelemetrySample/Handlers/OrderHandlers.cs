using Relay.Core;
using Relay.Core.Mediator;
using OpenTelemetrySample.Commands;
using OpenTelemetrySample.Services;
using System.Diagnostics;

namespace OpenTelemetrySample.Handlers;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly InventoryService _inventoryService;
    private static readonly ActivitySource ActivitySource = new("Relay.OrderProcessing");

    public CreateOrderCommandHandler(
        ILogger<CreateOrderCommandHandler> logger,
        IMediator mediator,
        InventoryService inventoryService)
    {
        _logger = logger;
        _mediator = mediator;
        _inventoryService = inventoryService;
    }

    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("CreateOrder", ActivityKind.Internal);
        activity?.SetTag("customer.id", request.CustomerId);
        activity?.SetTag("product.id", request.ProductId);
        activity?.SetTag("quantity", request.Quantity);

        try
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);

            // Check inventory
            var available = await _inventoryService.CheckAvailabilityAsync(
                request.ProductId, 
                request.Quantity);

            if (!available)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Product not available");
                _logger.LogWarning("Product {ProductId} not available", request.ProductId);
                return new OrderResponse("", "OutOfStock", 0);
            }

            // Create order
            var orderId = Guid.NewGuid().ToString();
            var total = request.Price * request.Quantity;

            activity?.AddEvent(new ActivityEvent("OrderCreated", 
                tags: new ActivityTagsCollection
                {
                    { "order.id", orderId },
                    { "order.total", total }
                }));

            // Process payment
            var paymentCommand = new ProcessPaymentCommand
            {
                OrderId = orderId,
                Amount = total,
                PaymentMethod = "CreditCard"
            };

            var paymentResult = await _mediator.Send(paymentCommand, cancellationToken);

            if (!paymentResult.Success)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Payment failed");
                _logger.LogError("Payment failed for order {OrderId}", orderId);
                return new OrderResponse(orderId, "PaymentFailed", total);
            }

            // Send confirmation email
            var emailCommand = new SendEmailCommand
            {
                To = request.CustomerId + "@example.com",
                Subject = "Order Confirmation",
                Body = $"Your order {orderId} has been confirmed!"
            };

            await _mediator.Send(emailCommand, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Order {OrderId} created successfully", orderId);

            return new OrderResponse(orderId, "Confirmed", total);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }
}

public class ProcessPaymentCommandHandler : IRequestHandler<ProcessPaymentCommand, PaymentResponse>
{
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;
    private readonly PaymentService _paymentService;
    private static readonly ActivitySource ActivitySource = new("Relay.PaymentProcessing");

    public ProcessPaymentCommandHandler(
        ILogger<ProcessPaymentCommandHandler> logger,
        PaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }

    public async Task<PaymentResponse> Handle(ProcessPaymentCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment", ActivityKind.Internal);
        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("amount", request.Amount);
        activity?.SetTag("payment.method", request.PaymentMethod);

        try
        {
            _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}", 
                request.OrderId, request.Amount);

            var result = await _paymentService.ProcessAsync(request.Amount, request.PaymentMethod);

            if (result.Success)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.AddEvent(new ActivityEvent("PaymentSuccessful",
                    tags: new ActivityTagsCollection
                    {
                        { "transaction.id", result.TransactionId }
                    }));

                _logger.LogInformation("Payment successful. Transaction ID: {TransactionId}", 
                    result.TransactionId);
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, result.Message);
                _logger.LogWarning("Payment failed: {Message}", result.Message);
            }

            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Error processing payment");
            throw;
        }
    }
}

public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, bool>
{
    private readonly ILogger<SendEmailCommandHandler> _logger;
    private readonly EmailService _emailService;
    private static readonly ActivitySource ActivitySource = new("Relay.EmailService");

    public SendEmailCommandHandler(
        ILogger<SendEmailCommandHandler> logger,
        EmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<bool> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity("SendEmail", ActivityKind.Internal);
        activity?.SetTag("email.to", request.To);
        activity?.SetTag("email.subject", request.Subject);

        try
        {
            _logger.LogInformation("Sending email to {To}", request.To);

            await _emailService.SendAsync(request.To, request.Subject, request.Body);

            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.AddEvent(new ActivityEvent("EmailSent"));

            _logger.LogInformation("Email sent successfully to {To}", request.To);
            return true;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Error sending email");
            return false;
        }
    }
}
