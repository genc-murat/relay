using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Base class for scenario templates that provide higher-level abstractions for common testing patterns.
/// </summary>
public abstract class ScenarioTemplate
{
    private readonly TestScenario _scenario;
    private readonly IRelay _relay;
    private readonly List<Func<Task>> _setupActions = new();
    private readonly List<Func<Task>> _teardownActions = new();

    /// <summary>
    /// Gets the name of the scenario.
    /// </summary>
    public string ScenarioName => _scenario.Name;

    /// <summary>
    /// Gets the underlying test scenario.
    /// </summary>
    protected TestScenario Scenario => _scenario;

    /// <summary>
    /// Gets the relay instance for sending requests and publishing notifications.
    /// </summary>
    protected IRelay Relay => _relay;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioTemplate"/> class.
    /// </summary>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="relay">The relay instance to use.</param>
    protected ScenarioTemplate(string scenarioName, IRelay relay)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            throw new ArgumentException("Scenario name cannot be null or empty", nameof(scenarioName));

        _scenario = new TestScenario { Name = scenarioName };
        _relay = relay ?? throw new ArgumentNullException(nameof(relay));
    }

    /// <summary>
    /// Adds a setup action to be executed before the scenario runs.
    /// </summary>
    /// <param name="setupAction">The setup action.</param>
    /// <returns>The scenario template for chaining.</returns>
    public ScenarioTemplate WithSetup(Func<Task> setupAction)
    {
        if (setupAction == null) throw new ArgumentNullException(nameof(setupAction));
        _setupActions.Add(setupAction);
        return this;
    }

    /// <summary>
    /// Adds a teardown action to be executed after the scenario runs.
    /// </summary>
    /// <param name="teardownAction">The teardown action.</param>
    /// <returns>The scenario template for chaining.</returns>
    public ScenarioTemplate WithTeardown(Func<Task> teardownAction)
    {
        if (teardownAction == null) throw new ArgumentNullException(nameof(teardownAction));
        _teardownActions.Add(teardownAction);
        return this;
    }

    /// <summary>
    /// Builds the scenario by configuring the steps.
    /// Subclasses must implement this method to define the scenario steps.
    /// </summary>
    protected abstract void BuildScenario();

    /// <summary>
    /// Allows subclasses to customize the scenario after building.
    /// </summary>
    /// <param name="builder">The scenario builder.</param>
    protected virtual void CustomizeScenario(TestScenarioBuilder builder)
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Executes the setup actions.
    /// </summary>
    private async Task ExecuteSetupAsync()
    {
        foreach (var setupAction in _setupActions)
        {
            await setupAction();
        }
    }

    /// <summary>
    /// Executes the teardown actions.
    /// </summary>
    private async Task ExecuteTeardownAsync()
    {
        foreach (var teardownAction in _teardownActions)
        {
            await teardownAction();
        }
    }

    /// <summary>
    /// Builds and executes the scenario.
    /// </summary>
    /// <returns>The scenario result.</returns>
    public async Task<ScenarioResult> ExecuteAsync()
    {
        await ExecuteSetupAsync();

        try
        {
            BuildScenario();

            var builder = new TestScenarioBuilder(_scenario, _relay);
            CustomizeScenario(builder);

            // Use the existing scenario execution from RelayTestBase
            var result = new ScenarioResult
            {
                ScenarioName = _scenario.Name,
                StartedAt = DateTime.UtcNow,
                Success = true
            };

            try
            {
                foreach (var step in _scenario.Steps)
                {
                    step.Validate();
                    await ExecuteStepAsync(step, result);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex.Message;
            }

            result.CompletedAt = DateTime.UtcNow;
            return result;
        }
        finally
        {
            await ExecuteTeardownAsync();
        }
    }

    private async Task ExecuteStepAsync(TestStep step, ScenarioResult result)
    {
        var stepResult = new StepResult
        {
            StepName = step.Name,
            StartedAt = DateTime.UtcNow,
            Success = true
        };

        try
        {
            switch (step.Type)
            {
                case StepType.SendRequest:
                    if (step.Request != null)
                    {
                        // Use dynamic dispatch to call the correct generic SendAsync method
                        dynamic request = step.Request;
                        await _relay.SendAsync(request);
                    }
                    break;
                case StepType.PublishNotification:
                    if (step.Notification != null)
                    {
                        // Use reflection to call the correct generic PublishAsync method
                        var notificationType = step.Notification.GetType();
                        var method = typeof(IRelay).GetMethod(nameof(IRelay.PublishAsync))!.MakeGenericMethod(notificationType);
                        await (ValueTask)method.Invoke(_relay, new[] { step.Notification, CancellationToken.None })!;
                    }
                    break;
                case StepType.StreamRequest:
                    if (step.StreamRequest != null)
                    {
                        var responses = new List<object>();
                        await foreach (var response in _relay.StreamAsync((IStreamRequest<object>)step.StreamRequest))
                        {
                            responses.Add(response);
                        }
                        stepResult.Response = responses;
                    }
                    break;
                case StepType.Verify:
                    if (step.VerificationFunc != null && !await step.VerificationFunc())
                        throw new VerificationException($"Verification failed for step: {step.Name}");
                    break;
                case StepType.Wait:
                    if (step.WaitTime.HasValue)
                        await Task.Delay(step.WaitTime.Value);
                    break;
            }
        }
        catch (Exception ex)
        {
            stepResult.Success = false;
            stepResult.Error = ex.Message;
            throw;
        }
        finally
        {
            stepResult.CompletedAt = DateTime.UtcNow;
            result.StepResults.Add(stepResult);
        }
    }
}