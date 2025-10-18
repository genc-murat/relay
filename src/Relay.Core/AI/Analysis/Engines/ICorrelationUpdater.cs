using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating metric correlation analysis
    /// </summary>
    public interface ICorrelationUpdater
    {
        /// <summary>
        /// Updates correlations between metrics
        /// </summary>
        Dictionary<string, List<string>> UpdateCorrelations(Dictionary<string, double> currentMetrics);
    }
}