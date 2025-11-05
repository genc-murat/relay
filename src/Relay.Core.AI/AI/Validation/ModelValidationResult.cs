using System;

namespace Relay.Core.AI
{
    public class ModelValidationResult
    {
        public bool IsHealthy { get; set; }
        public double OverallScore { get; set; }
        public ModelValidationIssue[] Issues { get; set; } = Array.Empty<ModelValidationIssue>();
        public DateTime ValidationTime { get; set; }
        public AIModelStatistics ModelStatistics { get; set; } = null!;
    }
}