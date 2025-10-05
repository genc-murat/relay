namespace Relay.Core.AI
{
    /// <summary>
    /// Risk assessment result
    /// </summary>
    internal class RiskAssessmentResult
    {
        public RiskLevel RiskLevel { get; set; }
        public double AdjustedConfidence { get; set; }
    }
}
