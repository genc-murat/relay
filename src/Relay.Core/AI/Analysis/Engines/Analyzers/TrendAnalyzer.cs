 using System;
 using System.Collections.Generic;
 using Microsoft.Extensions.Logging;

 namespace Relay.Core.AI
 {
     /// <summary>
     /// Analyzes performance metric trends and detects patterns
     /// </summary>
     internal sealed class TrendAnalyzer : ITrendAnalyzer
     {
         private readonly ILogger<TrendAnalyzer> _logger;
         private readonly IMovingAverageUpdater _movingAverageUpdater;
         private readonly ITrendDirectionUpdater _trendDirectionUpdater;
         private readonly ITrendVelocityUpdater _trendVelocityUpdater;
         private readonly ISeasonalityUpdater _seasonalityUpdater;
         private readonly IRegressionUpdater _regressionUpdater;
         private readonly ICorrelationUpdater _correlationUpdater;
         private readonly IAnomalyUpdater _anomalyUpdater;

         public TrendAnalyzer(
             ILogger<TrendAnalyzer> logger,
             IMovingAverageUpdater movingAverageUpdater,
             ITrendDirectionUpdater trendDirectionUpdater,
             ITrendVelocityUpdater trendVelocityUpdater,
             ISeasonalityUpdater seasonalityUpdater,
             IRegressionUpdater regressionUpdater,
             ICorrelationUpdater correlationUpdater,
             IAnomalyUpdater anomalyUpdater)
         {
             _logger = logger ?? throw new ArgumentNullException(nameof(logger));
             _movingAverageUpdater = movingAverageUpdater ?? throw new ArgumentNullException(nameof(movingAverageUpdater));
             _trendDirectionUpdater = trendDirectionUpdater ?? throw new ArgumentNullException(nameof(trendDirectionUpdater));
             _trendVelocityUpdater = trendVelocityUpdater ?? throw new ArgumentNullException(nameof(trendVelocityUpdater));
             _seasonalityUpdater = seasonalityUpdater ?? throw new ArgumentNullException(nameof(seasonalityUpdater));
             _regressionUpdater = regressionUpdater ?? throw new ArgumentNullException(nameof(regressionUpdater));
             _correlationUpdater = correlationUpdater ?? throw new ArgumentNullException(nameof(correlationUpdater));
             _anomalyUpdater = anomalyUpdater ?? throw new ArgumentNullException(nameof(anomalyUpdater));
         }

         public TrendAnalysisResult AnalyzeMetricTrends(Dictionary<string, double> currentMetrics)
         {
             try
             {
                 var timestamp = DateTime.UtcNow;
                 _logger.LogDebug("Starting metric trend analysis for {Count} metrics at {Timestamp}",
                     currentMetrics.Count, timestamp);

                 var movingAverages = _movingAverageUpdater.UpdateMovingAverages(currentMetrics, timestamp);
                 var trendDirections = _trendDirectionUpdater.UpdateTrendDirections(currentMetrics, movingAverages);
                 var trendVelocities = _trendVelocityUpdater.UpdateTrendVelocities(currentMetrics, timestamp);
                 var seasonalityPatterns = _seasonalityUpdater.UpdateSeasonalityPatterns(currentMetrics, timestamp);
                 var regressionAnalysis = _regressionUpdater.UpdateRegressionResults(currentMetrics, timestamp);
                 var correlations = _correlationUpdater.UpdateCorrelations(currentMetrics);
                 var anomalies = _anomalyUpdater.UpdateAnomalies(currentMetrics, movingAverages);

                 _logger.LogInformation("Metric trend analysis completed: {Trends} trends detected, {Anomalies} anomalies found",
                     trendDirections.Count, anomalies.Count);

                 return new TrendAnalysisResult
                 {
                     Timestamp = timestamp,
                     MovingAverages = movingAverages,
                     TrendDirections = trendDirections,
                     TrendVelocities = trendVelocities,
                     SeasonalityPatterns = seasonalityPatterns,
                     RegressionResults = regressionAnalysis,
                     Correlations = correlations,
                     Anomalies = anomalies
                 };
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error analyzing metric trends");
                 return new TrendAnalysisResult { Timestamp = DateTime.UtcNow };
             }
         }

         public Dictionary<string, MovingAverageData> CalculateMovingAverages(
             Dictionary<string, double> currentMetrics,
             DateTime timestamp)
         {
             return _movingAverageUpdater.UpdateMovingAverages(currentMetrics, timestamp);
         }











         public List<MetricAnomaly> DetectPerformanceAnomalies(
             Dictionary<string, double> currentMetrics,
             Dictionary<string, MovingAverageData> movingAverages)
         {
             return _anomalyUpdater.UpdateAnomalies(currentMetrics, movingAverages);
         }


    }
}
