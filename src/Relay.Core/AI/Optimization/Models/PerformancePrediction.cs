using Microsoft.ML.Data;

namespace Relay.Core.AI
{
    internal class PerformancePrediction
    {
        [ColumnName("Score")]
        public float PredictedGain { get; set; }
    }
}
