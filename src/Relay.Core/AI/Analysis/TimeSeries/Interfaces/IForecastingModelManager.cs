using System.Collections.Generic;
using Microsoft.ML;

namespace Relay.Core.AI.Analysis.TimeSeries;

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
    /// Retrieves the forecasting method used for a metric
    /// </summary>
    ForecastingMethod? GetMethod(string metricName);

    /// <summary>
    /// Checks if a model exists for a metric
    /// </summary>
    bool HasModel(string metricName);

    /// <summary>
    /// Gets all metric names that have trained models
    /// </summary>
    IEnumerable<string> GetAvailableMetrics();

    /// <summary>
    /// Gets the total number of stored models
    /// </summary>
    int GetModelCount();

    /// <summary>
    /// Stores multiple models at once
    /// </summary>
    void StoreModels(IDictionary<string, (ITransformer Model, ForecastingMethod Method)> models);

    /// <summary>
    /// Removes multiple models at once
    /// </summary>
    void RemoveModels(IEnumerable<string> metricNames);

    /// <summary>
    /// Removes a model for a metric
    /// </summary>
    void RemoveModel(string metricName);

    /// <summary>
    /// Clears all stored models
    /// </summary>
    void ClearAll();
}