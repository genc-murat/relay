using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating moving average calculations
    /// </summary>
    public interface IMovingAverageUpdater
    {
        /// <summary>
        /// Updates moving averages for the given metrics
        /// </summary>
        Dictionary<string, MovingAverageData> UpdateMovingAverages(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp);
    }
}