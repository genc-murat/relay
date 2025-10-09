using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Validation;

/// <summary>
/// Base class for fluent validators.
/// </summary>
/// <typeparam name="TRequest">The type of request to validate.</typeparam>
public abstract class AbstractValidator<TRequest> : IValidator<TRequest>
{
    private readonly List<IValidationRuleConfiguration<TRequest>> _rules = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractValidator{TRequest}"/> class.
    /// </summary>
    protected AbstractValidator()
    {
        ConfigureRules();
    }

    /// <summary>
    /// Configures validation rules. Override this method to define rules.
    /// </summary>
    protected virtual void ConfigureRules()
    {
    }

    /// <summary>
    /// Creates a validation rule builder for defining rules.
    /// </summary>
    protected ValidationRuleBuilder<TRequest> RuleBuilder()
    {
        var builder = new ValidationRuleBuilder<TRequest>();
        return builder;
    }

    /// <summary>
    /// Adds a rule to the validator.
    /// </summary>
    protected void AddRule(IValidationRuleConfiguration<TRequest> rule)
    {
        _rules.Add(rule);
    }

    /// <summary>
    /// Adds multiple rules using the builder.
    /// </summary>
    protected void AddRules(ValidationRuleBuilder<TRequest> builder)
    {
        _rules.AddRange(builder.Build());
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        foreach (var rule in _rules)
        {
            var ruleErrors = await rule.ValidateAsync(request, cancellationToken);
            errors.AddRange(ruleErrors);
        }

        return errors;
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsValidAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(request, cancellationToken);
        return !errors.Any();
    }
}
