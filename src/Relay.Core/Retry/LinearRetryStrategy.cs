using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Retry
{
    /// <summary>
    /// Linear retry strategy with fixed delay between attempts.
    /// </summary>
    public class LinearRetryStrategy : IRetryStrategy
    {
        private readonly TimeSpan _delay;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearRetryStrategy"/> class.
        /// </summary>
        /// <param name="delay">The fixed delay between retry attempts.</param>
        public LinearRetryStrategy(TimeSpan delay)
        {
            _delay = delay;
        }

        /// <inheritdoc />
        public async ValueTask<bool> ShouldRetryAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance
            return attempt > 0; // Always retry except for the first attempt
        }

        /// <inheritdoc />
        public async ValueTask<TimeSpan> GetRetryDelayAsync(int attempt, Exception exception, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Make method async for interface compliance
            return _delay;
        }
    }
}