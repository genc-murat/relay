using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core
{
    /// <summary>
    /// Provides validation methods for Relay attributes.
    /// </summary>
    public static class AttributeValidation
    {
        /// <summary>
        /// Valid HTTP methods for endpoint generation.
        /// </summary>
        public static readonly HashSet<string> ValidHttpMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"
        };

        /// <summary>
        /// Validates the ExposeAsEndpoint attribute parameters.
        /// </summary>
        /// <param name="attribute">The attribute to validate.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        public static IEnumerable<string> ValidateExposeAsEndpointAttribute(ExposeAsEndpointAttribute attribute)
        {
            if (attribute == null)
                throw new ArgumentNullException(nameof(attribute));

            var errors = new List<string>();

            // Validate HTTP method
            if (string.IsNullOrWhiteSpace(attribute.HttpMethod))
            {
                errors.Add("HttpMethod cannot be null or empty.");
            }
            else if (!ValidHttpMethods.Contains(attribute.HttpMethod))
            {
                errors.Add($"HttpMethod '{attribute.HttpMethod}' is not valid. Valid methods are: {string.Join(", ", ValidHttpMethods)}.");
            }

            // Validate route template
            if (!string.IsNullOrWhiteSpace(attribute.Route))
            {
                if (attribute.Route.Contains("//"))
                {
                    errors.Add("Route cannot contain consecutive forward slashes (//).");
                }

                if (attribute.Route.EndsWith("/") && attribute.Route.Length > 1)
                {
                    errors.Add("Route should not end with a trailing slash unless it's the root route.");
                }

                // Check for invalid characters in route
                var invalidChars = new[] { ' ', '\t', '\n', '\r' };
                if (attribute.Route.Any(c => invalidChars.Contains(c)))
                {
                    errors.Add("Route cannot contain whitespace characters.");
                }
            }

            // Validate version
            if (!string.IsNullOrEmpty(attribute.Version))
            {
                if (string.IsNullOrWhiteSpace(attribute.Version))
                {
                    errors.Add("Version cannot be whitespace only.");
                }

                // Basic version format validation (allows v1, 1.0, 2024-01-01, etc.)
                if (attribute.Version.Any(c => char.IsControl(c)))
                {
                    errors.Add("Version cannot contain control characters.");
                }
            }

            return errors;
        }

        /// <summary>
        /// Validates that a priority value is within acceptable bounds.
        /// </summary>
        /// <param name="priority">The priority value to validate.</param>
        /// <param name="parameterName">The name of the parameter for error messages.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        public static IEnumerable<string> ValidatePriority(int priority, string parameterName = "Priority")
        {
            var errors = new List<string>();

            // Allow reasonable priority range
            if (priority < -10000 || priority > 10000)
            {
                errors.Add($"{parameterName} must be between -10000 and 10000.");
            }

            return errors;
        }

        /// <summary>
        /// Validates that a pipeline order value is within acceptable bounds.
        /// </summary>
        /// <param name="order">The order value to validate.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        public static IEnumerable<string> ValidatePipelineOrder(int order)
        {
            var errors = new List<string>();

            // Allow reasonable order range, with negative values for system modules
            if (order < -100000 || order > 100000)
            {
                errors.Add("Pipeline order must be between -100000 and 100000.");
            }

            return errors;
        }

        /// <summary>
        /// Validates that a handler name is valid.
        /// </summary>
        /// <param name="name">The handler name to validate.</param>
        /// <returns>A collection of validation errors, empty if valid.</returns>
        public static IEnumerable<string> ValidateHandlerName(string? name)
        {
            var errors = new List<string>();

            if (!string.IsNullOrEmpty(name))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errors.Add("Handler name cannot be whitespace only.");
                }

                if (name.Length > 200)
                {
                    errors.Add("Handler name cannot exceed 200 characters.");
                }

                // Check for invalid characters
                if (name.Any(c => char.IsControl(c)))
                {
                    errors.Add("Handler name cannot contain control characters.");
                }
            }

            return errors;
        }
    }
}