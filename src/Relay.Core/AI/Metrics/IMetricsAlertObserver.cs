using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Observer interface for metrics alerts.
    /// </summary>
    public interface IMetricsAlertObserver
    {
        /// <summary>
        /// Called when alerts are detected.
        /// </summary>
        /// <param name="alerts">The list of alert messages.</param>
        /// <param name="statistics">The statistics that triggered the alerts.</param>
        void OnAlertsDetected(IReadOnlyList<string> alerts, AIModelStatistics statistics);
    }
}