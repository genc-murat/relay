using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows;

/// <summary>
/// In-memory implementation of workflow definition store for testing and simple scenarios.
/// </summary>
public class InMemoryWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = new();
    private readonly object _lock = new();

    public ValueTask<WorkflowDefinition?> GetDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _definitions.TryGetValue(definitionId, out var definition);
            return ValueTask.FromResult(definition);
        }
    }

    public ValueTask SaveDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (string.IsNullOrWhiteSpace(definition.Id))
            throw new ArgumentException("Workflow definition must have an Id", nameof(definition));

        lock (_lock)
        {
            _definitions[definition.Id] = definition;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IEnumerable<WorkflowDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var allDefinitions = _definitions.Values.ToList();
            return ValueTask.FromResult<IEnumerable<WorkflowDefinition>>(allDefinitions);
        }
    }

    public ValueTask<bool> DeleteDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(definitionId))
            return ValueTask.FromResult(false);

        lock (_lock)
        {
            var removed = _definitions.Remove(definitionId);
            return ValueTask.FromResult(removed);
        }
    }
}
