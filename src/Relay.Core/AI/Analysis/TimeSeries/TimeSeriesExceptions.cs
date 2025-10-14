using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Base exception for time-series operations.
    /// </summary>
    public class TimeSeriesException : Exception
    {
        public TimeSeriesException(string message) : base(message) { }
        public TimeSeriesException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when insufficient data is available for an operation.
    /// </summary>
    public class InsufficientDataException : TimeSeriesException
    {
        public InsufficientDataException(string message) : base(message) { }
        public InsufficientDataException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when model training fails.
    /// </summary>
    public class ModelTrainingException : TimeSeriesException
    {
        public ModelTrainingException(string message) : base(message) { }
        public ModelTrainingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when forecasting fails.
    /// </summary>
    public class ForecastingException : TimeSeriesException
    {
        public ForecastingException(string message) : base(message) { }
        public ForecastingException(string message, Exception innerException) : base(message, innerException) { }
    }
}