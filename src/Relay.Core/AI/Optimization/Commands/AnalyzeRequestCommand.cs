namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command for analyzing request patterns.
    /// </summary>
    public class AnalyzeRequestCommand : OptimizationCommandBase
    {
        public AnalyzeRequestCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze request execution patterns and provide optimization recommendations";

        public override bool CanUndo => false; // Analysis doesn't change state
    }
}