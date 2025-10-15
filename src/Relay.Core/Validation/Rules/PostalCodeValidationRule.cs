using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string is a valid postal code.
    /// Supports common formats like US ZIP codes, Canadian postal codes, UK postcodes, etc.
    /// </summary>
    public class PostalCodeValidationRule : IValidationRule<string>
    {
        private readonly Regex _postalCodeRegex;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostalCodeValidationRule"/> class.
        /// </summary>
        /// <param name="country">The country code to determine the postal code format. Defaults to "US".</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public PostalCodeValidationRule(string country = "US", string? errorMessage = null)
        {
            string pattern = GetPostalCodePattern(country);
            _postalCodeRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            _errorMessage = errorMessage ?? $"Invalid postal code format for {country}.";
        }

        private static string GetPostalCodePattern(string country)
        {
            return country.ToUpperInvariant() switch
            {
                "US" => @"^\d{5}(-\d{4})?$", // US ZIP code
                "CA" => @"^[A-Za-z]\d[A-Za-z] ?\d[A-Za-z]\d$", // Canadian postal code
                "UK" => @"^[A-Za-z]{1,2}\d[A-Za-z\d]? ?\d[A-Za-z]{2}$", // UK postcode
                "DE" => @"^\d{5}$", // German PLZ
                "FR" => @"^\d{5}$", // French postal code
                "AU" => @"^\d{4}$", // Australian postcode
                _ => @"^\d{5}(-\d{4})?$" // Default to US
            };
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (_postalCodeRegex.IsMatch(request.Trim()))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
        }
    }
}