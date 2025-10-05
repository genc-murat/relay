namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationStatistics
    {
        public int VectorOperations { get; set; }
        public int ScalarOperations { get; set; }
        public int TotalElementsProcessed { get; set; }
        public long DataProcessed { get; set; }
        public double VectorizationRatio { get; set; }
        public double VectorizedDataPercentage { get; set; }
        public double EstimatedSpeedup { get; set; }
    }
}
