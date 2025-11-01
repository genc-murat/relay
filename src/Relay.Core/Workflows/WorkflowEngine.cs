using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Workflows;

/// <summary>
/// Advanced workflow engine for orchestrating multi-step business processes.
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly IRelay _relay;
    private readonly ILogger<WorkflowEngine> _logger;
    private readonly IWorkflowStateStore _stateStore;
    private readonly IWorkflowDefinitionStore _definitionStore;

    public WorkflowEngine(
        IRelay relay,
        ILogger<WorkflowEngine> logger,
        IWorkflowStateStore stateStore,
        IWorkflowDefinitionStore definitionStore)
    {
        _relay = relay ?? throw new ArgumentNullException(nameof(relay));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _definitionStore = definitionStore ?? throw new ArgumentNullException(nameof(definitionStore));
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
            // Populate context from input object properties
            if (execution.Input != null)
            {
                // Handle Dictionary<string, object> input specially
                if (execution.Input is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        // Include null values in context - they may be needed for condition evaluation
                        execution.Context[kvp.Key] = kvp.Value!;
                    }
                }
                else
                {
                    // For regular objects, use reflection to get properties
                    var inputType = execution.Input.GetType();
                    var properties = inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var property in properties)
                    {
                        if (property.CanRead)
                        {
                            var value = property.GetValue(execution.Input);
                            // Include null values in context - they may be needed for condition evaluation
                            execution.Context[property.Name] = value!;
                        }
                    }
                }
            }

            var definition = await GetWorkflowDefinition(execution.WorkflowDefinitionId);
            if (definition == null)
            {
                await FailWorkflow(execution, "Workflow definition not found", cancellationToken);
                return;
            }

            if (definition.Steps.Count == 0)
            {
                await FailWorkflow(execution, "Workflow definition must have at least one step", cancellationToken);
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
            // Only fail the workflow if it's not already failed
            if (execution.Status != WorkflowStatus.Failed)
            {
                await FailWorkflow(execution, ex.Message, cancellationToken);
            }
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
                // Save the execution state with the failed step
                await _stateStore.SaveExecutionAsync(execution, cancellationToken);
                return; // Don't continue to the completion code below
            }
            else
            {
                execution.Status = WorkflowStatus.Failed;
                execution.Error = ex.Message;
                execution.CompletedAt = DateTime.UtcNow;
                await _stateStore.SaveExecutionAsync(execution, cancellationToken);
                // Don't re-throw, let the workflow execution loop handle the failure
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

        // Check if any parallel step failed after execution
        var failedStepExecutions = execution.StepExecutions
            .Where(se => step.ParallelSteps.Any(ps => ps.Name == se.StepName) && se.Status == StepStatus.Failed)
            .ToList();

        if (failedStepExecutions.Any())
        {
            var errorMessages = failedStepExecutions.Select(se => $"{se.StepName}: {se.Error}");
            var errorMessage = string.Join("; ", errorMessages);
            
            // Fail the workflow if any parallel step failed and ContinueOnError is not set
            if (!step.ContinueOnError)
            {
                execution.Status = WorkflowStatus.Failed;
                execution.Error = $"One or more parallel steps failed: {errorMessage}";
                execution.CompletedAt = DateTime.UtcNow;
                await _stateStore.SaveExecutionAsync(execution, cancellationToken);
                throw new InvalidOperationException(execution.Error);
            }
            else
            {
                _logger.LogWarning("One or more parallel steps failed but continuing due to ContinueOnError: {ErrorMessage}", errorMessage);
            }
        }
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
        // First, try to get the type using Type.GetType which can resolve well-known types
        var requestType = Type.GetType(requestTypeName);
        
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
            else
            {
                // Type found but doesn't implement IRequest
                throw new InvalidOperationException($"Type '{requestTypeName}' does not implement IRequest or IRequest<T>");
            }
        }
        
        // If Type.GetType didn't work, try searching in loaded assemblies
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                var foundType = types.FirstOrDefault(t =>
                    t.Name.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) ||
                    t.FullName?.Equals(requestTypeName, StringComparison.OrdinalIgnoreCase) == true);

                if (foundType != null)
                {
                    // Verify it implements IRequest or IRequest<T>
                    var isRequest = foundType.GetInterfaces().Any(i =>
                        i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>) ||
                        i == typeof(IRequest));

                    if (isRequest)
                    {
                        return foundType;
                    }
                    else
                    {
                        // Type found but doesn't implement IRequest
                        throw new InvalidOperationException($"Type '{requestTypeName}' does not implement IRequest or IRequest<T>");
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be loaded properly
                continue;
            }
            catch
            {
                // Skip assemblies that cause other errors
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

                // If value is null, default to true (graceful handling)
                if (value == null)
                    return true;

                return true;
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

    private async ValueTask<WorkflowDefinition?> GetWorkflowDefinition(string definitionId)
    {
        try
        {
            var definition = await _definitionStore.GetDefinitionAsync(definitionId);

            if (definition == null)
            {
                _logger.LogWarning("Workflow definition {DefinitionId} not found", definitionId);
                return null;
            }

            // Validate workflow definition
            ValidateWorkflowDefinition(definition);

            _logger.LogDebug("Loaded workflow definition {DefinitionId} with {StepCount} steps",
                definitionId, definition.Steps.Count);

            return definition;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load workflow definition {DefinitionId}", definitionId);
            throw new InvalidOperationException($"Failed to load workflow definition '{definitionId}'", ex);
        }
    }

    private void ValidateWorkflowDefinition(WorkflowDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new InvalidOperationException("Workflow definition must have an Id");
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            throw new InvalidOperationException("Workflow definition must have a Name");
        }

        if (definition.Steps == null || definition.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Workflow definition '{definition.Id}' must have at least one step");
        }

        // Validate each step
        for (int i = 0; i < definition.Steps.Count; i++)
        {
            var step = definition.Steps[i];
            ValidateWorkflowStep(step, i);
        }
    }

    private void ValidateWorkflowStep(WorkflowStep step, int index)
    {
        if (string.IsNullOrWhiteSpace(step.Name))
        {
            throw new InvalidOperationException($"Step at index {index} must have a Name");
        }

        switch (step.Type)
        {
            case StepType.Request:
                if (string.IsNullOrWhiteSpace(step.RequestType))
                {
                    throw new InvalidOperationException($"Request step '{step.Name}' must have a RequestType");
                }
                break;

            case StepType.Conditional:
                if (string.IsNullOrWhiteSpace(step.Condition))
                {
                    throw new InvalidOperationException($"Conditional step '{step.Name}' must have a Condition");
                }
                break;

            case StepType.Parallel:
                if (step.ParallelSteps == null || step.ParallelSteps.Count == 0)
                {
                    throw new InvalidOperationException($"Parallel step '{step.Name}' must have at least one ParallelStep");
                }

                // Validate nested steps
                for (int i = 0; i < step.ParallelSteps.Count; i++)
                {
                    ValidateWorkflowStep(step.ParallelSteps[i], i);
                }
                break;

            case StepType.Wait:
                if (step.WaitTimeMs == null || step.WaitTimeMs <= 0)
                {
                    throw new InvalidOperationException($"Wait step '{step.Name}' must have a positive WaitTimeMs value");
                }
                break;

            default:
                throw new NotSupportedException($"Step type {step.Type} is not supported");
        }

        // Validate else steps if present
        if (step.ElseSteps != null)
        {
            for (int i = 0; i < step.ElseSteps.Count; i++)
            {
                ValidateWorkflowStep(step.ElseSteps[i], i);
            }
        }
    }

    private async ValueTask FailWorkflow(WorkflowExecution execution, string error, CancellationToken cancellationToken)
    {
        execution.Status = WorkflowStatus.Failed;
        execution.Error = error;
        execution.CompletedAt = DateTime.UtcNow;
        await _stateStore.SaveExecutionAsync(execution, cancellationToken);
    }
}
