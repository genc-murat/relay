using Relay.Core.AI.Optimization.Data;
using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// System behavior prediction
/// </summary>
public class SystemPrediction
{
    public DateTime PredictionTime { get; set; }
    public Dictionary<string, double> PredictedMetrics { get; set; } = new();
    public LoadLevel PredictedLoadLevel { get; set; }
    public double Confidence { get; set; }
    public IEnumerable<string> Assumptions { get; set; } = Array.Empty<string>();
}
