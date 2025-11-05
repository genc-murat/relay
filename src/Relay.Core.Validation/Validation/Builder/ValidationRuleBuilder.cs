using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
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
