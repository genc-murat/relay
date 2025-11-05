using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Business rules engine interface.
/// </summary>
public interface IBusinessRulesEngine
{
    ValueTask<IEnumerable<string>> ValidateBusinessRulesAsync(
        BusinessValidationRequest request,
        CancellationToken cancellationToken = default);
}
