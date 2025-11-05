using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services
{
    /// <summary>
    /// Configuration options for metrics collection
    /// </summary>
    public class MetricsCollectionOptions
    {
        /// <summary>
        /// Which collectors to enable
        /// </summary>
        public HashSet<string> EnabledCollectors { get; set; } = new()
        {
            "CpuMetricsCollector",
            "MemoryMetricsCollector",
            "NetworkMetricsCollector",
            "DiskMetricsCollector",
            "SystemLoadMetricsCollector"
        };

        /// <summary>
        /// Global collection interval
        /// </summary>
        public TimeSpan DefaultCollectionInterval { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Specific collection intervals per collector (overrides default)
        /// </summary>
        public Dictionary<string, TimeSpan> CollectorIntervals { get; set; } = new();

        /// <summary>
        /// Maximum number of historical metric values to keep
        /// </summary>
        public int MaxHistorySize { get; set; } = 1000;

        /// <summary>
        /// Whether to enable real-time metrics publishing
        /// </summary>
        public bool EnableRealTimePublishing { get; set; } = true;

        /// <summary>
        /// Whether to enable metrics aggregation
        /// </summary>
        public bool EnableAggregation { get; set; } = true;

        /// <summary>
        /// Aggregation window for statistical calculations
        /// </summary>
        public TimeSpan AggregationWindow { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Whether to enable health scoring
        /// </summary>
        public bool EnableHealthScoring { get; set; } = true;

        /// <summary>
        /// Health score calculation interval
        /// </summary>
        public TimeSpan HealthScoreInterval { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable predictive analysis
        /// </summary>
        public bool EnablePredictiveAnalysis { get; set; } = true;

        /// <summary>
        /// Prediction analysis interval
        /// </summary>
        public TimeSpan PredictionAnalysisInterval { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Options for health scoring
    /// </summary>
    public class HealthScoringOptions
    {
        /// <summary>
        /// Weights for different health aspects
        /// </summary>
        public HealthWeights Weights { get; set; } = new();

        /// <summary>
        /// Thresholds for health status determination
        /// </summary>
        public HealthThresholds Thresholds { get; set; } = new();
    }

    /// <summary>
    /// Weights for health score calculation
    /// </summary>
    public class HealthWeights
    {
        public double Performance { get; set; } = 0.25;
        public double Reliability { get; set; } = 0.25;
        public double Scalability { get; set; } = 0.20;
        public double Security { get; set; } = 0.20;
        public double Maintainability { get; set; } = 0.10;
    }

    /// <summary>
    /// Thresholds for health status levels
    /// </summary>
    public class HealthThresholds
    {
        public double Excellent { get; set; } = 0.9;
        public double Good { get; set; } = 0.8;
        public double Fair { get; set; } = 0.7;
        public double Poor { get; set; } = 0.6;
        // Below Poor is Critical
    }
}