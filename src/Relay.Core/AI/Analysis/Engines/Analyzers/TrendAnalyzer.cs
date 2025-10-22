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

                  var insights = GenerateTrendInsights(trendDirections, trendVelocities, anomalies, currentMetrics);

                  _logger.LogInformation("Metric trend analysis completed: {Trends} trends detected, {Anomalies} anomalies found, {Insights} insights generated",
                      trendDirections.Count, anomalies.Count, insights.Count);

                  return new TrendAnalysisResult
                  {
                      Timestamp = timestamp,
                      MovingAverages = movingAverages,
                      TrendDirections = trendDirections,
                      TrendVelocities = trendVelocities,
                      SeasonalityPatterns = seasonalityPatterns,
                      RegressionResults = regressionAnalysis,
                      Correlations = correlations,
                      Anomalies = anomalies,
                      Insights = insights
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

          private List<TrendInsight> GenerateTrendInsights(
              Dictionary<string, TrendDirection> trendDirections,
              Dictionary<string, double> trendVelocities,
              List<MetricAnomaly> anomalies,
              Dictionary<string, double> currentMetrics)
          {
              var insights = new List<TrendInsight>();

              // Analyze trend directions for insights
              foreach (var trend in trendDirections)
              {
                  var velocity = trendVelocities.GetValueOrDefault(trend.Key, 0.0);
                  var currentValue = currentMetrics.GetValueOrDefault(trend.Key, 0.0);

                  // Generate insights based on trend direction and velocity
                  if (trend.Value == TrendDirection.Increasing && Math.Abs(velocity) > 0.1)
                  {
                      var severity = Math.Abs(velocity) > 0.5 ? InsightSeverity.Critical : InsightSeverity.Warning;
                      var message = $"Metric '{trend.Key}' is trending upward with high velocity ({velocity:F3}/min)";

                      string action;
                      if (trend.Key.Contains("cpu", StringComparison.OrdinalIgnoreCase))
                          action = "Consider scaling CPU resources or optimizing CPU-intensive operations";
                      else if (trend.Key.Contains("memory", StringComparison.OrdinalIgnoreCase))
                          action = "Monitor memory usage and consider memory optimization techniques";
                      else if (trend.Key.Contains("error", StringComparison.OrdinalIgnoreCase))
                          action = "Investigate error sources and implement error handling improvements";
                      else
                          action = "Monitor this metric closely and prepare for potential scaling needs";

                      insights.Add(new TrendInsight
                      {
                          Category = "Performance Trend",
                          Severity = severity,
                          Message = message,
                          RecommendedAction = action
                      });
                  }
                  else if (trend.Value == TrendDirection.Decreasing && Math.Abs(velocity) > 0.1)
                  {
                      var severity = Math.Abs(velocity) > 0.5 ? InsightSeverity.Info : InsightSeverity.Info;
                      var message = $"Metric '{trend.Key}' is improving with downward trend ({velocity:F3}/min)";

                      insights.Add(new TrendInsight
                      {
                          Category = "Performance Improvement",
                          Severity = severity,
                          Message = message,
                          RecommendedAction = "Continue monitoring to ensure trend stability"
                      });
                  }
              }

              // Generate insights for anomalies
              foreach (var anomaly in anomalies)
              {
                  var severity = anomaly.Severity == AnomalySeverity.Critical ? InsightSeverity.Critical :
                                anomaly.Severity == AnomalySeverity.High ? InsightSeverity.Warning :
                                anomaly.Severity == AnomalySeverity.Medium ? InsightSeverity.Warning : InsightSeverity.Info;

                  var message = $"Anomaly detected in '{anomaly.MetricName}': {anomaly.Description}";
                  var action = anomaly.Severity == AnomalySeverity.Critical ?
                      "Immediate investigation required - check system health and logs" :
                      "Monitor this anomaly and investigate underlying causes";

                  insights.Add(new TrendInsight
                  {
                      Category = "Anomaly Detection",
                      Severity = severity,
                      Message = message,
                      RecommendedAction = action
                  });
              }

              // Generate insights for high utilization metrics
              foreach (var metric in currentMetrics)
              {
                  if (metric.Value > 0.9 && (metric.Key.Contains("cpu") || metric.Key.Contains("memory")))
                  {
                      insights.Add(new TrendInsight
                      {
                          Category = "Resource Utilization",
                          Severity = InsightSeverity.Critical,
                          Message = $"Critical utilization level detected for '{metric.Key}': {metric.Value:P1}",
                          RecommendedAction = "Immediate action required - scale resources or optimize resource usage"
                      });
                  }
                  else if (metric.Value > 0.8 && (metric.Key.Contains("cpu") || metric.Key.Contains("memory")))
                  {
                      insights.Add(new TrendInsight
                      {
                          Category = "Resource Utilization",
                          Severity = InsightSeverity.Warning,
                          Message = $"High utilization level detected for '{metric.Key}': {metric.Value:P1}",
                          RecommendedAction = "Monitor closely and prepare scaling strategies"
                      });
                  }
              }

              return insights;
          }
    }
}
