using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.ExceptionHandling;

public class ProcessPaymentHandler : IRequestHandler<ProcessPaymentRequest, PaymentResult>
{
    private readonly ILogger<ProcessPaymentHandler> _logger;

    public ProcessPaymentHandler(ILogger<ProcessPaymentHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<PaymentResult> HandleAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Processing payment of {Amount} for account {AccountId}",
            request.Amount,
            request.AccountId);

        // Simulate balance check
        decimal availableBalance = 100m;

        if (request.Amount > availableBalance)
        {
            _logger.LogWarning(
                "Insufficient funds for account {AccountId}",
                request.AccountId);

            throw new InsufficientFundsException(
                request.AccountId,
                request.Amount,
                availableBalance);
        }

        // Process payment
        var transactionId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Payment processed successfully. Transaction: {TransactionId}",
            transactionId);

        var result = new PaymentResult(
            Success: true,
            Message: "Payment processed successfully",
            TransactionId: transactionId
        );

        return ValueTask.FromResult(result);
    }
}
