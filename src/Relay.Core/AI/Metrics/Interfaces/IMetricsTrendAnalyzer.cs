using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Metrics.Interfaces
{
    /// <summary>
    /// Interface for analyzing trends in AI model metrics.
    /// </summary>
    public interface IMetricsTrendAnalyzer
    {
        /// <summary>
        /// Analyzes trends for the given statistics.
        /// </summary>
        ValueTask AnalyzeTrendsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default);
    }
}