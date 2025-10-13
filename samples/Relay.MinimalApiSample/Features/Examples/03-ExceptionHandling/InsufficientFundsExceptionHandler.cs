using Relay.Core.Pipeline.Interfaces;

namespace Relay.MinimalApiSample.Features.Examples.ExceptionHandling;

/// <summary>
/// Exception Handler: Can suppress exceptions and return fallback responses
/// </summary>
public class InsufficientFundsExceptionHandler
    : IRequestExceptionHandler<ProcessPaymentRequest, PaymentResult, InsufficientFundsException>
{
    private readonly ILogger<InsufficientFundsExceptionHandler> _logger;

    public InsufficientFundsExceptionHandler(ILogger<InsufficientFundsExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<ExceptionHandlerResult<PaymentResult>> HandleAsync(
        ProcessPaymentRequest request,
        InsufficientFundsException exception,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Handling insufficient funds for account {AccountId}: Requested {Requested}, Available {Available}",
            exception.AccountId,
            exception.RequestedAmount,
            exception.AvailableBalance);

        // Return graceful fallback response instead of throwing
        var fallbackResult = new PaymentResult(
            Success: false,
            Message: $"Payment declined: Insufficient funds. Available balance: {exception.AvailableBalance:C}"
        );

        return ValueTask.FromResult(
            ExceptionHandlerResult<PaymentResult>.Handle(fallbackResult));
    }
}
