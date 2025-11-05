using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Validation.Builder;

/// <summary>
/// Adapter to use dedicated validation rules with property validation.
/// </summary>
internal class PropertyValidationRuleAdapter<TRequest, TProperty> : IValidationRuleConfiguration<TRequest>
{
    private readonly string _propertyName;
    private readonly Func<TRequest, TProperty> _propertyFunc;
    private readonly IValidationRule<string> _validationRule;
    private readonly string _errorMessage;
    private readonly Func<TRequest, bool>? _condition;

    public PropertyValidationRuleAdapter(
        string propertyName,
        Func<TRequest, TProperty> propertyFunc,
        IValidationRule<string> validationRule,
        string errorMessage,
        Func<TRequest, bool>? condition = null)
    {
        _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _propertyFunc = propertyFunc ?? throw new ArgumentNullException(nameof(propertyFunc));
        _validationRule = validationRule ?? throw new ArgumentNullException(nameof(validationRule));
        _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        _condition = condition;
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return new[] { "Request cannot be null." };
        }

        // Check condition first
        if (_condition != null && !_condition(request))
        {
            return Array.Empty<string>();
        }

        var value = _propertyFunc(request);

        // For nullable types, only validate if not null
        if (value == null && typeof(TProperty).IsClass)
        {
            return Array.Empty<string>();
        }

        var stringValue = value?.ToString();
        if (stringValue == null)
        {
            return Array.Empty<string>();
        }

        var errors = await _validationRule.ValidateAsync(stringValue, cancellationToken);
        if (errors.Any())
        {
            return new[] { _errorMessage };
        }

        return Array.Empty<string>();
    }
}
