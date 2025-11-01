using System;

namespace Relay.Core.Workflows;

/// <summary>
/// Step execution state.
/// </summary>
public class WorkflowStepExecution
{
    public string StepName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
}
