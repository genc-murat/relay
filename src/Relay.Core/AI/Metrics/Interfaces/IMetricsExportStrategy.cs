using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Metrics.Interfaces
{
    /// <summary>
    /// Strategy interface for different metrics export approaches.
    /// </summary>
    public interface IMetricsExportStrategy
    {
        /// <summary>
        /// Gets the name of the export strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Exports metrics using this strategy.
        /// </summary>
        ValueTask ExportAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default);
    }
}