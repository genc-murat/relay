using System;

namespace Relay.Core.AI
{
    internal class MetricForecast
    {
        public float[] ForecastedValues { get; set; } = Array.Empty<float>();
        public float[] LowerBound { get; set; } = Array.Empty<float>();
        public float[] UpperBound { get; set; } = Array.Empty<float>();
    }
}
