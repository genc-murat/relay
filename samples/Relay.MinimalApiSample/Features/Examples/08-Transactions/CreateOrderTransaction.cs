using Relay.Core.Contracts.Requests;
using Relay.Core.Transactions;
using System.Data;

namespace Relay.MinimalApiSample.Features.Examples.Transactions;

/// <summary>
/// Transaction example: All operations succeed together or fail together
/// </summary>
/// <remarks>
/// BREAKING CHANGE: TransactionAttribute with explicit IsolationLevel is now REQUIRED.
/// The new transaction system requires explicit configuration for all transactional requests.
/// </remarks>
[Transaction(IsolationLevel.ReadCommitted, TimeoutSeconds = 30)]
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
