using Relay.Core.Contracts.Handlers;

namespace Relay.MinimalApiSample.Features.Examples.Caching;

public class GetCachedProductsHandler : IRequestHandler<GetCachedProductsRequest, List<CachedProduct>>
{
    private readonly ILogger<GetCachedProductsHandler> _logger;

    public GetCachedProductsHandler(ILogger<GetCachedProductsHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<List<CachedProduct>> HandleAsync(
        GetCachedProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching products from database (slow operation)...");

        // Simulate slow database call
        await Task.Delay(1000, cancellationToken);

        var products = new List<CachedProduct>
        {
            new(Guid.NewGuid(), "Laptop", 1299.99m, DateTime.UtcNow),
            new(Guid.NewGuid(), "Mouse", 29.99m, DateTime.UtcNow),
            new(Guid.NewGuid(), "Keyboard", 89.99m, DateTime.UtcNow)
        };

        _logger.LogInformation("Products fetched: {Count} items", products.Count);

        return products;
    }
}
