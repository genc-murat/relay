using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating anomaly detection
    /// </summary>
    public interface IAnomalyUpdater
    {
        /// <summary>
        /// Updates anomaly detection results
        /// </summary>
        List<MetricAnomaly> UpdateAnomalies(
            Dictionary<string, double> currentMetrics,
            Dictionary<string, MovingAverageData> movingAverages);
    }
}