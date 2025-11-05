using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a date/time offset is in the future.
/// </summary>
public class FutureValidationRuleDateTimeOffset : IValidationRule<DateTimeOffset>
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(DateTimeOffset request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request <= DateTimeOffset.Now)
        {
            return new ValueTask<IEnumerable<string>>(new[] { "Date must be in the future." });
        }

        return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
    }
}