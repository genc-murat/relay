using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Machine learning enhancement result
    /// </summary>
    internal class MachineLearningEnhancement
    {
        public OptimizationStrategy AlternativeStrategy { get; set; }
        public double EnhancedConfidence { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }
}
