using Relay.Core.Contracts.Handlers;
using Relay.ControllerApiSample.Infrastructure;

namespace Relay.ControllerApiSample.Features.Products;

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
        _logger.LogInformation("Retrieving product with ID: {ProductId}", request.Id);

        if (_database.Products.TryGetValue(request.Id, out var product))
        {
            _logger.LogInformation("Product found: {ProductName}", product.Name);
            var response = new GetProductResponse(product.Id, product.Name, product.Description, product.Price, product.Stock);
            return ValueTask.FromResult<GetProductResponse?>(response);
        }

        _logger.LogWarning("Product not found with ID: {ProductId}", request.Id);
        return ValueTask.FromResult<GetProductResponse?>(null);
    }
}
