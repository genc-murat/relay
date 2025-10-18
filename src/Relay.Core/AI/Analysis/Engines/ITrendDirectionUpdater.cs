using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating trend direction analysis
    /// </summary>
    public interface ITrendDirectionUpdater
    {
        /// <summary>
        /// Updates trend directions based on current metrics and moving averages
        /// </summary>
        Dictionary<string, TrendDirection> UpdateTrendDirections(
            Dictionary<string, double> currentMetrics,
            Dictionary<string, MovingAverageData> movingAverages);
    }
}