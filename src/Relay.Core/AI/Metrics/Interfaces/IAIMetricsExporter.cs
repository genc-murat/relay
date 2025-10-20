using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Metrics.Interfaces
{
    public interface IAIMetricsExporter
    {
        ValueTask ExportMetricsAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default);
    }
}