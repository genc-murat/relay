using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a file path has an allowed extension.
    /// </summary>
    public class FileExtensionValidationRule : IValidationRule<string>
    {
        private readonly HashSet<string> _allowedExtensions;
        private readonly string _errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileExtensionValidationRule"/> class.
        /// </summary>
        /// <param name="allowedExtensions">The allowed file extensions (without dots, case-insensitive).</param>
        /// <param name="errorMessage">The error message to return when validation fails.</param>
        public FileExtensionValidationRule(IEnumerable<string> allowedExtensions, string errorMessage = null)
        {
            _allowedExtensions = new HashSet<string>(
                allowedExtensions.Select(ext => ext.TrimStart('.').ToLowerInvariant()),
                StringComparer.OrdinalIgnoreCase);
            _errorMessage = errorMessage ?? $"File extension must be one of: {string.Join(", ", _allowedExtensions)}.";
        }

        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request))
            {
                return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
            }

            string extension = Path.GetExtension(request)?.TrimStart('.').ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
            {
                return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}