using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows;

/// <summary>
/// Interface for workflow engine operations.
/// </summary>
public interface IWorkflowEngine
{
    ValueTask<WorkflowExecution> StartWorkflowAsync<TInput>(string workflowDefinitionId, TInput input, CancellationToken cancellationToken = default);
    ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default);
}
