using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid ISBN-10 or ISBN-13.
    /// </summary>
    public class IsbnValidationRule : IValidationRule<string>
    {
        private static readonly Regex Isbn10Regex = new Regex(
            @"^(?:\d{9}[\dX])$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Isbn13Regex = new Regex(
            @"^(?:\d{13})$",
            RegexOptions.Compiled);

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            // Remove hyphens and spaces for validation
            var cleanIsbn = request.Replace("-", "").Replace(" ", "");

            if (IsValidIsbn10(cleanIsbn) || IsValidIsbn13(cleanIsbn))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { "Invalid ISBN format." });
        }

        private static bool IsValidIsbn10(string isbn)
        {
            if (!Isbn10Regex.IsMatch(isbn))
            {
                return false;
            }

            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                if (!int.TryParse(isbn[i].ToString(), out var digit))
                {
                    return false;
                }
                sum += digit * (10 - i);
            }

            var checkDigit = isbn[9];
            var expectedCheckDigit = (11 - (sum % 11)) % 11;

            if (expectedCheckDigit == 10)
            {
                return checkDigit == 'X' || checkDigit == 'x';
            }

            return checkDigit.ToString() == expectedCheckDigit.ToString();
        }

        private static bool IsValidIsbn13(string isbn)
        {
            if (!Isbn13Regex.IsMatch(isbn))
            {
                return false;
            }

            var sum = 0;
            for (var i = 0; i < 12; i++)
            {
                if (!int.TryParse(isbn[i].ToString(), out var digit))
                {
                    return false;
                }
                sum += digit * (i % 2 == 0 ? 1 : 3);
            }

            var checkDigit = (10 - (sum % 10)) % 10;
            return isbn[12].ToString() == checkDigit.ToString();
        }
    }
}