using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows;

/// <summary>
/// Interface for workflow state storage.
/// </summary>
public interface IWorkflowStateStore
{
    ValueTask SaveExecutionAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default);
}
