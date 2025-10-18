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

    /// <summary>
    /// Exception thrown when model training fails.
    /// </summary>
    public class ModelTrainingException : TimeSeriesException
    {
        public ModelTrainingException(string message) : base(message) { }
        public ModelTrainingException(string message, Exception innerException) : base(message, innerException) { }

        /// <summary>
        /// Gets the forecasting method that was being used.
        /// </summary>
        public ForecastingMethod? ForecastingMethod { get; set; }
    }

    /// <summary>
    /// Exception thrown when forecasting fails.
    /// </summary>
    public class ForecastingException : TimeSeriesException
    {
        public ForecastingException(string message) : base(message) { }
        public ForecastingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when anomaly detection fails.
    /// </summary>
    public class AnomalyDetectionException : TimeSeriesException
    {
        public AnomalyDetectionException(string message) : base(message) { }
        public AnomalyDetectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}