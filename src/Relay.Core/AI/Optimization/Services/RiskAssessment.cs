using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Optimization.Services;

public class RiskAssessment
{
    public OptimizationStrategy Strategy { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public List<string> RiskFactors { get; set; } = new();
    public List<string> MitigationStrategies { get; set; } = new();
    public double AssessmentConfidence { get; set; }
    public DateTime LastAssessment { get; set; }
}