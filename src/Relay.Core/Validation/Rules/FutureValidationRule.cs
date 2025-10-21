using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a date/time is in the future.
/// </summary>
public class FutureValidationRule : IValidationRule<DateTime>
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(DateTime request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Convert both to UTC for consistent comparison
        var requestUtc = request.ToUniversalTime();
        var nowUtc = DateTime.UtcNow;

        // Use strict inequality to avoid timing precision issues
        if (requestUtc <= nowUtc)
        {
            return new ValueTask<IEnumerable<string>>(new[] { "Date must be in the future." });
        }

        return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
    }
}
