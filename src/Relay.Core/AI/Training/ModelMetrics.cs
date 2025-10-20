namespace Relay.Core.AI;

/// <summary>
/// Model evaluation metrics
/// </summary>
public class ModelMetrics
{
    /// <summary>
    /// R-Squared (for regression)
    /// </summary>
    public double? RSquared { get; set; }

    /// <summary>
    /// Mean Absolute Error (for regression)
    /// </summary>
    public double? MAE { get; set; }

    /// <summary>
    /// Root Mean Squared Error (for regression)
    /// </summary>
    public double? RMSE { get; set; }

    /// <summary>
    /// Accuracy (for classification)
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Area Under ROC Curve (for classification)
    /// </summary>
    public double? AUC { get; set; }

    /// <summary>
    /// F1 Score (for classification)
    /// </summary>
    public double? F1Score { get; set; }
}
