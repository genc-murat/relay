using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Custom validation rule that allows users to define their own validation logic.
/// </summary>
public class CustomValidationRule<TRequest> : IValidationRuleConfiguration<TRequest>
{
    private readonly Func<TRequest, CancellationToken, ValueTask<IEnumerable<string>>> _validationFunc;

    public CustomValidationRule(Func<TRequest, CancellationToken, ValueTask<IEnumerable<string>>> validationFunc)
    {
        _validationFunc = validationFunc ?? throw new ArgumentNullException(nameof(validationFunc));
    }

    public ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        return _validationFunc(request, cancellationToken);
    }
}