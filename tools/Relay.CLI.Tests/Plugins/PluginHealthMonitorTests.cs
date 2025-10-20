using Moq;
using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginHealthMonitorTests
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginHealthMonitor _monitor;

    public PluginHealthMonitorTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _monitor = new PluginHealthMonitor(_mockLogger.Object);
    }

    [Fact]
    public void RecordSuccess_UpdatesHealthInfo()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        _monitor.RecordSuccess(pluginName);

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal(1, healthInfo.SuccessCount);
        Assert.Equal(0, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Healthy, healthInfo.Status);
    }

    [Fact]
    public void RecordFailure_UpdatesHealthInfo()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        _monitor.RecordFailure(pluginName, new Exception("Test error"));

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal(0, healthInfo.SuccessCount);
        Assert.Equal(1, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Unhealthy, healthInfo.Status);
        Assert.Contains("Test error", healthInfo.LastErrorMessage);
    }

    [Fact]
    public void IsHealthy_NewPlugin_ReturnsTrue()
    {
        // Act
        var result = _monitor.IsHealthy("NewPlugin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHealthy_HealthyPlugin_ReturnsTrue()
    {
        // Arrange
        var pluginName = "HealthyPlugin";
        _monitor.RecordSuccess(pluginName);

        // Act
        var result = _monitor.IsHealthy(pluginName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHealthy_FailedPlugin_ReturnsFalse()
    {
        // Arrange
        var pluginName = "FailingPlugin";
        
        // Record multiple failures to trigger disabled state
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);

        // Act
        var result = _monitor.IsHealthy(pluginName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHealthInfo_ExistingPlugin_ReturnsInfo()
    {
        // Arrange
        var pluginName = "TestPlugin";
        _monitor.RecordSuccess(pluginName);

        // Act
        var healthInfo = _monitor.GetHealthInfo(pluginName);

        // Assert
        Assert.NotNull(healthInfo);
        Assert.Equal(pluginName, healthInfo.PluginName);
        Assert.Equal(1, healthInfo.SuccessCount);
    }

    [Fact]
    public void GetHealthInfo_NonExistingPlugin_ReturnsNull()
    {
        // Act
        var healthInfo = _monitor.GetHealthInfo("NonExistingPlugin");

        // Assert
        Assert.Null(healthInfo);
    }

    [Fact]
    public void GetAllHealthInfo_ReturnsAllInfo()
    {
        // Arrange
        _monitor.RecordSuccess("Plugin1");
        _monitor.RecordFailure("Plugin2");

        // Act
        var allHealthInfo = _monitor.GetAllHealthInfo();

        // Assert
        var list = allHealthInfo.ToList();
        Assert.Equal(2, list.Count);
        Assert.Contains(list, info => info.PluginName == "Plugin1");
        Assert.Contains(list, info => info.PluginName == "Plugin2");
    }

    [Fact]
    public void ResetHealth_ResetsPluginHealth()
    {
        // Arrange
        var pluginName = "TestPlugin";
        _monitor.RecordSuccess(pluginName);
        _monitor.RecordFailure(pluginName);

        // Verify initial state
        var initialInfo = _monitor.GetHealthInfo(pluginName);
        Assert.Equal(1, initialInfo.SuccessCount);
        Assert.Equal(1, initialInfo.FailureCount);

        // Act
        _monitor.ResetHealth(pluginName);

        // Assert
        var resetInfo = _monitor.GetHealthInfo(pluginName);
        Assert.Equal(0, resetInfo.SuccessCount);
        Assert.Equal(0, resetInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Healthy, resetInfo.Status);
    }

    [Fact]
    public void Dispose_DisposesResources()
    {
        // Act
        _monitor.Dispose();

        // Assert - Should not throw
        var result = _monitor.IsHealthy("AnyPlugin");
        Assert.False(result);
    }

    [Fact]
    public void RecordFailure_DisposedMonitor_DoesNothing()
    {
        // Arrange
        var pluginName = "TestPlugin";
        _monitor.Dispose();

        // Act
        _monitor.RecordFailure(pluginName, new Exception("Test error"));

        // Assert - Should not have recorded anything
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.Null(healthInfo);
        _mockLogger.Verify(x => x.LogWarning(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RecordFailure_NullException_UsesDefaultMessage()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        _monitor.RecordFailure(pluginName, null);

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal("Unknown error", healthInfo.LastErrorMessage);
        Assert.Equal(1, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Unhealthy, healthInfo.Status);
    }

    [Fact]
    public void RecordFailure_MultipleFailures_DisablesPlugin()
    {
        // Arrange
        var pluginName = "FailingPlugin";

        // Act - Record 4 failures to trigger disable logic
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName); // This should disable the plugin

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal(4, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Disabled, healthInfo.Status);
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("has failed multiple times and will be temporarily disabled"))), Times.Once);
    }

    [Fact]
    public void RecordFailure_MultipleFailures_IncrementsRestartCount()
    {
        // Arrange
        var pluginName = "FailingPlugin";

        // Act - Record multiple failures to trigger restart count tracking
        for (int i = 0; i < 5; i++)
        {
            _monitor.RecordFailure(pluginName);
        }

        // Assert - Should have incremented restart count
        // Note: We can't directly test the private _restartCount field,
        // but we can verify the behavior through the logging
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("has failed multiple times and will be temporarily disabled"))), Times.Exactly(2)); // 4th and 5th failure
    }

    [Fact]
    public void RecordFailure_ExceedsMaxRestartAttempts_StopsRestarting()
    {
        // Arrange
        var pluginName = "FailingPlugin";

        // Act - Record many failures to exceed max restart attempts
        for (int i = 0; i < 20; i++) // Way more than the limit of 5
        {
            _monitor.RecordFailure(pluginName);
        }

        // Assert - Should log that plugin exceeded max restart attempts
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("has exceeded maximum restart attempts and will remain disabled"))), Times.AtLeastOnce);
    }

    [Fact]
    public void RecordFailure_SingleFailure_DoesNotDisablePlugin()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act
        _monitor.RecordFailure(pluginName, new Exception("Test error"));

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal(1, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Unhealthy, healthInfo.Status); // Not disabled
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("will be temporarily disabled"))), Times.Never);
    }

    [Fact]
    public void RecordFailure_ThreeFailures_DoesNotDisablePlugin()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Act - Exactly 3 failures (boundary test)
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);
        _monitor.RecordFailure(pluginName);

        // Assert
        var healthInfo = _monitor.GetHealthInfo(pluginName);
        Assert.NotNull(healthInfo);
        Assert.Equal(3, healthInfo.FailureCount);
        Assert.Equal(PluginHealthStatus.Unhealthy, healthInfo.Status); // Not disabled yet
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("will be temporarily disabled"))), Times.Never);
    }
}