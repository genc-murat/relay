using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if an integer represents a valid network port.
    /// </summary>
    public class PortValidationRule : IValidationRule<int>
    {
        private readonly bool _allowSystemPorts;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortValidationRule"/> class.
        /// </summary>
        /// <param name="allowSystemPorts">Whether to allow system ports (0-1023). Default is true.</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public PortValidationRule(bool allowSystemPorts = true, string? errorMessage = null)
        {
            _allowSystemPorts = allowSystemPorts;
            _errorMessage = errorMessage ?? "Invalid network port.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(int request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var errors = new List<string>();

            if (request < 0 || request > 65535)
            {
                errors.Add("Port number must be between 0 and 65535.");
            }
            else if (!_allowSystemPorts && request < 1024)
            {
                errors.Add("Port number must be 1024 or greater.");
            }

            if (errors.Count > 0)
            {
                if (!string.IsNullOrEmpty(_errorMessage) && _errorMessage != "Invalid network port.")
                {
                    return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
                }
            }

            return new ValueTask<IEnumerable<string>>(errors);
        }
    }
}