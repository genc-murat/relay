using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule for a specific property.
/// </summary>
public class PropertyValidationRule<TRequest, TProperty> : IValidationRuleConfiguration<TRequest>
{
    private readonly string _propertyName;
    private readonly Func<TRequest, TProperty> _propertyFunc;
    private readonly Func<TProperty?, bool> _predicate;
    private readonly string _errorMessage;
    private readonly Func<TRequest, bool>? _condition;

    public PropertyValidationRule(
        string propertyName,
        Func<TRequest, TProperty> propertyFunc,
        Func<TProperty?, bool> predicate,
        string errorMessage,
        Func<TRequest, bool>? condition = null)
    {
        _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        _propertyFunc = propertyFunc ?? throw new ArgumentNullException(nameof(propertyFunc));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
        _condition = condition;
    }

    public ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            return new ValueTask<IEnumerable<string>>(new[] { "Request cannot be null." });
        }

        // Check condition first
        if (_condition != null && !_condition(request))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        var value = _propertyFunc(request);

        if (!_predicate(value))
        {
            return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
        }

        return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
    }
}
