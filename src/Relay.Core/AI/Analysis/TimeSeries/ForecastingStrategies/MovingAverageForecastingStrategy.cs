using System;
using System.Collections.Generic;
using Microsoft.ML;
using Microsoft.ML.TimeSeries;

namespace Relay.Core.AI.Analysis.TimeSeries.ForecastingStrategies;

/// <summary>
/// Moving Average forecasting strategy
/// </summary>
internal class MovingAverageForecastingStrategy : IForecastingStrategy
{
    public ForecastingMethod Method => ForecastingMethod.MovingAverage;

    public ITransformer TrainModel(MLContext mlContext, List<MetricDataPoint> history, int horizon)
    {
        var dataView = mlContext.Data.LoadFromEnumerable(history);

        // Use smaller window for moving average
        var windowSize = Math.Min(Math.Max(history.Count / 6, 3), history.Count - 1);

        var forecastingPipeline = mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(MetricForecastResult.ForecastedValues),
            inputColumnName: nameof(MetricDataPoint.Value),
            windowSize: windowSize,
            seriesLength: history.Count,
            trainSize: history.Count,
            horizon: horizon,
            confidenceLevel: 0.85f, // Lower confidence for simple method
            confidenceLowerBoundColumn: nameof(MetricForecastResult.LowerBound),
            confidenceUpperBoundColumn: nameof(MetricForecastResult.UpperBound));

        return forecastingPipeline.Fit(dataView);
    }
}