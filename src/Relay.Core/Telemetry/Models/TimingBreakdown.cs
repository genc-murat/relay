using System;
using System.Collections.Generic;

namespace Relay.Core.Telemetry;

/// <summary>
/// Detailed timing breakdown for an operation
/// </summary>
public class TimingBreakdown
{
    public string OperationId { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, TimeSpan> PhaseTimings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}