using Relay.Core.Validation.Interfaces;
using System.Text.RegularExpressions;

namespace Relay.MinimalApiSample.Features.Examples.Validation;

/// <summary>
/// Validation rules for user registration
/// Demonstrates comprehensive validation
/// </summary>
public class RegisterUserValidator : IValidationRule<RegisterUserRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Username validation
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add("Username is required");
        }
        else if (request.Username.Length < 3)
        {
            errors.Add("Username must be at least 3 characters");
        }
        else if (request.Username.Length > 50)
        {
            errors.Add("Username must not exceed 50 characters");
        }
        else if (!Regex.IsMatch(request.Username, @"^[a-zA-Z0-9_]+$"))
        {
            errors.Add("Username can only contain letters, numbers, and underscores");
        }

        // Email validation
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required");
        }
        else if (!IsValidEmail(request.Email))
        {
            errors.Add("Email must be a valid email address");
        }

        // Password validation
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required");
        }
        else
        {
            if (request.Password.Length < 8)
            {
                errors.Add("Password must be at least 8 characters");
            }
            if (!Regex.IsMatch(request.Password, @"[A-Z]"))
            {
                errors.Add("Password must contain at least one uppercase letter");
            }
            if (!Regex.IsMatch(request.Password, @"[a-z]"))
            {
                errors.Add("Password must contain at least one lowercase letter");
            }
            if (!Regex.IsMatch(request.Password, @"[0-9]"))
            {
                errors.Add("Password must contain at least one number");
            }
            if (!Regex.IsMatch(request.Password, @"[!@#$%^&*(),.?""':{}|<>]"))
            {
                errors.Add("Password must contain at least one special character");
            }
        }

        // Age validation
        if (request.Age < 18)
        {
            errors.Add("User must be at least 18 years old");
        }
        else if (request.Age > 120)
        {
            errors.Add("Age must be a valid value");
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
