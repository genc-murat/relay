using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies
{
    /// <summary>
    /// Ensemble forecasting strategy combining multiple approaches
    /// </summary>
    internal class EnsembleForecastingStrategy : IForecastingStrategy
    {
        public ForecastingMethod Method => ForecastingMethod.Ensemble;

        public ITransformer TrainModel(MLContext mlContext, List<MetricDataPoint> history, int horizon)
        {
            // For ensemble, use SSA with optimized parameters
            var dataView = mlContext.Data.LoadFromEnumerable(history);

            var windowSize = Math.Min(Math.Max(8, history.Count / 3), history.Count - 1);
            var seriesLength = Math.Min(Math.Max(history.Count * 3 / 4, 15), history.Count);

            var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(MetricForecastResult.ForecastedValues),
                inputColumnName: nameof(MetricDataPoint.Value),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: history.Count,
                horizon: horizon,
                confidenceLevel: 0.95f,
                confidenceLowerBoundColumn: nameof(MetricForecastResult.LowerBound),
                confidenceUpperBoundColumn: nameof(MetricForecastResult.UpperBound));

            return forecastingPipeline.Fit(dataView);
        }
    }
}