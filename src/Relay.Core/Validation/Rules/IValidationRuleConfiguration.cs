using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Interface for validation rule configurations.
/// </summary>
public interface IValidationRuleConfiguration<in TRequest>
{
    /// <summary>
    /// Validates the request.
    /// </summary>
    ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}
