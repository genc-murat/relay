using System;
using System.Collections.Generic;

namespace Relay.Core.AI.Models;

/// <summary>
/// Represents a snapshot of system metrics at a specific point in time
/// </summary>
public class SystemMetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}