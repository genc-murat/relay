using System;
using System.Collections.Generic;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Validation.Builder;

/// <summary>
/// Builder for conditional property validation rules.
/// </summary>
public class ConditionalPropertyRuleBuilder<TRequest, TProperty>
{
    private readonly string _propertyName;
    private readonly Func<TRequest, TProperty> _propertyFunc;
    private readonly List<IValidationRuleConfiguration<TRequest>> _rules;
    private readonly Func<TRequest, bool> _condition;
    private readonly bool _whenCondition;

    internal ConditionalPropertyRuleBuilder(
        string propertyName,
        Func<TRequest, TProperty> propertyFunc,
        List<IValidationRuleConfiguration<TRequest>> rules,
        Func<TRequest, bool> condition,
        bool whenCondition)
    {
        _propertyName = propertyName;
        _propertyFunc = propertyFunc;
        _rules = rules;
        _condition = condition;
        _whenCondition = whenCondition;
    }

    private Func<TRequest, bool> GetEffectiveCondition()
    {
        return _whenCondition ? _condition : (request) => !_condition(request);
    }

    /// <summary>
    /// Ensures the property is not null.
    /// </summary>
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> NotNull(string? errorMessage = null)
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
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> NotEmpty(string? errorMessage = null)
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
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> Must(Func<TProperty?, bool> predicate, string errorMessage)
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
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> EmailAddress(string? errorMessage = null)
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
    /// Ensures the string has a minimum length.
    /// </summary>
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> MinLength(int minLength, string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value == null || value?.ToString()?.Length >= minLength,
                errorMessage ?? $"{_propertyName} must be at least {minLength} characters long.",
                GetEffectiveCondition()));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string has a maximum length.
    /// </summary>
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> MaxLength(int maxLength, string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value == null || value?.ToString()?.Length <= maxLength,
                errorMessage ?? $"{_propertyName} must not exceed {maxLength} characters.",
                GetEffectiveCondition()));
        }
        return this;
    }
}
