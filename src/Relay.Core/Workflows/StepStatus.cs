namespace Relay.Core.Workflows;

/// <summary>
/// Step execution status.
/// </summary>
public enum StepStatus
{
    Running,
    Completed,
    Failed,
    Skipped
}