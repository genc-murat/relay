using Relay.Core.Contracts.Requests;

namespace Relay.ControllerApiSample.Features.Products;

// Request
public record CreateProductRequest(string Name, string Description, decimal Price, int Stock)
    : IRequest<CreateProductResponse>;

// Response
public record CreateProductResponse(Guid Id, string Name, decimal Price, int Stock);
