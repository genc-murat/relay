using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Helpers;
using Relay.Core.Validation.Interfaces;
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
    /// Adds a custom validation rule.
    /// </summary>
    public ValidationRuleBuilder<TRequest> Custom(Func<TRequest, CancellationToken, ValueTask<IEnumerable<string>>> validationFunc)
    {
        _rules.Add(new CustomValidationRule<TRequest>(validationFunc));
        return this;
    }

    /// <summary>
    /// Adds a registered custom validation rule by name.
    /// </summary>
    public ValidationRuleBuilder<TRequest> Custom(string ruleName, CustomValidationRuleRegistry registry)
    {
        var ruleFunc = registry.GetRule(ruleName);
        if (ruleFunc == null)
        {
            throw new ArgumentException($"Custom validation rule '{ruleName}' is not registered", nameof(ruleName));
        }

        _rules.Add(new CustomValidationRule<TRequest>((request, ct) => ruleFunc(request, ct)));
        return this;
    }

    /// <summary>
    /// Adds a custom validation rule instance.
    /// </summary>
    public ValidationRuleBuilder<TRequest> Custom(IValidationRuleConfiguration<TRequest> customRule)
    {
        _rules.Add(customRule);
        return this;
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

    /// <summary>
    /// Ensures the value is a valid Turkish ID number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishId(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishId(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish ID number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish foreigner ID number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishForeignerId(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishForeignerId(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish foreigner ID number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish phone number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishPhone(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishPhone(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish phone number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish postal code.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishPostalCode(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishPostalCode(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish postal code."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish IBAN.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishIban(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishIban(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish IBAN."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish tax number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishTaxNumber(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && TurkishValidationHelpers.IsValidTurkishTaxNumber(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid Turkish tax number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string value is numeric.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Numeric(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && GeneralValidationHelpers.IsValidNumeric(value.ToString()),
                errorMessage ?? $"{_propertyName} must be a valid number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only letters.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Alpha(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && GeneralValidationHelpers.IsValidAlpha(value.ToString()),
                errorMessage ?? $"{_propertyName} must contain only letters."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only letters and digits.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Alphanumeric(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && GeneralValidationHelpers.IsValidAlphanumeric(value.ToString()),
                errorMessage ?? $"{_propertyName} must contain only letters and numbers."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only digits.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> DigitsOnly(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && GeneralValidationHelpers.IsValidDigitsOnly(value.ToString()),
                errorMessage ?? $"{_propertyName} must contain only digits."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the string value has no whitespace.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NoWhitespace(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && GeneralValidationHelpers.HasNoWhitespace(value.ToString()),
                errorMessage ?? $"{_propertyName} must not contain whitespace."));
        }
        return this;
    }


}
