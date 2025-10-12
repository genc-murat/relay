using Relay.Core.Validation.Interfaces;

namespace Relay.MinimalApiSample.Features.Products;

public class CreateProductValidator : IValidationRule<CreateProductRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Product name is required");
        }
        else if (request.Name.Length < 3)
        {
            errors.Add("Product name must be at least 3 characters long");
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors.Add("Description is required");
        }

        if (request.Price <= 0)
        {
            errors.Add("Price must be greater than zero");
        }

        if (request.Stock < 0)
        {
            errors.Add("Stock cannot be negative");
        }

        return ValueTask.FromResult<IEnumerable<string>>(errors);
    }
}
