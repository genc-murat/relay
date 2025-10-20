namespace Relay.Core.AI.Analysis.Engines
{
    /// <summary>
    /// Thresholds for load classification
    /// </summary>
    public class LoadThresholds
    {
        /// <summary>
        /// Medium load threshold (concurrent executions)
        /// </summary>
        public int MediumLoad { get; set; } = 50;

        /// <summary>
        /// High load threshold (concurrent executions)
        /// </summary>
        public int HighLoad { get; set; } = 100;
    }
}