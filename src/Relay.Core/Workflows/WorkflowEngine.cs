using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Workflows
{
    /// <summary>
    /// Advanced workflow engine for orchestrating multi-step business processes.
    /// </summary>
    public class WorkflowEngine : IWorkflowEngine
    {
        private readonly IRelay _relay;
        private readonly ILogger<WorkflowEngine> _logger;
        private readonly IWorkflowStateStore _stateStore;

        public WorkflowEngine(
            IRelay relay,
            ILogger<WorkflowEngine> logger,
            IWorkflowStateStore stateStore)
        {
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        }

        public async ValueTask<WorkflowExecution> StartWorkflowAsync<TInput>(
            string workflowDefinitionId,
            TInput input,
            CancellationToken cancellationToken = default)
        {
            var executionId = Guid.NewGuid().ToString();
            var execution = new WorkflowExecution
            {
                Id = executionId,
                WorkflowDefinitionId = workflowDefinitionId,
                Status = WorkflowStatus.Running,
                StartedAt = DateTime.UtcNow,
                Input = input,
                CurrentStepIndex = 0
            };

            await _stateStore.SaveExecutionAsync(execution, cancellationToken);
            _logger.LogInformation("Started workflow {WorkflowId} with execution {ExecutionId}", 
                workflowDefinitionId, executionId);

            // Start execution in background
            _ = Task.Run(async () => await ExecuteWorkflowAsync(execution, cancellationToken), cancellationToken);

            return execution;
        }

        public async ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default)
        {
            return await _stateStore.GetExecutionAsync(executionId, cancellationToken);
        }

        private async ValueTask ExecuteWorkflowAsync(WorkflowExecution execution, CancellationToken cancellationToken)
        {
            try
            {
                var definition = await GetWorkflowDefinition(execution.WorkflowDefinitionId);
                if (definition == null)
                {
                    await FailWorkflow(execution, "Workflow definition not found", cancellationToken);
                    return;
                }

                while (execution.CurrentStepIndex < definition.Steps.Count && 
                       execution.Status == WorkflowStatus.Running)
                {
                    var step = definition.Steps[execution.CurrentStepIndex];
                    await ExecuteStepAsync(execution, step, cancellationToken);
                    
                    if (execution.Status == WorkflowStatus.Running)
                    {
                        execution.CurrentStepIndex++;
                        await _stateStore.SaveExecutionAsync(execution, cancellationToken);
                    }
                }

                if (execution.Status == WorkflowStatus.Running)
                {
                    execution.Status = WorkflowStatus.Completed;
                    execution.CompletedAt = DateTime.UtcNow;
                    await _stateStore.SaveExecutionAsync(execution, cancellationToken);
                    
                    _logger.LogInformation("Completed workflow execution {ExecutionId}", execution.Id);
                }
            }
            catch (Exception ex)
            {
                await FailWorkflow(execution, ex.Message, cancellationToken);
                _logger.LogError(ex, "Workflow execution {ExecutionId} failed", execution.Id);
            }
        }

        private async ValueTask ExecuteStepAsync(WorkflowExecution execution, WorkflowStep step, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Executing step {StepName} in workflow {ExecutionId}", step.Name, execution.Id);

            var stepExecution = new WorkflowStepExecution
            {
                StepName = step.Name,
                StartedAt = DateTime.UtcNow,
                Status = StepStatus.Running
            };

            execution.StepExecutions.Add(stepExecution);

            try
            {
                switch (step.Type)
                {
                    case StepType.Request:
                        await ExecuteRequestStep(execution, step, stepExecution, cancellationToken);
                        break;
                    case StepType.Conditional:
                        await ExecuteConditionalStep(execution, step, stepExecution, cancellationToken);
                        break;
                    case StepType.Parallel:
                        await ExecuteParallelStep(execution, step, stepExecution, cancellationToken);
                        break;
                    case StepType.Wait:
                        await ExecuteWaitStep(execution, step, stepExecution, cancellationToken);
                        break;
                    default:
                        throw new NotSupportedException($"Step type {step.Type} is not supported");
                }

                stepExecution.Status = StepStatus.Completed;
                stepExecution.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                stepExecution.Status = StepStatus.Failed;
                stepExecution.Error = ex.Message;
                stepExecution.CompletedAt = DateTime.UtcNow;

                if (step.ContinueOnError)
                {
                    _logger.LogWarning(ex, "Step {StepName} failed but continuing due to ContinueOnError", step.Name);
                }
                else
                {
                    execution.Status = WorkflowStatus.Failed;
                    throw;
                }
            }
        }

        private async ValueTask ExecuteRequestStep(WorkflowExecution execution, WorkflowStep step, WorkflowStepExecution stepExecution, CancellationToken cancellationToken)
        {
            // Create request from step configuration and workflow context
            var request = CreateRequestFromStep(step, execution.Context);
            var response = await _relay.SendAsync((dynamic)request, cancellationToken);
            
            // Update workflow context with response
            execution.Context[step.OutputKey ?? step.Name] = response;
        }

        private async ValueTask ExecuteConditionalStep(WorkflowExecution execution, WorkflowStep step, WorkflowStepExecution stepExecution, CancellationToken cancellationToken)
        {
            var condition = EvaluateCondition(step.Condition, execution.Context);
            stepExecution.Output = new { ConditionResult = condition };
            
            if (!condition && step.ElseSteps?.Any() == true)
            {
                // Execute else steps
                foreach (var elseStep in step.ElseSteps)
                {
                    await ExecuteStepAsync(execution, elseStep, cancellationToken);
                }
            }
        }

        private async ValueTask ExecuteParallelStep(WorkflowExecution execution, WorkflowStep step, WorkflowStepExecution stepExecution, CancellationToken cancellationToken)
        {
            if (step.ParallelSteps == null) return;

            var tasks = step.ParallelSteps.Select(parallelStep => 
                ExecuteStepAsync(execution, parallelStep, cancellationToken).AsTask());

            await Task.WhenAll(tasks);
        }

        private async ValueTask ExecuteWaitStep(WorkflowExecution execution, WorkflowStep step, WorkflowStepExecution stepExecution, CancellationToken cancellationToken)
        {
            var waitTimeMs = step.WaitTimeMs ?? 1000;
            await Task.Delay(waitTimeMs, cancellationToken);
        }

        private object CreateRequestFromStep(WorkflowStep step, Dictionary<string, object> context)
        {
            // Create request instance from step configuration
            // This would need proper implementation based on step.RequestType
            throw new NotImplementedException("Request creation from step configuration needs implementation");
        }

        private bool EvaluateCondition(string? condition, Dictionary<string, object> context)
        {
            // Simple condition evaluation - in real implementation, this could use expression trees
            if (string.IsNullOrEmpty(condition)) return true;
            
            // For now, just return true
            return true;
        }

        private async ValueTask<WorkflowDefinition?> GetWorkflowDefinition(string definitionId)
        {
            // This would load workflow definition from storage
            // For now, return a simple definition
            return new WorkflowDefinition
            {
                Id = definitionId,
                Name = "Sample Workflow",
                Steps = new List<WorkflowStep>()
            };
        }

        private async ValueTask FailWorkflow(WorkflowExecution execution, string error, CancellationToken cancellationToken)
        {
            execution.Status = WorkflowStatus.Failed;
            execution.Error = error;
            execution.CompletedAt = DateTime.UtcNow;
            await _stateStore.SaveExecutionAsync(execution, cancellationToken);
        }
    }

    /// <summary>
    /// Interface for workflow engine operations.
    /// </summary>
    public interface IWorkflowEngine
    {
        ValueTask<WorkflowExecution> StartWorkflowAsync<TInput>(string workflowDefinitionId, TInput input, CancellationToken cancellationToken = default);
        ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for workflow state storage.
    /// </summary>
    public interface IWorkflowStateStore
    {
        ValueTask SaveExecutionAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
        ValueTask<WorkflowExecution?> GetExecutionAsync(string executionId, CancellationToken cancellationToken = default);
    }

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

    /// <summary>
    /// Workflow definition.
    /// </summary>
    public class WorkflowDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<WorkflowStep> Steps { get; set; } = new();
    }

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

    /// <summary>
    /// Workflow execution status.
    /// </summary>
    public enum WorkflowStatus
    {
        Running,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Step types.
    /// </summary>
    public enum StepType
    {
        Request,
        Conditional,
        Parallel,
        Wait
    }

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
}