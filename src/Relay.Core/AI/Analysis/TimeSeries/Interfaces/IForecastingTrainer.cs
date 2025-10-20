using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Analysis.TimeSeries
{
    /// <summary>
    /// Interface for training forecasting models
    /// </summary>
    public interface IForecastingTrainer
    {
        /// <summary>
        /// Trains a forecasting model for a metric
        /// </summary>
        void TrainModel(string metricName, ForecastingMethod? method = null);

        /// <summary>
        /// Trains a forecasting model for a metric (async)
        /// </summary>
        Task TrainModelAsync(string metricName, ForecastingMethod? method = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if sufficient data is available for training
        /// </summary>
        bool HasSufficientData(string metricName, out int actualCount);
    }
}