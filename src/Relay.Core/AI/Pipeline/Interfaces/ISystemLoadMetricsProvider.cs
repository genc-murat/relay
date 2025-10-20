using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Pipeline.Interfaces
{
    /// <summary>
    /// Interface for providing system load metrics for AI optimization decisions.
    /// </summary>
    public interface ISystemLoadMetricsProvider
    {
        /// <summary>
        /// Gets the current system load metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The current system load metrics.</returns>
        ValueTask<SystemLoadMetrics> GetCurrentLoadAsync(CancellationToken cancellationToken = default);
    }
}