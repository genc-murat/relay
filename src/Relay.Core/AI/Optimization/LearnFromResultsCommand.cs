namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command for learning from optimization results.
    /// </summary>
    public class LearnFromResultsCommand : OptimizationCommandBase
    {
        public LearnFromResultsCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Learn from past optimization results to improve future recommendations";

        public override bool CanUndo => false; // Learning updates internal state
    }
}