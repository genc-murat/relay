using Microsoft.AspNetCore.Mvc;
using Relay.Core.Contracts.Core;
using Relay.ControllerApiSample.Features.Products;

namespace Relay.ControllerApiSample.Controllers;

/// <summary>
/// Products controller demonstrating Relay framework usage with ASP.NET Core controllers
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IRelay _relay;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IRelay relay, ILogger<ProductsController> logger)
    {
        _relay = relay;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    /// <returns>List of all products</returns>
    [HttpGet]
    [ProducesResponseType(typeof(GetAllProductsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<GetAllProductsResponse>> GetAllProducts(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET api/products - Retrieving all products");

        var request = new GetAllProductsRequest();
        var response = await _relay.SendAsync(request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Product details if found, NotFound otherwise</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GetProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetProductResponse>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET api/products/{Id} - Retrieving product", id);

        var request = new GetProductRequest(id);
        var response = await _relay.SendAsync(request, cancellationToken);

        if (response == null)
        {
            _logger.LogWarning("Product with ID {Id} not found", id);
            return NotFound();
        }

        return Ok(response);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created product details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST api/products - Creating new product: {Name}", request.Name);

        var response = await _relay.SendAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = response.Id },
            response);
    }
}
