using System;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Base exception for time-series operations.
    /// </summary>
    public class TimeSeriesException : Exception
    {
        public TimeSeriesException(string message) : base(message) { }
        public TimeSeriesException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the name of the metric that caused the exception.
        /// </summary>
        public string? MetricName { get; set; }

        /// <summary>
        /// Gets the operation that was being performed when the exception occurred.
        /// </summary>
        public string? Operation { get; set; }
    }
}