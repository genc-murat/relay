using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a date/time offset is in the past.
/// </summary>
public class PastValidationRuleDateTimeOffset : IValidationRule<DateTimeOffset>
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(DateTimeOffset request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request >= DateTimeOffset.Now)
        {
            return new ValueTask<IEnumerable<string>>(new[] { "Date must be in the past." });
        }

        return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
    }
}