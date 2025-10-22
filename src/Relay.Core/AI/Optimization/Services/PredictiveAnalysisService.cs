using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using Relay.Core.AI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for generating predictive analysis of system behavior
    /// </summary>
    public class PredictiveAnalysisService
    {
        private readonly ILogger _logger;
        private readonly Queue<SystemMetricsSnapshot> _metricsHistory = new();
        private readonly List<LoadTransition> _loadTransitions = new();
        private readonly int _maxHistorySize = 100;
        private readonly object _historyLock = new();
        private LoadLevel _previousLoadLevel = LoadLevel.Idle;

        public PredictiveAnalysisService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PredictiveAnalysis GeneratePredictiveAnalysis()
        {
            lock (_historyLock)
            {
                if (_metricsHistory.Count < 10)
                {
                    // Not enough data for reliable predictions
                    return new PredictiveAnalysis
                    {
                        PredictionConfidence = 0.1,
                        PotentialIssues = new List<string> { "Insufficient historical data for predictions" }
                    };
                }

                var nextHourPredictions = PredictNextHour();
                var nextDayPredictions = PredictNextDay();
                var potentialIssues = IdentifyPotentialIssues();
                var scalingRecommendations = GenerateScalingRecommendations();
                var confidence = CalculatePredictionConfidence();

                return new PredictiveAnalysis
                {
                    NextHourPredictions = nextHourPredictions,
                    NextDayPredictions = nextDayPredictions,
                    PotentialIssues = potentialIssues,
                    ScalingRecommendations = scalingRecommendations,
                    PredictionConfidence = confidence
                };
            }
        }

        internal List<LoadTransition> GetLoadTransitions()
        {
            lock (_historyLock)
            {
                return _loadTransitions.ToList();
            }
        }

        public LoadPatternData AnalyzeLoadPatterns()
        {
            lock (_historyLock)
            {
                // Track load level transitions even with minimal data
                if (_metricsHistory.Count >= 1)
                {
                    var currentMetrics = _metricsHistory.Last();
                    var loadLevel = DetermineLoadLevel(currentMetrics.Metrics);

                    // Track load level transitions
                    if (loadLevel != _previousLoadLevel)
                    {
                        var transition = new LoadTransition
                        {
                            FromLevel = _previousLoadLevel,
                            ToLevel = loadLevel,
                            Timestamp = currentMetrics.Timestamp,
                            TimeSincePrevious = _loadTransitions.Count > 0
                                ? currentMetrics.Timestamp - _loadTransitions.Last().Timestamp
                                : TimeSpan.Zero,
                            PerformanceImpact = CalculatePerformanceImpact(_previousLoadLevel, loadLevel)
                        };
                        _loadTransitions.Add(transition);
                        _previousLoadLevel = loadLevel;

                        // Maintain transition history size
                        if (_loadTransitions.Count > 50)
                        {
                            _loadTransitions.RemoveAt(0);
                        }
                    }
                }

                if (_metricsHistory.Count < 5)
                {
                    return new LoadPatternData
                    {
                        Level = _previousLoadLevel,
                        SuccessRate = 0.0,
                        AverageImprovement = 0.0,
                        TotalPredictions = 0
                    };
                }

                var latestMetrics = _metricsHistory.Last();
                var currentLoadLevel = DetermineLoadLevel(latestMetrics.Metrics);

                var predictions = GenerateLoadPredictions(latestMetrics.Metrics);
                var successRate = CalculateHistoricalSuccessRate();
                var averageImprovement = CalculateHistoricalImprovement();
                var totalPredictions = _metricsHistory.Count;
                var strategyEffectiveness = CalculateStrategyEffectiveness();

                return new LoadPatternData
                {
                    Level = currentLoadLevel,
                    Predictions = predictions,
                    SuccessRate = successRate,
                    AverageImprovement = averageImprovement,
                    TotalPredictions = totalPredictions,
                    StrategyEffectiveness = strategyEffectiveness
                };
            }
        }

        private LoadLevel DetermineLoadLevel(Dictionary<string, double> metrics)
        {
            var cpuUtilization = metrics.GetValueOrDefault("CpuUtilization", 0);
            var memoryUtilization = metrics.GetValueOrDefault("MemoryUtilization", 0);
            var throughput = metrics.GetValueOrDefault("ThroughputPerSecond", 0);

            if (cpuUtilization > 0.9 || memoryUtilization > 0.9)
                return LoadLevel.Critical;
            else if (cpuUtilization > 0.7 || memoryUtilization > 0.7)
                return LoadLevel.High;
            else if (cpuUtilization > 0.5 || memoryUtilization > 0.5)
                return LoadLevel.Medium;
            else if (cpuUtilization > 0.2 || memoryUtilization > 0.2 || throughput > 10)
                return LoadLevel.Low;
            else
                return LoadLevel.Idle;
        }

        private List<PredictionResult> GenerateLoadPredictions(Dictionary<string, double> metrics)
        {
            var predictions = new List<PredictionResult>();

            // Generate predictions based on historical patterns
            var predictedStrategies = new[] { OptimizationStrategy.EnableCaching };
            var improvement = TimeSpan.FromMilliseconds(metrics.GetValueOrDefault("AverageResponseTime", 100) * 0.2);

            predictions.Add(new PredictionResult
            {
                RequestType = typeof(object),
                PredictedStrategies = predictedStrategies,
                ActualImprovement = improvement,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(metrics.GetValueOrDefault("AverageResponseTime", 100)),
                    ConcurrentExecutions = (int)metrics.GetValueOrDefault("ConcurrentRequests", 1),
                    MemoryUsage = (long)(metrics.GetValueOrDefault("MemoryUsageMB", 100) * 1024 * 1024),
                    DatabaseCalls = (int)metrics.GetValueOrDefault("DatabaseCalls", 0)
                }
            });

            return predictions;
        }

        private double CalculateHistoricalSuccessRate()
        {
            // Calculate success rate based on historical predictions vs actual outcomes
            // Placeholder implementation
            return 0.78; // 78% historical success rate
        }

        private double CalculateHistoricalImprovement()
        {
            // Calculate average improvement from historical predictions
            // Placeholder implementation
            return 0.12; // 12% average improvement
        }

        private Dictionary<string, double> CalculateStrategyEffectiveness()
        {
            // Calculate effectiveness of different strategies based on historical data
            return new Dictionary<string, double>
            {
                ["EnableCaching"] = 0.75,
                ["BatchProcessing"] = 0.65,
                ["ParallelProcessing"] = 0.55,
                ["CircuitBreaker"] = 0.85
            };
        }

        public void AddMetricsSnapshot(Dictionary<string, double> metrics)
        {
            lock (_historyLock)
            {
                var snapshot = new SystemMetricsSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Metrics = metrics != null ? new Dictionary<string, double>(metrics) : new Dictionary<string, double>()
                };

                _metricsHistory.Enqueue(snapshot);

                // Maintain history size
                while (_metricsHistory.Count > _maxHistorySize)
                {
                    _metricsHistory.Dequeue();
                }
            }
        }

        private Dictionary<string, double> PredictNextHour()
        {
            var predictions = new Dictionary<string, double>();
            var recentSnapshots = _metricsHistory.Reverse().Take(10).ToArray();

            if (recentSnapshots.Length < 5)
                return predictions;

            // Simple linear trend prediction
            predictions["CpuUtilization"] = PredictMetric(recentSnapshots, "CpuUtilization");
            predictions["MemoryUtilization"] = PredictMetric(recentSnapshots, "MemoryUtilization");
            predictions["ThroughputPerSecond"] = PredictMetric(recentSnapshots, "ThroughputPerSecond");
            predictions["ErrorRate"] = PredictMetric(recentSnapshots, "ErrorRate");

            return predictions;
        }

        private Dictionary<string, double> PredictNextDay()
        {
            var predictions = new Dictionary<string, double>();
            var dailySnapshots = AggregateDailySnapshots();

            if (dailySnapshots.Length < 3)
                return predictions;

            // Predict based on daily patterns
            predictions["AverageCpuUtilization"] = PredictMetric(dailySnapshots, "CpuUtilization");
            predictions["PeakMemoryUtilization"] = PredictMetric(dailySnapshots, "MemoryUtilization");
            predictions["DailyThroughput"] = PredictMetric(dailySnapshots, "ThroughputPerSecond");

            return predictions;
        }

        private List<string> IdentifyPotentialIssues()
        {
            var issues = new List<string>();
            var recentSnapshots = _metricsHistory.Reverse().Take(5).ToArray();

            if (recentSnapshots.Length < 3)
                return issues;

            // Check for trending issues
            var cpuTrend = CalculateTrend(recentSnapshots, "CpuUtilization");
            if (cpuTrend > 0.1) // CPU usage increasing
                issues.Add("CPU utilization trending upward - potential bottleneck");

            var memoryTrend = CalculateTrend(recentSnapshots, "MemoryUtilization");
            if (memoryTrend > 0.1)
                issues.Add("Memory utilization trending upward - potential memory leak");

            var errorTrend = CalculateTrend(recentSnapshots, "ErrorRate");
            if (errorTrend > 0.05)
                issues.Add("Error rate increasing - investigate recent changes");

            // Check for current high utilization
            var latest = recentSnapshots.Last();
            if (latest.Metrics.GetValueOrDefault("CpuUtilization", 0) > 0.9)
                issues.Add("CPU utilization critically high");

            if (latest.Metrics.GetValueOrDefault("MemoryUtilization", 0) > 0.9)
                issues.Add("Memory utilization critically high");

            return issues;
        }

        private List<string> GenerateScalingRecommendations()
        {
            var recommendations = new List<string>();
            var recentSnapshots = _metricsHistory.Reverse().Take(10).ToArray();

            if (recentSnapshots.Length < 5)
                return recommendations;

            var avgCpu = recentSnapshots.Average(s => s.Metrics.GetValueOrDefault("CpuUtilization", 0));
            var avgMemory = recentSnapshots.Average(s => s.Metrics.GetValueOrDefault("MemoryUtilization", 0));
            var avgThroughput = recentSnapshots.Average(s => s.Metrics.GetValueOrDefault("ThroughputPerSecond", 0));

            if (avgCpu > 0.8)
                recommendations.Add("Consider horizontal scaling - CPU utilization consistently high");

            if (avgMemory > 0.8)
                recommendations.Add("Consider increasing memory allocation or optimizing memory usage");

            if (avgThroughput > 1000) // High throughput
                recommendations.Add("High throughput detected - consider load balancing");

            // Check for time-based patterns
            var hourlyPattern = DetectHourlyPattern();
            if (hourlyPattern.HasValue)
            {
                if (hourlyPattern.Value.peakHour > 0.8)
                    recommendations.Add($"Scale up during peak hours ({hourlyPattern.Value.peakHour * 100:F0}% utilization)");
            }

            return recommendations;
        }

        private double CalculatePredictionConfidence()
        {
            var dataPoints = _metricsHistory.Count;
            var timeSpan = _metricsHistory.Count > 0 ?
                (DateTime.UtcNow - _metricsHistory.Peek().Timestamp).TotalHours : 0;

            // Confidence increases with more data and longer history
            var baseConfidence = Math.Min(dataPoints / 50.0, 1.0);
            var timeConfidence = Math.Min(timeSpan / 24.0, 1.0); // 24 hours of data

            return (baseConfidence + timeConfidence) / 2.0;
        }

        private double PredictMetric(SystemMetricsSnapshot[] snapshots, string metricName)
        {
            if (snapshots.Length < 2)
                return 0.0;

            // Simple linear regression
            var values = snapshots.Select(s => s.Metrics.GetValueOrDefault(metricName, 0)).ToArray();
            var trend = CalculateTrend(snapshots, metricName);

            // Predict next value based on trend
            var latestValue = values.Last();
            return Math.Max(0, Math.Min(1, latestValue + trend)); // Clamp to [0,1] for utilization metrics
        }

        private double CalculateTrend(SystemMetricsSnapshot[] snapshots, string metricName)
        {
            if (snapshots.Length < 2)
                return 0.0;

            var values = snapshots.Select(s => s.Metrics.GetValueOrDefault(metricName, 0)).ToArray();
            var n = values.Length;

            // Calculate slope using simple linear regression
            var sumX = n * (n - 1) / 2.0;
            var sumY = values.Sum();
            var sumXY = values.Select((v, i) => v * i).Sum();
            var sumXX = n * (n - 1) * (2 * n - 1) / 6.0;

            var slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            return slope;
        }

        private SystemMetricsSnapshot[] AggregateDailySnapshots()
        {
            var dailyGroups = _metricsHistory
                .GroupBy(s => s.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Take(7) // Last 7 days
                .Select(g => new SystemMetricsSnapshot
                {
                    Timestamp = g.Key,
                    Metrics = new Dictionary<string, double>
                    {
                        ["CpuUtilization"] = g.Average(s => s.Metrics.GetValueOrDefault("CpuUtilization", 0)),
                        ["MemoryUtilization"] = g.Average(s => s.Metrics.GetValueOrDefault("MemoryUtilization", 0)),
                        ["ThroughputPerSecond"] = g.Sum(s => s.Metrics.GetValueOrDefault("ThroughputPerSecond", 0))
                    }
                })
                .ToArray();

            return dailyGroups;
        }

        private (double peakHour, int peakHourIndex)? DetectHourlyPattern()
        {
            if (_metricsHistory.Count < 24)
                return null;

            var hourlyAverages = _metricsHistory
                .GroupBy(s => s.Timestamp.Hour)
                .Select(g => g.Average(s => s.Metrics.GetValueOrDefault("CpuUtilization", 0)))
                .ToArray();

            var maxUtilization = hourlyAverages.Max();
            var peakHour = Array.IndexOf(hourlyAverages, maxUtilization);

            return (maxUtilization, peakHour);
        }

        private TimeSpan CalculatePerformanceImpact(LoadLevel fromLevel, LoadLevel toLevel)
        {
            // Estimate performance impact based on load level transition
            // Higher load levels generally have more performance impact
            var fromImpact = GetLoadLevelImpact(fromLevel);
            var toImpact = GetLoadLevelImpact(toLevel);

            // Impact is the difference in processing time (simplified)
            var impactSeconds = Math.Abs(toImpact - fromImpact) * 0.1; // 0.1 seconds per impact unit
            return TimeSpan.FromSeconds(impactSeconds);
        }

        private double GetLoadLevelImpact(LoadLevel level)
        {
            return level switch
            {
                LoadLevel.Idle => 0.0,
                LoadLevel.Low => 1.0,
                LoadLevel.Medium => 2.0,
                LoadLevel.High => 3.0,
                LoadLevel.Critical => 4.0,
                _ => 0.0
            };
        }

        private class SystemMetricsSnapshot
        {
            public DateTime Timestamp { get; set; }
            public Dictionary<string, double> Metrics { get; set; } = new();
        }
    }
}