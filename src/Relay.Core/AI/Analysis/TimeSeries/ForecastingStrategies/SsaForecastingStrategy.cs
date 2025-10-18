using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies
{
    /// <summary>
    /// SSA (Singular Spectrum Analysis) forecasting strategy
    /// </summary>
    internal class SsaForecastingStrategy : IForecastingStrategy
    {
        public ForecastingMethod Method => ForecastingMethod.SSA;

        public ITransformer TrainModel(MLContext mlContext, List<MetricDataPoint> history, int horizon)
        {
            var dataView = mlContext.Data.LoadFromEnumerable(history);

            // SSA requires minimum window size
            var windowSize = Math.Min(Math.Max(8, history.Count / 4), history.Count - 1);
            var seriesLength = Math.Min(Math.Max(history.Count / 2, 10), history.Count);

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