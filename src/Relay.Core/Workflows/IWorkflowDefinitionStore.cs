using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows;

/// <summary>
/// Interface for workflow definition storage.
/// </summary>
public interface IWorkflowDefinitionStore
{
    ValueTask<WorkflowDefinition?> GetDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
    ValueTask SaveDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    ValueTask<IEnumerable<WorkflowDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteDefinitionAsync(string definitionId, CancellationToken cancellationToken = default);
}
