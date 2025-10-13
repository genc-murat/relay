using Relay.Core.Contracts.Requests;

namespace Relay.ControllerApiSample.Features.Products;

// Request
public record GetProductRequest(Guid Id) : IRequest<GetProductResponse?>;

// Response
public record GetProductResponse(Guid Id, string Name, string Description, decimal Price, int Stock);
