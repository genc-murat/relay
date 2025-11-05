#if IncludeMSTest
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Relay.Core.Testing;

/// <summary>
/// Base class for MSTest-based Relay tests providing common setup and teardown functionality.
/// </summary>
[TestClass]
public abstract class RelayTestClass
{
    private TestRelay? _testRelay;
    private IServiceProvider? _serviceProvider;

    /// <summary>
    /// Gets the TestRelay instance for the current test.
    /// </summary>
    protected TestRelay TestRelay => _testRelay ?? throw new InvalidOperationException("TestRelay not initialized. Ensure TestInitialize is called.");

    /// <summary>
    /// Gets the service provider for dependency injection.
    /// </summary>
    protected IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Services not initialized. Ensure TestInitialize is called.");

    /// <summary>
    /// Initializes the test environment.
    /// Called before each test method.
    /// </summary>
    [TestInitialize]
    public async Task TestInitialize()
    {
        _testRelay = new TestRelay();
        ConfigureTestRelay(_testRelay);
        _serviceProvider = new TestServiceProvider();

        await OnTestInitializedAsync();
    }

    /// <summary>
    /// Cleans up the test environment.
    /// Called after each test method.
    /// </summary>
    [TestCleanup]
    public async Task TestCleanup()
    {
        await OnTestCleanupAsync();

        if (_testRelay != null)
        {
            await _testRelay.DisposeAsync();
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
        var builder = new TestScenarioBuilder(scenarioName);
        configureScenario(builder);
        var scenario = builder.Build();

        return await TestRelay.RunScenarioAsync(scenario);
    }

    /// <summary>
    /// Asserts that the scenario completed successfully.
    /// </summary>
    /// <param name="result">The scenario result to assert.</param>
    protected void AssertScenarioSuccess(ScenarioResult result)
    {
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, $"Scenario failed: {result.ErrorMessage}");
        Assert.IsNull(result.Exception);
    }

    /// <summary>
    /// Asserts that the scenario failed with the expected exception type.
    /// </summary>
    /// <typeparam name="TException">The expected exception type.</typeparam>
    /// <param name="result">The scenario result to assert.</param>
    protected void AssertScenarioFailure<TException>(ScenarioResult result) where TException : Exception
    {
        Assert.IsNotNull(result);
        Assert.IsFalse(result.IsSuccessful, "Scenario was expected to fail but succeeded");
        Assert.IsNotNull(result.Exception);
        Assert.IsInstanceOfType(result.Exception, typeof(TException));
    }
}
#endif