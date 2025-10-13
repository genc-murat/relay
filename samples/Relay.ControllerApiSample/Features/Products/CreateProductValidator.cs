using Relay.Core.Validation.Interfaces;

namespace Relay.ControllerApiSample.Features.Products;

public class CreateProductValidator : IValidationRule<CreateProductRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Product name is required");
        }
        else if (request.Name.Length < 2)
        {
            errors.Add("Product name must be at least 2 characters long");
        }
        else if (request.Name.Length > 200)
        {
            errors.Add("Product name must not exceed 200 characters");
        }

        if (request.Price <= 0)
        {
            errors.Add("Price must be greater than 0");
        }

        if (request.Stock < 0)
        {
            errors.Add("Stock cannot be negative");
        }

        return ValueTask.FromResult<IEnumerable<string>>(errors);
    }
}
