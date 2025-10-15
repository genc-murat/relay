using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules
{
    /// <summary>
    /// Validation rule that checks if a date is today.
    /// </summary>
    public class TodayValidationRule : IValidationRule<DateTime>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(DateTime request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var today = DateTime.Today;
            if (request.Date != today)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Date must be today." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Validation rule that checks if a date/time offset is today.
    /// </summary>
    public class TodayValidationRuleDateTimeOffset : IValidationRule<DateTimeOffset>
    {
        /// <inheritdoc />
        public ValueTask<IEnumerable<string>> ValidateAsync(DateTimeOffset request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var today = DateTimeOffset.Now.Date;
            if (request.Date != today)
            {
                return new ValueTask<IEnumerable<string>>(new[] { "Date must be today." });
            }

            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }
    }
}