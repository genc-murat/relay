using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Interface for updating trend velocity calculations
    /// </summary>
    public interface ITrendVelocityUpdater
    {
        /// <summary>
        /// Updates trend velocities for the given metrics
        /// </summary>
        Dictionary<string, double> UpdateTrendVelocities(
            Dictionary<string, double> currentMetrics,
            DateTime timestamp);
    }
}