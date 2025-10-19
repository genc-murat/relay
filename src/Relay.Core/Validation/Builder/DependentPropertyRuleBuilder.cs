using System;
using System.Collections.Generic;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Validation.Builder;

/// <summary>
/// Builder for dependent property validation rules.
/// </summary>
public class DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty>
{
    private readonly string _propertyName;
    private readonly Func<TRequest, TProperty> _propertyFunc;
    private readonly List<IValidationRuleConfiguration<TRequest>> _rules;
    private readonly string _dependentPropertyName;
    private readonly Func<TRequest, TDependentProperty> _dependentPropertyFunc;
    private readonly Func<TDependentProperty, bool> _condition;

    internal DependentPropertyRuleBuilder(
        string propertyName,
        Func<TRequest, TProperty> propertyFunc,
        List<IValidationRuleConfiguration<TRequest>> rules,
        string dependentPropertyName,
        Func<TRequest, TDependentProperty> dependentPropertyFunc,
        Func<TDependentProperty, bool> condition)
    {
        _propertyName = propertyName;
        _propertyFunc = propertyFunc;
        _rules = rules;
        _dependentPropertyName = dependentPropertyName;
        _dependentPropertyFunc = dependentPropertyFunc;
        _condition = condition;
    }

    private Func<TRequest, bool> GetEffectiveCondition()
    {
        return request =>
        {
            var dependentValue = _dependentPropertyFunc(request);
            return _condition(dependentValue);
        };
    }

    /// <summary>
    /// Ensures the property is not null.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> NotNull(string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value != null,
            errorMessage ?? $"{_propertyName} must not be null.",
            GetEffectiveCondition()));
        return this;
    }

    /// <summary>
    /// Ensures the string property is not empty.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> NotEmpty(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && !string.IsNullOrWhiteSpace(value.ToString()),
                errorMessage ?? $"{_propertyName} must not be empty.",
                GetEffectiveCondition()));
        }
        return this;
    }

    /// <summary>
    /// Ensures the property meets a custom condition.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> Must(Func<TProperty?, bool> predicate, string errorMessage)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            predicate,
            errorMessage,
            GetEffectiveCondition()));
        return this;
    }

    /// <summary>
    /// Ensures the email format is valid.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> EmailAddress(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new EmailValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid email address.",
                GetEffectiveCondition()));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish ID number.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> TurkishId(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new TurkishIdValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid Turkish ID number.",
                GetEffectiveCondition()));
        }
        return this;
    }
}
