using Relay.MessageBroker.Saga;
using SagaPatternSample.Services;

namespace SagaPatternSample.Sagas.Steps;

public class ProcessPaymentStep : SagaStep<OrderSagaData>
{
    private readonly PaymentService _paymentService;
    private readonly ILogger<ProcessPaymentStep> _logger;

    public ProcessPaymentStep(
        PaymentService paymentService,
        ILogger<ProcessPaymentStep> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    public override string Name => "ProcessPayment";

    public override async ValueTask ExecuteAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing payment for order {OrderId}, amount: {Amount}", 
            data.OrderId, data.TotalAmount);

        var transactionId = await _paymentService.ProcessPaymentAsync(
            data.CustomerId,
            data.TotalAmount,
            cancellationToken);

        data.PaymentTransactionId = transactionId;

        _logger.LogInformation("Payment processed with transaction ID {TransactionId}", transactionId);
    }

    public override async ValueTask CompensateAsync(OrderSagaData data, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(data.PaymentTransactionId))
        {
            _logger.LogWarning("Refunding payment {TransactionId}", data.PaymentTransactionId);
            await _paymentService.RefundAsync(data.PaymentTransactionId, cancellationToken);
            _logger.LogInformation("Payment refunded");
        }
    }
}
