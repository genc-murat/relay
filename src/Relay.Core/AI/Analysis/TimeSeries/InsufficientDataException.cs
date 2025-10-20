using System;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Exception thrown when insufficient data is available for an operation.
    /// </summary>
    public class InsufficientDataException : TimeSeriesException
    {
        public InsufficientDataException(string message) : base(message) { }
        public InsufficientDataException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the minimum required data points.
        /// </summary>
        public int MinimumRequired { get; set; }

        /// <summary>
        /// Gets the actual data points available.
        /// </summary>
        public int ActualCount { get; set; }
    }
}