using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Analysis.TimeSeries;

/// <summary>
/// Interface for anomaly detection operations
/// </summary>
public interface IAnomalyDetectionService
{
    /// <summary>
    /// Detect anomalies in time-series data
    /// </summary>
    List<AnomalyDetectionResult> DetectAnomalies(string metricName, int lookbackPoints = 100);

    /// <summary>
    /// Detect anomalies in time-series data (async)
    /// </summary>
    Task<List<AnomalyDetectionResult>> DetectAnomaliesAsync(string metricName, int lookbackPoints = 100, CancellationToken cancellationToken = default);
}