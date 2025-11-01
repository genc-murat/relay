using System;
using System.Collections.Generic;

namespace Relay.Core.Workflows;

/// <summary>
/// Workflow execution state.
/// </summary>
public class WorkflowExecution
{
    public string Id { get; set; } = string.Empty;
    public string WorkflowDefinitionId { get; set; } = string.Empty;
    public WorkflowStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public object? Input { get; set; }
    public object? Output { get; set; }
    public string? Error { get; set; }
    public int CurrentStepIndex { get; set; }
    public Dictionary<string, object> Context { get; set; } = new();
    public List<WorkflowStepExecution> StepExecutions { get; set; } = new();
}
