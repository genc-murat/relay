using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.ExceptionHandling;

public record ProcessPaymentRequest(
    int AccountId,
    decimal Amount
) : IRequest<PaymentResult>;

public record PaymentResult(
    bool Success,
    string Message,
    string? TransactionId = null
);

public class InsufficientFundsException : Exception
{
    public int AccountId { get; }
    public decimal RequestedAmount { get; }
    public decimal AvailableBalance { get; }

    public InsufficientFundsException(int accountId, decimal requested, decimal available)
        : base($"Insufficient funds. Requested: {requested}, Available: {available}")
    {
        AccountId = accountId;
        RequestedAmount = requested;
        AvailableBalance = available;
    }
}
