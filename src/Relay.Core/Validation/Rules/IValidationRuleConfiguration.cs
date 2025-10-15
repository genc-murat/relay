using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Interface for validation rule configurations.
/// </summary>
public interface IValidationRuleConfiguration<in TRequest> : IValidationRule<TRequest>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    new ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}
