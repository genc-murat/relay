using Relay.Core.Contracts.Handlers;
using Relay.ControllerApiSample.Infrastructure;

namespace Relay.ControllerApiSample.Features.Products;

public class GetAllProductsHandler : IRequestHandler<GetAllProductsRequest, GetAllProductsResponse>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<GetAllProductsHandler> _logger;

    public GetAllProductsHandler(InMemoryDatabase database, ILogger<GetAllProductsHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<GetAllProductsResponse> HandleAsync(GetAllProductsRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all products");

        var products = _database.Products.Values
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock))
            .ToList();

        _logger.LogInformation("Retrieved {Count} products", products.Count);

        var response = new GetAllProductsResponse(products);
        return ValueTask.FromResult(response);
    }
}
