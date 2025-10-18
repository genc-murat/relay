using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies
{
    /// <summary>
    /// Exponential Smoothing forecasting strategy
    /// </summary>
    internal class ExponentialSmoothingForecastingStrategy : IForecastingStrategy
    {
        public ForecastingMethod Method => ForecastingMethod.ExponentialSmoothing;

        public ITransformer TrainModel(MLContext mlContext, List<MetricDataPoint> history, int horizon)
        {
            var dataView = mlContext.Data.LoadFromEnumerable(history);

            // Use Single Spectrum Analysis for exponential smoothing approximation
            var windowSize = Math.Min(Math.Max(history.Count / 4, 5), history.Count - 1);

            var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(MetricForecastResult.ForecastedValues),
                inputColumnName: nameof(MetricDataPoint.Value),
                windowSize: windowSize,
                seriesLength: history.Count,
                trainSize: history.Count,
                horizon: horizon,
                confidenceLevel: 0.90f, // Slightly lower confidence for smoothing
                confidenceLowerBoundColumn: nameof(MetricForecastResult.LowerBound),
                confidenceUpperBoundColumn: nameof(MetricForecastResult.UpperBound));

            return forecastingPipeline.Fit(dataView);
        }
    }
}