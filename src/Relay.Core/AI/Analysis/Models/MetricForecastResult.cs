using System;
using Microsoft.ML.Data;

namespace Relay.Core.AI
{
    /// <summary>
    /// Forecast result from ML.NET time-series model
    /// </summary>
    internal class MetricForecastResult
    {
        [VectorType]
        public float[] ForecastedValues { get; set; } = Array.Empty<float>();
        
        [VectorType]
        public float[] LowerBound { get; set; } = Array.Empty<float>();
        
        [VectorType]
        public float[] UpperBound { get; set; } = Array.Empty<float>();
    }
}
