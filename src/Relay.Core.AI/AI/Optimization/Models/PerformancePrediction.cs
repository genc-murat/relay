using Microsoft.ML.Data;

namespace Relay.Core.AI.Optimization.Models
{
    internal class PerformancePrediction
    {
        [ColumnName("Score")]
        public float PredictedGain { get; set; }
    }
}
