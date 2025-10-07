using System;

namespace Relay.Core.Telemetry;

/// <summary>
/// Configuration options for the enhanced metrics provider
/// </summary>
public class MetricsProviderOptions
{
    /// <summary>
    /// Threshold for detecting slow executions (multiplier of average)
    /// </summary>
    public double SlowExecutionThreshold { get; set; } = 2.0;

    /// <summary>
    /// Threshold for detecting high failure rates (percentage)
    /// </summary>
    public double HighFailureRateThreshold { get; set; } = 0.1;

    /// <summary>
    /// Time window for anomaly detection
    /// </summary>
    public TimeSpan AnomalyDetectionWindow { get; set; } = TimeSpan.FromMinutes(15);



    /// <summary>
    /// Enable advanced anomaly detection features
    /// </summary>
    public bool EnableAdvancedAnomalyDetection { get; set; } = true;

    /// <summary>
    /// Minimum number of executions required for anomaly detection
    /// </summary>
    public int MinExecutionsForAnomalyDetection { get; set; } = 10;

    /// <summary>
    /// Timeout threshold for detecting timeout exceeded anomalies
    /// </summary>
    public TimeSpan TimeoutThreshold { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Minimum sample size for anomaly detection
    /// </summary>
    public int MinSampleSizeForAnomalyDetection { get; set; } = 10;

    /// <summary>
    /// Number of recent executions to check for anomalies
    /// </summary>
    public int RecentExecutionsForAnomalyCheck { get; set; } = 5;

    /// <summary>
    /// Enable real-time anomaly detection
    /// </summary>
    public bool EnableRealTimeAnomalyDetection { get; set; } = true;

    /// <summary>
    /// Maximum number of records per handler
    /// </summary>
    public int MaxRecordsPerHandler { get; set; } = 1000;

    /// <summary>
    /// Maximum number of timing breakdowns to keep
    /// </summary>
    public int MaxTimingBreakdowns { get; set; } = 10000;
}