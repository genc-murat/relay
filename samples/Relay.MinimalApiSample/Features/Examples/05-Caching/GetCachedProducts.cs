using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.Caching;

public record GetCachedProductsRequest : IRequest<List<CachedProduct>>;

public record CachedProduct(
    Guid Id,
    string Name,
    decimal Price,
    DateTime CachedAt
);
