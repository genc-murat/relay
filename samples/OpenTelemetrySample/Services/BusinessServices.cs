using OpenTelemetrySample.Commands;
using System.Diagnostics;

namespace OpenTelemetrySample.Services;

public class InventoryService
{
    private readonly ILogger<InventoryService> _logger;
    private static readonly ActivitySource ActivitySource = new("Relay.InventoryService");
    private readonly Dictionary<string, int> _inventory = new()
    {
        { "PROD-001", 100 },
        { "PROD-002", 50 },
        { "PROD-003", 25 }
    };

    public InventoryService(ILogger<InventoryService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> CheckAvailabilityAsync(string productId, int quantity)
    {
        using var activity = ActivitySource.StartActivity("CheckInventory", ActivityKind.Internal);
        activity?.SetTag("product.id", productId);
        activity?.SetTag("quantity.requested", quantity);

        try
        {
            await Task.Delay(50); // Simulate database query

            var available = _inventory.TryGetValue(productId, out var stock) && stock >= quantity;
            
            activity?.SetTag("quantity.available", stock);
            activity?.SetTag("is.available", available);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Inventory check for {ProductId}: {Available} (Stock: {Stock})", 
                productId, available, stock);

            return available;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    public async Task ReserveAsync(string productId, int quantity)
    {
        using var activity = ActivitySource.StartActivity("ReserveInventory", ActivityKind.Internal);
        activity?.SetTag("product.id", productId);
        activity?.SetTag("quantity", quantity);

        await Task.Delay(30);

        if (_inventory.ContainsKey(productId))
        {
            _inventory[productId] -= quantity;
            activity?.AddEvent(new ActivityEvent("InventoryReserved"));
        }

        activity?.SetStatus(ActivityStatusCode.Ok);
    }
}

public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly HttpClient _httpClient;
    private static readonly ActivitySource ActivitySource = new("Relay.PaymentService");

    public PaymentService(ILogger<PaymentService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("PaymentGateway");
    }

    public async Task<PaymentResponse> ProcessAsync(decimal amount, string paymentMethod)
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment", ActivityKind.Client);
        activity?.SetTag("payment.amount", amount);
        activity?.SetTag("payment.method", paymentMethod);
        activity?.SetTag("payment.gateway", "StripeSimulator");

        try
        {
            // Simulate payment gateway call
            await Task.Delay(200);

            // Simulate occasional failures
            var success = Random.Shared.Next(100) > 10; // 90% success rate

            var transactionId = Guid.NewGuid().ToString("N");
            
            if (success)
            {
                activity?.SetTag("transaction.id", transactionId);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.AddEvent(new ActivityEvent("PaymentApproved"));

                _logger.LogInformation("Payment approved. Transaction: {TransactionId}", transactionId);

                return new PaymentResponse(transactionId, true, "Payment successful");
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Payment declined");
                activity?.AddEvent(new ActivityEvent("PaymentDeclined"));

                _logger.LogWarning("Payment declined");

                return new PaymentResponse("", false, "Payment declined by gateway");
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            _logger.LogError(ex, "Payment processing error");
            throw;
        }
    }
}

public class EmailService
{
    private readonly ILogger<EmailService> _logger;
    private static readonly ActivitySource ActivitySource = new("Relay.EmailService");

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body)
    {
        using var activity = ActivitySource.StartActivity("SendEmail", ActivityKind.Producer);
        activity?.SetTag("email.to", to);
        activity?.SetTag("email.subject", subject);
        activity?.SetTag("email.provider", "SMTP");

        try
        {
            // Simulate email sending
            await Task.Delay(100);

            activity?.AddEvent(new ActivityEvent("EmailQueued"));
            await Task.Delay(50);
            activity?.AddEvent(new ActivityEvent("EmailSent"));

            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation("Email sent to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
