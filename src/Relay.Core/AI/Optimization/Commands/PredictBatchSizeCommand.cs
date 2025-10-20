namespace Relay.Core.AI.Optimization
{
    /// <summary>
    /// Command for predicting optimal batch sizes.
    /// </summary>
    public class PredictBatchSizeCommand : OptimizationCommandBase
    {
        public PredictBatchSizeCommand(OptimizationEngine engine, OptimizationContext context)
            : base(engine, context)
        {
        }

        public override string Description => "Predict optimal batch size based on system load and request characteristics";

        public override bool CanUndo => false; // Prediction doesn't change state
    }
}