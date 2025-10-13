using Relay.Core.Contracts.Requests;
using Relay.Core.Transactions;

namespace Relay.MinimalApiSample.Features.Examples.Transactions;

/// <summary>
/// Transaction example: All operations succeed together or fail together
/// </summary>
public record CreateOrderTransactionRequest(
    int CustomerId,
    int ProductId,
    int Quantity
) : IRequest<OrderTransactionResult>, ITransactionalRequest<OrderTransactionResult>;

public record OrderTransactionResult(
    Guid OrderId,
    string Status,
    string Message
);
