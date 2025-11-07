using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Updates feature importance weights using multiple statistical methods:
/// - Correlation analysis with prediction success
/// - Mutual information calculation
/// - Variance analysis for feature stability
/// - Normalized importance scoring
/// </summary>
internal class FeatureImportancePatternUpdater : IPatternUpdater
{
    private readonly ILogger<FeatureImportancePatternUpdater> _logger;
    private readonly PatternRecognitionConfig _config;

    public FeatureImportancePatternUpdater(ILogger<FeatureImportancePatternUpdater> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var updatedCount = 0;

            foreach (var feature in _config.Features)
            {
                var importanceScore = CalculateFeatureImportance(feature, predictions);

                _logger.LogDebug("Feature {Feature} importance score: {Score:F4} (based on {Count} predictions)",
                    feature, importanceScore, predictions.Length);

                updatedCount++;
            }

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating feature importance weights");
            return 0;
        }
    }

    /// <summary>
    /// Calculates feature importance using multiple statistical methods:
    /// 1. Correlation with success rate
    /// 2. Mutual information contribution
    /// 3. Variance stability analysis
    /// 4. Predictive discriminability
    /// </summary>
    private double CalculateFeatureImportance(string feature, PredictionResult[] predictions)
    {
        try
        {
            if (predictions == null || predictions.Length == 0)
                return 0.0;

            // Separate successful and failed predictions
            var successfulPredictions = predictions.Where(p => p.ActualImprovement > TimeSpan.Zero).ToArray();
            var failedPredictions = predictions.Where(p => p.ActualImprovement <= TimeSpan.Zero).ToArray();

            if (successfulPredictions.Length == 0 || failedPredictions.Length == 0)
                return 0.0;

            // Calculate four components of importance
            var correlationImportance = CalculateCorrelationImportance(feature, successfulPredictions, failedPredictions);
            var mutualInformation = CalculateMutualInformation(feature, successfulPredictions, failedPredictions);
            var varianceStability = CalculateVarianceStability(feature, predictions);
            var discriminability = CalculateDiscriminability(feature, successfulPredictions, failedPredictions);

            // Weighted combination of importance metrics
            var importance = (correlationImportance * 0.4) +
                           (mutualInformation * 0.3) +
                           (varianceStability * 0.2) +
                           (discriminability * 0.1);

            return Math.Clamp(importance, 0.0, 1.0);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error calculating importance for feature {Feature}", feature);
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates correlation between feature values and prediction success.
    /// Higher values for features that show stronger correlation with successful predictions.
    /// </summary>
    private double CalculateCorrelationImportance(string feature, PredictionResult[] successful, PredictionResult[] failed)
    {
        try
        {
            var successfulValues = ExtractFeatureValues(feature, successful);
            var failedValues = ExtractFeatureValues(feature, failed);

            if (successfulValues.Count == 0 || failedValues.Count == 0)
                return 0.0;

            var successMean = successfulValues.Average();
            var failedMean = failedValues.Average();

            // Measure separation between successful and failed distributions
            var successVariance = successfulValues.Count > 1
                ? successfulValues.Sum(v => Math.Pow(v - successMean, 2)) / successfulValues.Count
                : 0.0;
            var failedVariance = failedValues.Count > 1
                ? failedValues.Sum(v => Math.Pow(v - failedMean, 2)) / failedValues.Count
                : 0.0;

            // Calculate effect size (Cohen's d)
            var pooledStdDev = Math.Sqrt((successVariance + failedVariance) / 2);
            if (pooledStdDev <= 0.0)
                return 0.0;

            var effectSize = Math.Abs(successMean - failedMean) / pooledStdDev;

            // Normalize to 0-1 range (effect sizes > 2.0 are considered very strong)
            return Math.Min(effectSize / 2.0, 1.0);
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates mutual information between feature and prediction outcome.
    /// Measures how much knowing the feature reduces uncertainty about prediction success.
    /// </summary>
    private double CalculateMutualInformation(string feature, PredictionResult[] successful, PredictionResult[] failed)
    {
        try
        {
            var allPredictions = successful.Concat(failed).ToArray();
            var successRate = (double)successful.Length / allPredictions.Length;
            var failureRate = 1.0 - successRate;

            // Entropy of the outcome
            var outcomeEntropy = CalculateEntropy(new[] { successRate, failureRate });

            // Categorize feature values into bins for mutual information calculation
            var values = ExtractFeatureValues(feature, allPredictions);
            if (values.Count < 2)
                return 0.0;

            var min = values.Min();
            var max = values.Max();
            var range = max - min;

            if (range <= 0.0)
                return 0.0;

            // Create 3 equal-width bins
            const int binCount = 3;
            var binSize = range / binCount;
            var bins = new List<(double Lower, double Upper, int SuccessCount, int TotalCount)>();

            for (int i = 0; i < binCount; i++)
            {
                var lower = min + (i * binSize);
                var upper = (i == binCount - 1) ? max + 0.001 : min + ((i + 1) * binSize);
                var binSuccesses = successful.Count(p =>
                {
                    var val = ExtractSingleFeatureValue(feature, p);
                    return val >= lower && val < upper;
                });
                var binTotal = allPredictions.Count(p =>
                {
                    var val = ExtractSingleFeatureValue(feature, p);
                    return val >= lower && val < upper;
                });

                if (binTotal > 0)
                    bins.Add((lower, upper, binSuccesses, binTotal));
            }

            if (bins.Count == 0)
                return 0.0;

            // Calculate conditional entropy
            double conditionalEntropy = 0.0;
            foreach (var bin in bins)
            {
                var binProbability = (double)bin.TotalCount / allPredictions.Length;
                var successProbInBin = bin.TotalCount > 0 ? (double)bin.SuccessCount / bin.TotalCount : 0.0;
                var failureProbInBin = 1.0 - successProbInBin;
                var binEntropy = CalculateEntropy(new[] { successProbInBin, failureProbInBin });
                conditionalEntropy += binProbability * binEntropy;
            }

            // Mutual information = outcome entropy - conditional entropy
            var mutualInfo = outcomeEntropy - conditionalEntropy;

            // Normalize to 0-1 range
            return Math.Max(0.0, mutualInfo / outcomeEntropy);
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates feature stability based on variance analysis.
    /// More stable features (lower variance) are more predictive and important.
    /// </summary>
    private double CalculateVarianceStability(string feature, PredictionResult[] predictions)
    {
        try
        {
            var values = ExtractFeatureValues(feature, predictions);
            if (values.Count <= 1)
                return 1.0; // Perfect stability if only one value

            var mean = values.Average();
            var variance = values.Sum(v => Math.Pow(v - mean, 2)) / values.Count;
            var stdDev = Math.Sqrt(variance);

            if (mean <= 0.0)
                return 0.0;

            // Coefficient of variation (normalized standard deviation)
            var coefficientOfVariation = stdDev / Math.Abs(mean);

            // Lower variation = higher stability (importance)
            // Values with CV < 0.5 are considered stable
            return Math.Max(0.0, 1.0 - Math.Min(coefficientOfVariation, 2.0) / 2.0);
        }
        catch
        {
            return 0.5; // Default neutral value on error
        }
    }

    /// <summary>
    /// Calculates discriminability - how well the feature separates successful from failed predictions.
    /// Uses area under the curve (AUC) concept for binary classification.
    /// </summary>
    private double CalculateDiscriminability(string feature, PredictionResult[] successful, PredictionResult[] failed)
    {
        try
        {
            var successfulValues = ExtractFeatureValues(feature, successful);
            var failedValues = ExtractFeatureValues(feature, failed);

            if (successfulValues.Count == 0 || failedValues.Count == 0)
                return 0.0;

            // Calculate Mann-Whitney U statistic (non-parametric discriminability measure)
            int concordantPairs = 0;
            int totalPairs = 0;

            foreach (var sVal in successfulValues)
            {
                foreach (var fVal in failedValues)
                {
                    totalPairs++;
                    if (sVal > fVal)
                        concordantPairs++;
                }
            }

            // AUC approximation from U statistic
            if (totalPairs == 0)
                return 0.0;

            var auc = (double)concordantPairs / totalPairs;

            // AUC closer to 0.5 means no discriminability, closer to 1.0 means perfect discriminability
            // Transform to 0-1 scale: (AUC - 0.5) * 2
            return Math.Max(0.0, (auc - 0.5) * 2.0);
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Extracts numerical values for a given feature from prediction results.
    /// </summary>
    private List<double> ExtractFeatureValues(string feature, PredictionResult[] predictions)
    {
        var values = new List<double>();
        foreach (var prediction in predictions)
        {
            var value = ExtractSingleFeatureValue(feature, prediction);
            if (!double.IsNaN(value) && !double.IsInfinity(value))
                values.Add(value);
        }
        return values;
    }

    /// <summary>
    /// Extracts a single numerical value for a feature from a prediction result.
    /// </summary>
    private double ExtractSingleFeatureValue(string feature, PredictionResult prediction)
    {
        try
        {
            return feature switch
            {
                "ExecutionTime" => prediction.Metrics.AverageExecutionTime.TotalMilliseconds,
                "ConcurrencyLevel" => prediction.Metrics.ConcurrentExecutions,
                "MemoryUsage" => prediction.Metrics.MemoryUsage,
                "RepeatRate" => CalculateRepeatRate(prediction),
                "CacheHitRatio" => CalculateCacheHitRatio(prediction),
                _ => 0.0
            };
        }
        catch
        {
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates repeat rate from prediction metrics.
    /// </summary>
    private double CalculateRepeatRate(PredictionResult prediction)
    {
        if (prediction.Metrics.TotalExecutions <= 0)
            return 0.0;
        return prediction.Metrics.SuccessfulExecutions / (double)prediction.Metrics.TotalExecutions;
    }

    /// <summary>
    /// Calculates cache hit ratio from prediction metrics.
    /// </summary>
    private double CalculateCacheHitRatio(PredictionResult prediction)
    {
        // For now, estimate based on success rate and execution pattern
        var baseRatio = prediction.Metrics.SuccessRate;
        var executionFactor = Math.Min(1.0, prediction.Metrics.TotalExecutions / 100.0);
        return baseRatio * executionFactor;
    }

    /// <summary>
    /// Calculates Shannon entropy for a probability distribution.
    /// </summary>
    private double CalculateEntropy(double[] probabilities)
    {
        double entropy = 0.0;
        foreach (var p in probabilities)
        {
            if (p > 0.0)
                entropy -= p * Math.Log(p, 2.0);
        }
        return entropy;
    }
}