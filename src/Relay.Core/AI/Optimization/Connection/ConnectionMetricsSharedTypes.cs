using System;

namespace Relay.Core.AI.Optimization.Connection;

internal enum LoadLevel
{
    Idle,
    Low,
    Medium,
    High,
    Critical
}

internal class LoadBalancerComponent
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Description { get; set; } = string.Empty;
}

internal class TechnologyTrendComponent
{
    public string Name { get; set; } = string.Empty;
    public double Factor { get; set; }
    public double Weight { get; set; }
}

internal class MemoryPressureFactor
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Weight { get; set; }
}