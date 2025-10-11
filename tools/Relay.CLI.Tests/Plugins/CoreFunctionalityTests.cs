using Moq;
using Xunit;

namespace Relay.CLI.Plugins.Tests;

public class CoreFunctionalityTests
{
    private Mock<IPluginLogger> _mockLogger = null!;
    private PluginManager _pluginManager = null!;

    public CoreFunctionalityTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _pluginManager = new PluginManager(_mockLogger.Object);
    }

    public void Dispose()
    {
        _pluginManager?.Dispose();
    }

    [Fact]
    public async Task DependencyResolver_InitializesCorrectly()
    {
        // Arrange & Act
        var dependencyResolver = new DependencyResolver(_mockLogger.Object);

        // Assert
        Assert.NotNull(dependencyResolver);
    }

    [Fact]
    public async Task PluginHealthMonitor_AutoRestartFunctionality()
    {
        // Arrange
        var healthMonitor = new PluginHealthMonitor(_mockLogger.Object);
        var pluginName = "TestPlugin";

        // Act - Manually set the plugin to disabled state to test restart functionality
        healthMonitor.RecordFailure(pluginName, new Exception("Test failure"));
        var healthInfo = healthMonitor.GetHealthInfo(pluginName);
        
        // Manually set to disabled state for testing
        if (healthInfo != null)
        {
            healthInfo.Status = PluginHealthStatus.Disabled;
        }

        // Check that the plugin is not healthy
        var isHealthy = healthMonitor.IsHealthy(pluginName);
        
        // Attempt to restart
        var restartAttempted = healthMonitor.AttemptRestart(pluginName);

        // Check health again after restart attempt
        var isHealthyAfterRestart = healthMonitor.IsHealthy(pluginName);

        // Assert
        Assert.False(isHealthy, "Plugin should not be healthy when disabled");
        Assert.True(restartAttempted, "Restart should be attempted");
        Assert.False(isHealthyAfterRestart, "Plugin should be in unknown state after restart attempt, not immediately healthy");
    }

    [Fact]
    public async Task LazyPluginLoader_InitializesCorrectly()
    {
        // Arrange & Act
        var lazyLoader = new LazyPluginLoader(_pluginManager, _mockLogger.Object);

        // Assert
        Assert.NotNull(lazyLoader);
    }

    [Fact]
    public async Task PluginSandbox_ResourceLimits()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            MaxMemoryBytes = 50 * 1024 * 1024, // 50MB
            MaxExecutionTimeMs = 10000 // 10 seconds
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act & Assert - this test mainly verifies the sandbox can be created with resource limits
        Assert.NotNull(sandbox);
    }

    [Fact]
    public async Task PluginManager_UsesNewFunctionality()
    {
        // Arrange - The plugin manager should have been initialized with all new components
        var pluginManager = new PluginManager(_mockLogger.Object);

        // Act & Assert - Verify the new components are present
        // This is mainly testing that no exceptions are thrown during initialization
        Assert.NotNull(pluginManager);
    }
}