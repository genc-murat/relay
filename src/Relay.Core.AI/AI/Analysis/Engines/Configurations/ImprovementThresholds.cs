namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Thresholds for classifying improvement impact
    /// </summary>
    public class ImprovementThresholds
    {
        /// <summary>
        /// Low impact threshold (milliseconds)
        /// </summary>
        public int LowImpact { get; set; } = 50;

        /// <summary>
        /// High impact threshold (milliseconds)
        /// </summary>
        public int HighImpact { get; set; } = 100;
    }
}