using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Service for generating predictive analysis of system behavior
    /// </summary>
    internal class PredictiveAnalysisService
    {
        private readonly ILogger _logger;
        private readonly Queue<SystemMetricsSnapshot> _metricsHistory = new();
        private readonly int _maxHistorySize = 100;
        private readonly object _historyLock = new();

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

        public void AddMetricsSnapshot(Dictionary<string, double> metrics)
        {
            lock (_historyLock)
            {
                var snapshot = new SystemMetricsSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    Metrics = new Dictionary<string, double>(metrics)
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

        private class SystemMetricsSnapshot
        {
            public DateTime Timestamp { get; set; }
            public Dictionary<string, double> Metrics { get; set; } = new();
        }
    }
}