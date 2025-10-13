using Relay.Core.Contracts.Handlers;
using Relay.ControllerApiSample.Infrastructure;
using Relay.ControllerApiSample.Models;

namespace Relay.ControllerApiSample.Features.Products;

public class CreateProductHandler : IRequestHandler<CreateProductRequest, CreateProductResponse>
{
    private readonly InMemoryDatabase _database;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(InMemoryDatabase database, ILogger<CreateProductHandler> logger)
    {
        _database = database;
        _logger = logger;
    }

    public ValueTask<CreateProductResponse> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating product: {Name}", request.Name);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedAt = DateTime.UtcNow
        };

        _database.Products.TryAdd(product.Id, product);

        _logger.LogInformation("Product created with ID: {ProductId}", product.Id);

        var response = new CreateProductResponse(product.Id, product.Name, product.Price, product.Stock);
        return ValueTask.FromResult(response);
    }
}
