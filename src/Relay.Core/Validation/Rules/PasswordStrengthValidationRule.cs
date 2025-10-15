using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks password strength requirements.
    /// </summary>
    public class PasswordStrengthValidationRule : IValidationRule<string>
    {
        private readonly int _minLength;
        private readonly bool _requireUppercase;
        private readonly bool _requireLowercase;
        private readonly bool _requireDigit;
        private readonly bool _requireSpecialChar;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordStrengthValidationRule"/> class.
        /// </summary>
        /// <param name="minLength">The minimum password length.</param>
        /// <param name="requireUppercase">Whether uppercase letters are required.</param>
        /// <param name="requireLowercase">Whether lowercase letters are required.</param>
        /// <param name="requireDigit">Whether digits are required.</param>
        /// <param name="requireSpecialChar">Whether special characters are required.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public PasswordStrengthValidationRule(
            int minLength = 8,
            bool requireUppercase = true,
            bool requireLowercase = true,
            bool requireDigit = true,
            bool requireSpecialChar = true,
            string errorMessage = null)
        {
            _minLength = minLength;
            _requireUppercase = requireUppercase;
            _requireLowercase = requireLowercase;
            _requireDigit = requireDigit;
            _requireSpecialChar = requireSpecialChar;
            _errorMessage = errorMessage ?? BuildErrorMessage();
        }

        private string BuildErrorMessage()
        {
            var requirements = new List<string>();

            requirements.Add($"at least {_minLength} characters");

            if (_requireUppercase) requirements.Add("an uppercase letter");
            if (_requireLowercase) requirements.Add("a lowercase letter");
            if (_requireDigit) requirements.Add("a digit");
            if (_requireSpecialChar) requirements.Add("a special character");

            return $"Password must contain {string.Join(", ", requirements)}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            var errors = new List<string>();

            if (request.Length < _minLength)
            {
                errors.Add($"Password must be at least {_minLength} characters long.");
            }

            if (_requireUppercase && !request.Any(char.IsUpper))
            {
                errors.Add("Password must contain at least one uppercase letter.");
            }

            if (_requireLowercase && !request.Any(char.IsLower))
            {
                errors.Add("Password must contain at least one lowercase letter.");
            }

            if (_requireDigit && !request.Any(char.IsDigit))
            {
                errors.Add("Password must contain at least one digit.");
            }

            if (_requireSpecialChar && !Regex.IsMatch(request, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            {
                errors.Add("Password must contain at least one special character.");
            }

            if (errors.Any())
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}