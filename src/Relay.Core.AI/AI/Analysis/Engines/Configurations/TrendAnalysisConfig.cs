namespace Relay.Core.AI
{
    /// <summary>
    /// Configuration options for trend analysis
    /// </summary>
    public class TrendAnalysisConfig
    {
        /// <summary>
        /// Moving average periods to calculate
        /// </summary>
        public int[] MovingAveragePeriods { get; set; } = { 5, 15, 60 };

        /// <summary>
        /// Alpha value for exponential moving average
        /// </summary>
        public double ExponentialMovingAverageAlpha { get; set; } = 0.3;

        /// <summary>
        /// Z-score threshold for anomaly detection
        /// </summary>
        public double AnomalyZScoreThreshold { get; set; } = 3.0;

        /// <summary>
        /// High anomaly Z-score threshold
        /// </summary>
        public double HighAnomalyZScoreThreshold { get; set; } = 4.0;

        /// <summary>
        /// Correlation threshold for identifying relationships
        /// </summary>
        public double CorrelationThreshold { get; set; } = 0.7;

        /// <summary>
        /// Velocity threshold for logging high velocity changes
        /// </summary>
        public double HighVelocityThreshold { get; set; } = 0.1;
    }
}