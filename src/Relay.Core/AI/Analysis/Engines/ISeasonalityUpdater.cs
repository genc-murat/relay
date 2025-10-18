using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating seasonality pattern analysis
    /// </summary>
    public interface ISeasonalityUpdater
    {
        /// <summary>
        /// Updates seasonality patterns for the given metrics
        /// </summary>
        Dictionary<string, SeasonalityPattern> UpdateSeasonalityPatterns(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp);
    }
}