using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows.Infrastructure;

/// <summary>
/// EF Core implementation of IWorkflowStateStore using database persistence.
/// </summary>
public class EfCoreWorkflowStateStore : IWorkflowStateStore
{
    private readonly WorkflowDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreWorkflowStateStore"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfCoreWorkflowStateStore(WorkflowDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async ValueTask SaveExecutionAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        if (execution == null)
        {
            throw new ArgumentNullException(nameof(execution));
        }

        if (string.IsNullOrWhiteSpace(execution.Id))
        {
            throw new ArgumentException("Execution Id cannot be empty", nameof(execution));
        }

        var entity = await _context.WorkflowExecutions
            .FirstOrDefaultAsync(e => e.Id == execution.Id, cancellationToken);

        if (entity == null)
        {
            // Create new entity
            entity = new WorkflowExecutionEntity
            {
                Id = execution.Id,
                Version = 0
            };
            _context.WorkflowExecutions.Add(entity);
        }
        else
        {
            // Update existing - increment version for optimistic concurrency
            entity.Version++;
        }

        // Map properties
        entity.WorkflowDefinitionId = execution.WorkflowDefinitionId;
        entity.Status = execution.Status.ToString();
        entity.StartedAt = execution.StartedAt;
        entity.CompletedAt = execution.CompletedAt;
        entity.InputData = execution.Input != null ? JsonSerializer.Serialize(execution.Input, _jsonOptions) : null;
        entity.OutputData = execution.Output != null ? JsonSerializer.Serialize(execution.Output, _jsonOptions) : null;
        entity.Error = execution.Error;
        entity.CurrentStepIndex = execution.CurrentStepIndex;
        entity.ContextData = execution.Context.Count > 0 ? JsonSerializer.Serialize(execution.Context, _jsonOptions) : null;
        entity.StepExecutionsData = execution.StepExecutions.Count > 0 ? JsonSerializer.Serialize(execution.StepExecutions, _jsonOptions) : null;
        entity.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException($"Concurrent update detected for workflow execution {execution.Id}");
        }
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executionId))
        {
            return null;
        }

        var entity = await _context.WorkflowExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return MapToExecution(entity);
    }

    private WorkflowExecution MapToExecution(WorkflowExecutionEntity entity)
    {
        var execution = new WorkflowExecution
        {
            Id = entity.Id,
            WorkflowDefinitionId = entity.WorkflowDefinitionId,
            Status = Enum.Parse<WorkflowStatus>(entity.Status),
            StartedAt = entity.StartedAt,
            CompletedAt = entity.CompletedAt,
            Error = entity.Error,
            CurrentStepIndex = entity.CurrentStepIndex
        };

        // Deserialize input
        if (!string.IsNullOrWhiteSpace(entity.InputData))
        {
            try
            {
                execution.Input = JsonSerializer.Deserialize<object>(entity.InputData, _jsonOptions);
            }
            catch
            {
                // Input deserialization failed, leave as null
            }
        }

        // Deserialize output
        if (!string.IsNullOrWhiteSpace(entity.OutputData))
        {
            try
            {
                execution.Output = JsonSerializer.Deserialize<object>(entity.OutputData, _jsonOptions);
            }
            catch
            {
                // Output deserialization failed, leave as null
            }
        }

        // Deserialize context
        if (!string.IsNullOrWhiteSpace(entity.ContextData))
        {
            try
            {
                var context = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(entity.ContextData, _jsonOptions);
                if (context != null)
                {
                    execution.Context = context;
                }
            }
            catch
            {
                // Context deserialization failed, use empty dictionary
            }
        }

        // Deserialize step executions
        if (!string.IsNullOrWhiteSpace(entity.StepExecutionsData))
        {
            try
            {
                var stepExecutions = JsonSerializer.Deserialize<System.Collections.Generic.List<WorkflowStepExecution>>(entity.StepExecutionsData, _jsonOptions);
                if (stepExecutions != null)
                {
                    execution.StepExecutions = stepExecutions;
                }
            }
            catch
            {
                // Step executions deserialization failed, use empty list
            }
        }

        return execution;
    }
}
