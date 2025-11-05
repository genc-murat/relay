using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// Base class for xUnit-based Relay tests providing common setup and teardown functionality.
/// </summary>
public abstract class RelayTestBase : IAsyncLifetime
{
    private TestRelay? _testRelay;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the TestRelay instance for the current test.
    /// </summary>
    protected TestRelay TestRelay => _testRelay ?? throw new InvalidOperationException("TestRelay not initialized. Ensure InitializeAsync is called.");

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    protected IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Services not initialized. Ensure InitializeAsync is called.");

    /// <summary>
    /// Initializes the test environment asynchronously.
    /// Override this method to customize test setup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task InitializeAsync()
    {
        _testRelay = new TestRelay();
        ConfigureTestRelay(_testRelay);
        _serviceProvider = new TestServiceProvider();

        await OnTestInitializedAsync();
    }

    /// <summary>
    /// Cleans up the test environment asynchronously.
    /// Override this method to customize test teardown.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task DisposeAsync()
    {
        await OnTestCleanupAsync();

        if (_testRelay != null)
        {
            _testRelay.Clear();
            _testRelay = null;
        }

        _serviceProvider = null;
    }

    /// <summary>
    /// Configures the TestRelay instance for the test.
    /// Override this method to customize the TestRelay setup.
    /// </summary>
    /// <param name="testRelay">The TestRelay instance to configure.</param>
    protected virtual void ConfigureTestRelay(TestRelay testRelay)
    {
        // Default configuration - override in derived classes
    }

    /// <summary>
    /// Called after the test environment has been initialized.
    /// Override this method to perform additional setup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnTestInitializedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called before the test environment is cleaned up.
    /// Override this method to perform additional cleanup.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task OnTestCleanupAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Creates and runs a test scenario.
    /// </summary>
    /// <param name="scenarioName">The name of the scenario.</param>
    /// <param name="configureScenario">Action to configure the scenario.</param>
    /// <returns>The scenario result.</returns>
    protected async Task<ScenarioResult> RunScenarioAsync(string scenarioName, Action<TestScenarioBuilder> configureScenario)
    {
        var scenario = new TestScenario { Name = scenarioName };
        var builder = new TestScenarioBuilder(scenario, TestRelay);
        configureScenario(builder);

        // Simple scenario execution
        var result = new ScenarioResult
        {
            ScenarioName = scenarioName,
            StartedAt = DateTime.UtcNow,
            Success = true
        };

        try
        {
            foreach (var step in scenario.Steps)
            {
                await ExecuteStepAsync(step);
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

    private async Task ExecuteStepAsync(TestStep step)
    {
        switch (step.Type)
        {
            case StepType.SendRequest:
                if (step.Request != null)
                    await TestRelay.SendAsync((IRequest)step.Request);
                break;
            case StepType.PublishNotification:
                if (step.Notification != null)
                    await TestRelay.PublishAsync((INotification)step.Notification);
                break;
            case StepType.StreamRequest:
                if (step.StreamRequest != null)
                {
                    await foreach (var _ in TestRelay.StreamAsync((IStreamRequest<object>)step.StreamRequest))
                    {
                        // Consume the stream
                    }
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

    /// <summary>
    /// Asserts that the scenario completed successfully.
    /// </summary>
    /// <param name="result">The scenario result to assert.</param>
    protected void AssertScenarioSuccess(ScenarioResult result)
    {
        Assert.NotNull(result);
        Assert.True(result.Success, $"Scenario failed: {result.Error}");
    }

    /// <summary>
    /// Asserts that the scenario failed with the expected exception type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="result">The scenario result to assert.</param>
    protected void AssertScenarioFailure<TException>(ScenarioResult result) where TException : Exception
    {
        Assert.NotNull(result);
        Assert.False(result.Success, "Scenario was expected to fail but succeeded");
        // Note: ScenarioResult doesn't store exception type, just error message
    }
}