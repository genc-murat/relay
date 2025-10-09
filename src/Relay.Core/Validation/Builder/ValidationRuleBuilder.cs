using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Validation.Builder;

/// <summary>
/// Fluent builder for creating validation rules.
/// </summary>
/// <typeparam name="TRequest">The type of request to validate.</typeparam>
public class ValidationRuleBuilder<TRequest>
{
    private readonly List<IValidationRuleConfiguration<TRequest>> _rules = new();

    /// <summary>
    /// Adds a rule for a property.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> RuleFor<TProperty>(Expression<Func<TRequest, TProperty>> propertyExpression)
    {
        var propertyName = GetPropertyName(propertyExpression);
        var propertyFunc = propertyExpression.Compile();

        return new PropertyRuleBuilder<TRequest, TProperty>(propertyName, propertyFunc, _rules);
    }

    /// <summary>
    /// Builds the validation rules.
    /// </summary>
    public IEnumerable<IValidationRuleConfiguration<TRequest>> Build()
    {
        return _rules;
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is UnaryExpression unaryExpression &&
            unaryExpression.Operand is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        throw new ArgumentException("Invalid property expression", nameof(expression));
    }
}

/// <summary>
/// Builder for property-specific validation rules.
/// </summary>
public class PropertyRuleBuilder<TRequest, TProperty>
{
    private readonly string _propertyName;
    private readonly Func<TRequest, TProperty> _propertyFunc;
    private readonly List<IValidationRuleConfiguration<TRequest>> _rules;

    internal PropertyRuleBuilder(
        string propertyName,
        Func<TRequest, TProperty> propertyFunc,
        List<IValidationRuleConfiguration<TRequest>> rules)
    {
        _propertyName = propertyName;
        _propertyFunc = propertyFunc;
        _rules = rules;
    }

    /// <summary>
    /// Ensures the property is not null.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NotNull(string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value != null,
            errorMessage ?? $"{_propertyName} must not be null."));
        return this;
    }

    /// <summary>
    /// Ensures the string property is not empty.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NotEmpty(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && !string.IsNullOrWhiteSpace(value.ToString()),
                errorMessage ?? $"{_propertyName} must not be empty."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the property meets a custom condition.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Must(Func<TProperty, bool> predicate, string errorMessage)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            predicate,
            errorMessage));
        return this;
    }

    /// <summary>
    /// Ensures the string has a minimum length.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> MinLength(int minLength, string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value?.ToString()?.Length >= minLength,
                errorMessage ?? $"{_propertyName} must be at least {minLength} characters long."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string has a maximum length.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> MaxLength(int maxLength, string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value?.ToString()?.Length <= maxLength,
                errorMessage ?? $"{_propertyName} must not exceed {maxLength} characters."));
        }
        return this;
    }

    /// <summary>
    /// Ensures a numeric value is greater than a minimum.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> GreaterThan(IComparable min, string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value is IComparable comparable && comparable.CompareTo(min) > 0,
            errorMessage ?? $"{_propertyName} must be greater than {min}."));
        return this;
    }

    /// <summary>
    /// Ensures a numeric value is less than a maximum.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> LessThan(IComparable max, string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value is IComparable comparable && comparable.CompareTo(max) < 0,
            errorMessage ?? $"{_propertyName} must be less than {max}."));
        return this;
    }

    /// <summary>
    /// Ensures the value matches a regular expression pattern.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Matches(string pattern, string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && System.Text.RegularExpressions.Regex.IsMatch(value.ToString()!, pattern),
                errorMessage ?? $"{_propertyName} format is invalid."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the email format is valid.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> EmailAddress(string? errorMessage = null)
    {
        const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Matches(emailPattern, errorMessage ?? $"{_propertyName} must be a valid email address.");
    }
}
