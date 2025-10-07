using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Attributes;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation
{
    /// <summary>
    /// Default implementation of IValidator that executes validation rules.
    /// </summary>
    /// <typeparam name="TRequest">The type of request to validate.</typeparam>
    public class DefaultValidator<TRequest> : IValidator<TRequest>
    {
        private readonly IEnumerable<IValidationRule<TRequest>> _validationRules;

        /// <summary>
        /// Initializes a new instance of the DefaultValidator class.
        /// </summary>
        /// <param name="validationRules">The validation rules to execute.</param>
        public DefaultValidator(IEnumerable<IValidationRule<TRequest>> validationRules)
        {
            _validationRules = validationRules ?? throw new ArgumentNullException(nameof(validationRules));
        }

        /// <inheritdoc />
        public async ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var errors = new List<string>();

            // If no validation rules are registered, the request is considered valid
            if (!_validationRules.Any())
                return errors;

            // Execute validation rules in order
            foreach (var rule in _validationRules.OrderBy(r =>
            {
                var attr = r.GetType().GetCustomAttributes(typeof(ValidationRuleAttribute), false)
                    .OfType<ValidationRuleAttribute>()
                    .FirstOrDefault();
                return attr?.Order ?? 0;
            }))
            {
                try
                {
                    var ruleErrors = await rule.ValidateAsync(request, cancellationToken);
                    errors.AddRange(ruleErrors);

                    // If this rule doesn't allow continuing on error and we have errors, stop validation
                    var ruleAttribute = rule.GetType().GetCustomAttributes(typeof(ValidationRuleAttribute), false)
                        .OfType<ValidationRuleAttribute>()
                        .FirstOrDefault();

                    if (ruleAttribute != null && !ruleAttribute.ContinueOnError && errors.Any())
                        break;
                }
                catch (Exception ex)
                {
                    // Add error for exception and stop validation unless the rule allows continuing on error
                    errors.Add($"Validation rule '{rule.GetType().Name}' threw an exception: {ex.Message}");

                    var ruleAttribute = rule.GetType().GetCustomAttributes(typeof(ValidationRuleAttribute), false)
                        .OfType<ValidationRuleAttribute>()
                        .FirstOrDefault();

                    if (ruleAttribute == null || !ruleAttribute.ContinueOnError)
                        break;
                }
            }

            return errors;
        }
    }
}
