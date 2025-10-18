using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating regression analysis
    /// </summary>
    public interface IRegressionUpdater
    {
        /// <summary>
        /// Updates regression results for the given metrics
        /// </summary>
        Dictionary<string, RegressionResult> UpdateRegressionResults(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp);
    }
}