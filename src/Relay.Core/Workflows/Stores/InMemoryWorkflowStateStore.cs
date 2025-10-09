using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows.Stores;

/// <summary>
/// In-memory implementation of IWorkflowStateStore for testing and development.
/// </summary>
public class InMemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new();

    /// <inheritdoc />
    public ValueTask SaveExecutionAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        if (string.IsNullOrWhiteSpace(execution.Id))
        {
            throw new ArgumentException("Execution Id cannot be empty", nameof(execution));
        }

        // Clone the execution to prevent external modifications
        var clonedExecution = CloneExecution(execution);
        _executions[execution.Id] = clonedExecution;

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            return ValueTask.FromResult<WorkflowExecution?>(null);
        }

        if (_executions.TryGetValue(executionId, out var execution))
        {
            // Clone to prevent external modifications
            return ValueTask.FromResult<WorkflowExecution?>(CloneExecution(execution));
        }

        return ValueTask.FromResult<WorkflowExecution?>(null);
    }

    /// <summary>
    /// Clears all executions (for testing purposes).
    /// </summary>
    public void Clear()
    {
        _executions.Clear();
    }

    /// <summary>
    /// Gets the count of stored executions.
    /// </summary>
    public int Count => _executions.Count;

    private static WorkflowExecution CloneExecution(WorkflowExecution source)
    {
        return new WorkflowExecution
        {
            Id = source.Id,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            Status = source.Status,
            StartedAt = source.StartedAt,
            CompletedAt = source.CompletedAt,
            Input = source.Input,
            Output = source.Output,
            Error = source.Error,
            CurrentStepIndex = source.CurrentStepIndex,
            Context = new System.Collections.Generic.Dictionary<string, object>(source.Context),
            StepExecutions = new System.Collections.Generic.List<WorkflowStepExecution>(
                source.StepExecutions.Select(CloneStepExecution))
        };
    }

    private static WorkflowStepExecution CloneStepExecution(WorkflowStepExecution source)
    {
        return new WorkflowStepExecution
        {
            StepName = source.StepName,
            Status = source.Status,
            StartedAt = source.StartedAt,
            CompletedAt = source.CompletedAt,
            Output = source.Output,
            Error = source.Error
        };
    }
}
