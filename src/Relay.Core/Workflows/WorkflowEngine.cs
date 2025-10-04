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
            if (string.IsNullOrWhiteSpace(step.RequestType))
                throw new ArgumentException("RequestType is required for request steps", nameof(step));

            // Try to find the request type in loaded assemblies
            var requestType = FindRequestType(step.RequestType);
            if (requestType == null)
            {
                throw new InvalidOperationException($"Request type '{step.RequestType}' not found in loaded assemblies");
            }

            try
            {
                // Create an instance of the request
                var request = Activator.CreateInstance(requestType);
                if (request == null)
                {
                    throw new InvalidOperationException($"Failed to create instance of request type '{step.RequestType}'");
                }

                // Populate request properties from workflow context using reflection
                var properties = requestType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (!property.CanWrite) continue;

                    // Try to find matching value in context
                    var contextKey = property.Name;
                    if (context.TryGetValue(contextKey, out var value))
                    {
                        try
                        {
                            // Convert and set the value
                            var convertedValue = Convert.ChangeType(value, property.PropertyType);
                            property.SetValue(request, convertedValue);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to set property {PropertyName} on request type {RequestType}", 
                                property.Name, step.RequestType);
                        }
                    }
                }

                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create request from step configuration for type {RequestType}", step.RequestType);
                throw new InvalidOperationException($"Failed to create request instance: {ex.Message}", ex);
            }
        }

        private Type? FindRequestType(string requestTypeName)
        {
            // Try to find the type in all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    var requestType = types.FirstOrDefault(t => 
                        t.Name.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) ||
                        t.FullName?.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) == true);

                    if (requestType != null)
                    {
                        // Verify it implements IRequest or IRequest<T>
                        var isRequest = requestType.GetInterfaces().Any(i => 
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>) ||
                            i == typeof(IRequest));

                        if (isRequest)
                        {
                            return requestType;
                        }
                    }
                }
                catch
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            return null;
        }

        private bool EvaluateCondition(string? condition, Dictionary<string, object> context)
        {
            if (string.IsNullOrWhiteSpace(condition)) 
                return true;

            try
            {
                // Simple condition evaluation using basic operators
                // Format: "key operator value" or "key" (checks if key exists and is truthy)
                var trimmedCondition = condition.Trim();

                // Check for comparison operators
                var operators = new[] { "==", "!=", ">", "<", ">=", "<=", "contains", "startswith", "endswith" };
                
                foreach (var op in operators)
                {
                    var parts = trimmedCondition.Split(new[] { $" {op} " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var expectedValue = parts[1].Trim().Trim('"', '\'');

                        if (!context.TryGetValue(key, out var actualValue))
                            return false;

                        return EvaluateComparison(actualValue, op, expectedValue);
                    }
                }

                // Simple boolean check - just check if key exists and is truthy
                if (context.TryGetValue(trimmedCondition, out var value))
                {
                    if (value is bool boolValue)
                        return boolValue;
                    
                    if (value is string stringValue)
                        return !string.IsNullOrWhiteSpace(stringValue);
                    
                    return value != null;
                }

                // Default to true if condition cannot be evaluated
                _logger.LogWarning("Could not evaluate condition: {Condition}. Defaulting to true.", condition);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evaluating condition: {Condition}. Defaulting to true.", condition);
                return true;
            }
        }

        private bool EvaluateComparison(object actualValue, string op, string expectedValue)
        {
            var actualString = actualValue?.ToString() ?? string.Empty;

            switch (op.ToLowerInvariant())
            {
                case "==":
                    return actualString.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
                
                case "!=":
                    return !actualString.Equals(expectedValue, StringComparison.OrdinalIgnoreCase);
                
                case "contains":
                    return actualString.Contains(expectedValue, StringComparison.OrdinalIgnoreCase);
                
                case "startswith":
                    return actualString.StartsWith(expectedValue, StringComparison.OrdinalIgnoreCase);
                
                case "endswith":
                    return actualString.EndsWith(expectedValue, StringComparison.OrdinalIgnoreCase);
                
                case ">":
                case "<":
                case ">=":
                case "<=":
                    if (double.TryParse(actualString, out var actualNum) && 
                        double.TryParse(expectedValue, out var expectedNum))
                    {
                        return op switch
                        {
                            ">" => actualNum > expectedNum,
                            "<" => actualNum < expectedNum,
                            ">=" => actualNum >= expectedNum,
                            "<=" => actualNum <= expectedNum,
                            _ => false
                        };
                    }
                    return false;
                
                default:
                    return false;
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators
        private async ValueTask<WorkflowDefinition?> GetWorkflowDefinition(string definitionId)
        {
            // TODO: This would load workflow definition from storage
            // For now, return a simple definition
            return new WorkflowDefinition
            {
                Id = definitionId,
                Name = "Sample Workflow",
                Steps = new List<WorkflowStep>()
            };
        }
#pragma warning restore CS1998

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