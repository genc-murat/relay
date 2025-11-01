using System.Collections.Generic;

namespace Relay.Core.Workflows;

/// <summary>
/// Workflow step definition.
/// </summary>
public class WorkflowStep
{
    public string Name { get; set; } = string.Empty;
    public StepType Type { get; set; }
    public string? RequestType { get; set; }
    public string? OutputKey { get; set; }
    public string? Condition { get; set; }
    public bool ContinueOnError { get; set; }
    public int? WaitTimeMs { get; set; }
    public List<WorkflowStep>? ParallelSteps { get; set; }
    public List<WorkflowStep>? ElseSteps { get; set; }
}
