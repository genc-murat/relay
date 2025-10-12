using Relay.Core.Validation.Interfaces;

namespace Relay.MinimalApiSample.Features.Users;

public class CreateUserValidator : IValidationRule<CreateUserRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name is required");
        }
        else if (request.Name.Length < 2)
        {
            errors.Add("Name must be at least 2 characters long");
        }
        else if (request.Name.Length > 100)
        {
            errors.Add("Name must not exceed 100 characters");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required");
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add("Email must be a valid email address");
        }

        return ValueTask.FromResult<IEnumerable<string>>(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
