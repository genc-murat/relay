using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Products;

// Request
public record GetAllProductsRequest : IRequest<GetAllProductsResponse>;

// Response
public record GetAllProductsResponse(List<ProductDto> Products);

public record ProductDto(Guid Id, string Name, decimal Price, int Stock);
