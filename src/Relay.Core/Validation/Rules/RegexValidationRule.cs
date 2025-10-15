using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a string matches a regular expression pattern.
    /// </summary>
    public class RegexValidationRule : IValidationRule<string>
    {
        private readonly Regex _regex;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexValidationRule"/> class.
        /// </summary>
        /// <param name="pattern">The regular expression pattern to match.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        /// <param name="options">The regex options to use.</param>
        public RegexValidationRule(string pattern, string? errorMessage = null, RegexOptions options = RegexOptions.None)
        {
            _regex = new Regex(pattern, options | RegexOptions.Compiled);
            _errorMessage = errorMessage ?? "Value does not match the required format.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            if (!_regex.IsMatch(request))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}