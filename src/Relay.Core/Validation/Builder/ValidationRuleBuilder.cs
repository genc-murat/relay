using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<string, List<IValidationRuleConfiguration<TRequest>>> _ruleSets = new();

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

        _rules.Add(new CustomValidationRule<TRequest>((request, ct) => ruleFunc(request!, ct)));
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
    /// Adds business validation rules using the business rules engine.
    /// Only available when TRequest is BusinessValidationRequest.
    /// </summary>
    public ValidationRuleBuilder<TRequest> Business(IBusinessRulesEngine businessRulesEngine)
    {
        if (typeof(TRequest) != typeof(BusinessValidationRequest))
        {
            throw new InvalidOperationException("Business validation is only available for BusinessValidationRequest types.");
        }

        _rules.Add(new CustomValidationRule<TRequest>(async (request, ct) =>
        {
            var businessRequest = (BusinessValidationRequest)(object)request!;
            return await businessRulesEngine.ValidateBusinessRulesAsync(businessRequest, ct);
        }));

        return this;
    }

    /// <summary>
    /// Builds the validation rules.
    /// </summary>
    public IEnumerable<IValidationRuleConfiguration<TRequest>> Build()
    {
        return _rules;
    }

    /// <summary>
    /// Creates a rule set with the specified name.
    /// </summary>
    public ValidationRuleBuilder<TRequest> RuleSet(string ruleSetName, Action<ValidationRuleBuilder<TRequest>> configureRules)
    {
        var ruleSetBuilder = new ValidationRuleBuilder<TRequest>();
        configureRules(ruleSetBuilder);
        _ruleSets[ruleSetName] = ruleSetBuilder._rules;
        return this;
    }

    /// <summary>
    /// Includes rules from the specified rule set.
    /// </summary>
    public ValidationRuleBuilder<TRequest> IncludeRuleSet(string ruleSetName)
    {
        if (_ruleSets.TryGetValue(ruleSetName, out var ruleSet))
        {
            _rules.AddRange(ruleSet);
        }
        return this;
    }

    /// <summary>
    /// Builds the validation rules for the specified rule set.
    /// </summary>
    public IEnumerable<IValidationRuleConfiguration<TRequest>> BuildRuleSet(string ruleSetName)
    {
        return _ruleSets.TryGetValue(ruleSetName, out var ruleSet) ? ruleSet : Enumerable.Empty<IValidationRuleConfiguration<TRequest>>();
    }

    internal static string GetPropertyName<TProperty>(Expression<Func<TRequest, TProperty>> expression)
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
    /// Adds a validation rule for string properties only.
    /// </summary>
    private void AddStringRule(Func<string?, bool> predicate, string errorMessage)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value => value != null && predicate(value.ToString()),
                errorMessage));
        }
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
        AddStringRule(value => !string.IsNullOrWhiteSpace(value), errorMessage ?? $"{_propertyName} must not be empty.");
        return this;
    }

    /// <summary>
    /// Ensures the property meets a custom condition.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Must(Func<TProperty?, bool> predicate, string errorMessage)
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
        AddStringRule(value => value != null && value.Length >= minLength, errorMessage ?? $"{_propertyName} must be at least {minLength} characters long.");
        return this;
    }

    /// <summary>
    /// Ensures the string has a maximum length.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> MaxLength(int maxLength, string? errorMessage = null)
    {
        AddStringRule(value => value != null && value.Length <= maxLength, errorMessage ?? $"{_propertyName} must not exceed {maxLength} characters.");
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
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new EmailValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid email address."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid credit card number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> CreditCard(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new CreditCardValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid credit card number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid URL.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Url(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new UrlValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid URL."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid phone number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> PhoneNumber(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new PhoneNumberValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid phone number."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid GUID.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Guid(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new GuidValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid GUID."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid JSON string.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Json(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new JsonValidationRule(),
                errorMessage ?? $"{_propertyName} must be valid JSON."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid XML string.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Xml(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new XmlValidationRule(),
                errorMessage ?? $"{_propertyName} must be valid XML."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Base64 string.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Base64(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new Base64ValidationRule(),
                errorMessage ?? $"{_propertyName} must be valid Base64."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid JWT token.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Jwt(string? errorMessage = null)
    {
        if (typeof(TProperty) == typeof(string))
        {
            _rules.Add(new PropertyValidationRuleAdapter<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                new JwtValidationRule(),
                errorMessage ?? $"{_propertyName} must be a valid JWT token."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish ID number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishId(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishId(value), errorMessage ?? $"{_propertyName} must be a valid Turkish ID number.");
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish foreigner ID number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishForeignerId(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishForeignerId(value), errorMessage ?? $"{_propertyName} must be a valid Turkish foreigner ID number.");
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish phone number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishPhone(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishPhone(value), errorMessage ?? $"{_propertyName} must be a valid Turkish phone number.");
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish postal code.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishPostalCode(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishPostalCode(value), errorMessage ?? $"{_propertyName} must be a valid Turkish postal code.");
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish IBAN.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishIban(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishIban(value), errorMessage ?? $"{_propertyName} must be a valid Turkish IBAN.");
        return this;
    }

    /// <summary>
    /// Ensures the value is a valid Turkish tax number.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> TurkishTaxNumber(string? errorMessage = null)
    {
        AddStringRule(value => value != null && TurkishValidationHelpers.IsValidTurkishTaxNumber(value), errorMessage ?? $"{_propertyName} must be a valid Turkish tax number.");
        return this;
    }

    /// <summary>
    /// Ensures the string value is numeric.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Numeric(string? errorMessage = null)
    {
        AddStringRule(value => value != null && GeneralValidationHelpers.IsValidNumeric(value), errorMessage ?? $"{_propertyName} must be a valid number.");
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only letters.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Alpha(string? errorMessage = null)
    {
        AddStringRule(value => value != null && GeneralValidationHelpers.IsValidAlpha(value), errorMessage ?? $"{_propertyName} must contain only letters.");
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only letters and digits.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Alphanumeric(string? errorMessage = null)
    {
        AddStringRule(value => value != null && GeneralValidationHelpers.IsValidAlphanumeric(value), errorMessage ?? $"{_propertyName} must contain only letters and numbers.");
        return this;
    }

    /// <summary>
    /// Ensures the string value contains only digits.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> DigitsOnly(string? errorMessage = null)
    {
        AddStringRule(value => value != null && GeneralValidationHelpers.IsValidDigitsOnly(value), errorMessage ?? $"{_propertyName} must contain only digits.");
        return this;
    }

    /// <summary>
    /// Ensures the string value has no whitespace.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NoWhitespace(string? errorMessage = null)
    {
        AddStringRule(value => value != null && GeneralValidationHelpers.HasNoWhitespace(value), errorMessage ?? $"{_propertyName} must not contain whitespace.");
        return this;
    }

    /// <summary>
    /// Applies validation only when the condition is met.
    /// </summary>
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> When(Func<TRequest, bool> condition)
    {
        return new ConditionalPropertyRuleBuilder<TRequest, TProperty>(_propertyName, _propertyFunc, _rules, condition, true);
    }

    /// <summary>
    /// Applies validation only when the condition is not met.
    /// </summary>
    public ConditionalPropertyRuleBuilder<TRequest, TProperty> Unless(Func<TRequest, bool> condition)
    {
        return new ConditionalPropertyRuleBuilder<TRequest, TProperty>(_propertyName, _propertyFunc, _rules, condition, false);
    }

    /// <summary>
    /// Creates a dependent validation rule based on another property.
    /// </summary>
    public DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty> WhenProperty<TDependentProperty>(
        Expression<Func<TRequest, TDependentProperty>> dependentPropertyExpression,
        Func<TDependentProperty, bool> condition)
    {
        var dependentPropertyName = ValidationRuleBuilder<TRequest>.GetPropertyName(dependentPropertyExpression);
        var dependentPropertyFunc = dependentPropertyExpression.Compile();

        return new DependentPropertyRuleBuilder<TRequest, TProperty, TDependentProperty>(
            _propertyName, _propertyFunc, _rules,
            dependentPropertyName, dependentPropertyFunc, condition);
    }

    /// <summary>
    /// Ensures the property value equals the specified value.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> EqualTo(TProperty expectedValue, string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => EqualityComparer<TProperty>.Default.Equals(value, expectedValue),
            errorMessage ?? $"{_propertyName} must equal {expectedValue}."));
        return this;
    }

    /// <summary>
    /// Ensures the property value does not equal the specified value.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NotEqualTo(TProperty expectedValue, string? errorMessage = null)
    {
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => !EqualityComparer<TProperty>.Default.Equals(value, expectedValue),
            errorMessage ?? $"{_propertyName} must not equal {expectedValue}."));
        return this;
    }

    /// <summary>
    /// Ensures the property value is in the specified collection.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> In(IEnumerable<TProperty> validValues, string? errorMessage = null)
    {
        var validSet = new HashSet<TProperty>(validValues);
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value != null && validSet.Contains(value),
            errorMessage ?? $"{_propertyName} must be one of the valid values."));
        return this;
    }

    /// <summary>
    /// Ensures the property value is not in the specified collection.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> NotIn(IEnumerable<TProperty> invalidValues, string? errorMessage = null)
    {
        var invalidSet = new HashSet<TProperty>(invalidValues);
        _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
            _propertyName,
            _propertyFunc,
            value => value == null || !invalidSet.Contains(value),
            errorMessage ?? $"{_propertyName} must not be one of the invalid values."));
        return this;
    }

    /// <summary>
    /// Ensures the property value is between the specified minimum and maximum (inclusive).
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> Between(TProperty min, TProperty max, string? errorMessage = null)
    {
        if (typeof(TProperty).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>)))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value =>
                {
                    if (value == null) return false;
                    var comparable = (IComparable)value;
                    return comparable.CompareTo(min) >= 0 && comparable.CompareTo(max) <= 0;
                },
                errorMessage ?? $"{_propertyName} must be between {min} and {max}."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the property value is greater than or equal to the specified minimum.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> GreaterThanOrEqualTo(TProperty min, string? errorMessage = null)
    {
        if (typeof(TProperty).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>)))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value =>
                {
                    if (value == null) return false;
                    var comparable = (IComparable)value;
                    return comparable.CompareTo(min) >= 0;
                },
                errorMessage ?? $"{_propertyName} must be greater than or equal to {min}."));
        }
        return this;
    }

    /// <summary>
    /// Ensures the property value is less than or equal to the specified maximum.
    /// </summary>
    public PropertyRuleBuilder<TRequest, TProperty> LessThanOrEqualTo(TProperty max, string? errorMessage = null)
    {
        if (typeof(TProperty).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IComparable<>)))
        {
            _rules.Add(new PropertyValidationRule<TRequest, TProperty>(
                _propertyName,
                _propertyFunc,
                value =>
                {
                    if (value == null) return false;
                    var comparable = (IComparable)value;
                    return comparable.CompareTo(max) <= 0;
                },
                errorMessage ?? $"{_propertyName} must be less than or equal to {max}."));
        }
        return this;
    }

}

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
                value => value?.ToString()?.Length >= minLength,
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
                value => value?.ToString()?.Length <= maxLength,
                errorMessage ?? $"{_propertyName} must not exceed {maxLength} characters.",
                GetEffectiveCondition()));
        }
        return this;
    }
}

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
