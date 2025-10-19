namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command for optimizing caching configuration.
    /// </summary>
    public class OptimizeCachingCommand : OptimizationCommandBase
    {
        public OptimizeCachingCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Analyze and optimize caching configuration for better performance";

        public override bool CanUndo => true; // Could potentially revert cache settings
    }
}