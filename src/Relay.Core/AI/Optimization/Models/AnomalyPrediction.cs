using Microsoft.ML.Data;

namespace Relay.Core.AI
{
    internal class AnomalyPrediction
    {
        [VectorType(3)]
        public double[] Prediction { get; set; } = new double[3];
    }
}
