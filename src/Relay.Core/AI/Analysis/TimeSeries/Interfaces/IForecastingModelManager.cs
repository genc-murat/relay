using System.Collections.Generic;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Interface for managing forecasting models
    /// </summary>
    public interface IForecastingModelManager
    {
        /// <summary>
        /// Stores a trained model for a metric
        /// </summary>
        void StoreModel(string metricName, ITransformer model, ForecastingMethod method);

        /// <summary>
        /// Retrieves a trained model for a metric
        /// </summary>
        ITransformer? GetModel(string metricName);

        /// <summary>
        /// Checks if a model exists for a metric
        /// </summary>
        bool HasModel(string metricName);

        /// <summary>
        /// Gets all metric names that have trained models
        /// </summary>
        IEnumerable<string> GetAvailableMetrics();

        /// <summary>
        /// Removes a model for a metric
        /// </summary>
        void RemoveModel(string metricName);
    }
}