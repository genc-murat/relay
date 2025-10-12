using Relay.Core.Contracts.Handlers;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Features.Products;

public class GetProductHandler : IRequestHandler<GetProductRequest, GetProductResponse?>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<GetProductHandler> _logger;

    public GetProductHandler(InMemoryDatabase database, ILogger<GetProductHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<GetProductResponse?> HandleAsync(GetProductRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", request.Id);

        if (_database.Products.TryGetValue(request.Id, out var product))
        {
            var response = new GetProductResponse(
                product.Id,
                product.Name,
                product.Description,
                product.Price,
                product.Stock);
            return ValueTask.FromResult<GetProductResponse?>(response);
        }

        _logger.LogWarning("Product with ID: {ProductId} not found", request.Id);
        return ValueTask.FromResult<GetProductResponse?>(null);
    }
}
