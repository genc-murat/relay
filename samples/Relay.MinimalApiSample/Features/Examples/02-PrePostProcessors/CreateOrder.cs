using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.PrePostProcessors;

/// <summary>
/// Example: Order creation with pre/post processors
/// Demonstrates request pre-processing and response post-processing
/// </summary>
public record CreateOrderRequest(
    int CustomerId,
    string[] Items,
    decimal TotalAmount
) : IRequest<CreateOrderResponse>;

public record CreateOrderResponse(
    Guid OrderId,
    int CustomerId,
    decimal TotalAmount,
    DateTime CreatedAt,
    string Status
);
