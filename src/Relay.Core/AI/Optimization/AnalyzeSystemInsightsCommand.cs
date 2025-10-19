namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command for analyzing system insights.
    /// </summary>
    public class AnalyzeSystemInsightsCommand : OptimizationCommandBase
    {
        public AnalyzeSystemInsightsCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze system-wide metrics and provide global optimization insights";

        public override bool CanUndo => false; // Analysis doesn't change state
    }
}