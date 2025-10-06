using Microsoft.ML.Data;

namespace Relay.Core.AI.Optimization.Models
{
    internal class StrategyPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }

        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
