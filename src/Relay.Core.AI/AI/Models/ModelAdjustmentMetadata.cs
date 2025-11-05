using System;

namespace Relay.Core.AI
{

    /// <summary>
    /// Model adjustment metadata for audit trail
    /// </summary>
    internal class ModelAdjustmentMetadata
    {
        public DateTime Timestamp { get; set; }
        public double AdjustmentFactor { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string TriggeredBy { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public double AccuracyBeforeAdjustment { get; set; }
        public long TotalPredictions { get; set; }
        public long CorrectPredictions { get; set; }
    }
}
