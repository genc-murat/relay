using System;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Exception thrown when anomaly detection fails.
    /// </summary>
    public class AnomalyDetectionException : TimeSeriesException
    {
        public AnomalyDetectionException(string message) : base(message) { }
        public AnomalyDetectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}