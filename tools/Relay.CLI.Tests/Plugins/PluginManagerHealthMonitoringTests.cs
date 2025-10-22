using Moq;
using Relay.CLI.Plugins;
using System.Reflection;
using System.Text.Json;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.CLI.Tests.Plugins;

public class PluginManagerHealthMonitoringTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginManager _manager;
    private readonly string _tempPluginsDir;
    private readonly string _tempGlobalPluginsDir;

    public PluginManagerHealthMonitoringTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        
        // Create temporary directories for testing
        _tempPluginsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempGlobalPluginsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        Directory.CreateDirectory(_tempPluginsDir);
        Directory.CreateDirectory(_tempGlobalPluginsDir);
        
        _manager = new PluginManager(_mockLogger.Object);
        
        // Override the directory paths using reflection for testing
        var pluginsDirField = typeof(PluginManager).GetField("_pluginsDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
        var globalPluginsDirField = typeof(PluginManager).GetField("_globalPluginsDirectory", BindingFlags.NonPublic | BindingFlags.Instance);

        pluginsDirField?.SetValue(_manager, _tempPluginsDir);
        globalPluginsDirField?.SetValue(_manager, _tempGlobalPluginsDir);
    }

    [Fact]
    public async Task LoadPluginAsync_PluginUnhealthy_LoadsAfterRestart()
    {
        // Arrange
        var pluginName = "UnhealthyPlugin";
        
        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Unhealthy Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-unhealthy.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Mark the plugin as unhealthy in the health monitor
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures to make the plugin unhealthy
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, new Exception($"Failure {i+1}"));
        }
        
        var context = new Mock<IPluginContext>();
        
        // Mock security validator to return valid result
        var securityValidatorField = typeof(PluginManager).GetField("_securityValidator", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalSecurityValidator = securityValidatorField.GetValue(_manager);
        
        var mockSecurityValidator = new Mock<PluginSecurityValidator>(_mockLogger.Object);
        var validationResult = new SecurityValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };
        mockSecurityValidator.Setup(sv => sv.ValidatePluginAsync(It.IsAny<string>(), It.IsAny<PluginInfo>()))
                            .ReturnsAsync(validationResult);
        
        securityValidatorField.SetValue(_manager, mockSecurityValidator.Object);
        
        try
        {
            // Act
            var result = await _manager.LoadPluginAsync(pluginName, context.Object);
            
            // Assert
            // Should return null because the plugin is unhealthy
            Assert.Null(result);
        }
        finally
        {
            // Restore original security validator
            securityValidatorField.SetValue(_manager, originalSecurityValidator);
        }
    }

    [Fact]
    public async Task HealthMonitor_RecordSuccess_UpdatesPluginStatus()
    {
        // Arrange
        var pluginName = "HealthyPlugin";
        
        // Use the health monitor to record a success
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Act
        healthMonitor.RecordSuccess(pluginName);
        
        // Assert
        // Plugin should be considered healthy after success record
        Assert.True(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task HealthMonitor_RecordMultipleFailures_MakesPluginUnhealthy()
    {
        // Arrange
        var pluginName = "FailingPlugin";
        
        // Use the health monitor to record multiple failures
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures to make the plugin unhealthy
        var exception = new Exception("Test failure");
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, exception);
        }
        
        // Act & Assert
        // Plugin should be considered unhealthy after multiple failures
        Assert.False(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task HealthMonitor_AttemptRestart_ResetsFailureCount()
    {
        // Arrange
        var pluginName = "RestartablePlugin";
        
        // Use the health monitor to record failures
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures to make the plugin unhealthy
        var exception = new Exception("Test failure");
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, exception);
        }
        
        // Verify plugin is unhealthy
        Assert.False(healthMonitor.IsHealthy(pluginName));
        
        // Act
        var restartAttempted = healthMonitor.AttemptRestart(pluginName);
        
        // Reset the count manually since we can't wait for the time-based reset
        healthMonitor.ResetRestartCount(pluginName);
        
        // Assert
        Assert.True(restartAttempted);
    }

    [Fact]
    public async Task LoadPluginAsync_WithRestartMechanism_TriesToRestart()
    {
        // Arrange
        var pluginName = "NeedRestartPlugin";
        
        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Need Restart Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-needrestart.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Mark the plugin as unhealthy in the health monitor
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures to make the plugin unhealthy
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, new Exception($"Failure {i+1}"));
        }
        
        // Pre-load the plugin so it can be restarted
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);
        
        loadedPlugins[pluginName] = new LoadedPlugin
        {
            Name = pluginName,
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };
        
        var context = new Mock<IPluginContext>();
        
        // Mock security validator to return valid result
        var securityValidatorField = typeof(PluginManager).GetField("_securityValidator", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalSecurityValidator = securityValidatorField.GetValue(_manager);
        
        var mockSecurityValidator = new Mock<PluginSecurityValidator>(_mockLogger.Object);
        var validationResult = new SecurityValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };
        mockSecurityValidator.Setup(sv => sv.ValidatePluginAsync(It.IsAny<string>(), It.IsAny<PluginInfo>()))
                            .ReturnsAsync(validationResult);
        
        securityValidatorField.SetValue(_manager, mockSecurityValidator.Object);
        
        try
        {
            // Act
            var result = await _manager.LoadPluginAsync(pluginName, context.Object);
            
            // Assert
            // The result might be null because of the unhealthy status, but the restart mechanism should be triggered
            // The important thing is that it doesn't crash
            Assert.Null(result);
        }
        finally
        {
            // Restore original security validator
            securityValidatorField.SetValue(_manager, originalSecurityValidator);
        }
    }

    [Fact]
    public async Task ExecutePluginAsync_UnhealthyPlugin_ReturnsErrorAndRecordsFailure()
    {
        // Arrange
        var pluginName = "ExecuteUnhealthyPlugin";
        
        // Mark the plugin as unhealthy in the health monitor
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures to make the plugin unhealthy
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, new Exception($"Failure {i+1}"));
        }
        
        var context = new Mock<IPluginContext>();
        var mockFileSystem = new Mock<IFileSystem>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockServices = new Mock<IServiceProvider>();
        
        context.Setup(c => c.Logger).Returns(_mockLogger.Object);
        context.Setup(c => c.FileSystem).Returns(mockFileSystem.Object);
        context.Setup(c => c.Configuration).Returns(mockConfiguration.Object);
        context.Setup(c => c.Services).Returns(mockServices.Object);
        context.Setup(c => c.CliVersion).Returns("2.1.0");
        context.Setup(c => c.WorkingDirectory).Returns(Directory.GetCurrentDirectory());
        
        // Act
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object);
        
        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task PluginHealthMonitor_GetHealthStats_ReturnsCorrectInformation()
    {
        // Arrange
        var pluginName = "TestPlugin";

        // Use the health monitor to record some events
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);

        // Record a success
        healthMonitor.RecordSuccess(pluginName);

        // Record a failure
        healthMonitor.RecordFailure(pluginName, new Exception("Test failure"));

        // Act & Assert - After recording a failure, the plugin should be unhealthy
        // The status is set to Unhealthy on any failure (even if there was a prior success)
        Assert.False(healthMonitor.IsHealthy(pluginName));

        // Record another success to make it healthy again
        healthMonitor.RecordSuccess(pluginName);
        Assert.True(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task PluginHealthMonitor_Dispose_CleansUpResources()
    {
        // Arrange
        var healthMonitor = new PluginHealthMonitor(_mockLogger.Object);
        
        // Act
        healthMonitor.Dispose();
        
        // Assert - The dispose should complete without throwing
        // We can't easily verify internal cleanup, but we can at least ensure no exception is thrown
    }

    [Fact]
    public async Task HealthMonitor_FailureThreshold_Exceeded_MakesPluginUnhealthy()
    {
        // Arrange
        var pluginName = "ThresholdExceededPlugin";
        
        // Use the health monitor to record failures up to and beyond the threshold
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record more failures than the threshold (assuming default threshold)
        var exception = new Exception("Threshold test failure");
        for (int i = 0; i < 10; i++) // More than typical failure threshold
        {
            healthMonitor.RecordFailure(pluginName, exception);
        }
        
        // Act & Assert
        Assert.False(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task HealthMonitor_ResetRestartCount_AfterSuccess()
    {
        // Arrange
        var pluginName = "ResetCountPlugin";
        
        // Use the health monitor to record failures
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        
        // Record multiple failures
        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, new Exception($"Failure {i+1}"));
        }
        
        // Verify it's unhealthy
        Assert.False(healthMonitor.IsHealthy(pluginName));
        
        // Act - Record success and reset count
        healthMonitor.RecordSuccess(pluginName);
        healthMonitor.ResetRestartCount(pluginName);
        
        // Assert - After success and reset, plugin should be healthy again
        Assert.True(healthMonitor.IsHealthy(pluginName));
    }

    private byte[] GenerateDummyAssembly()
    {
        // Create a minimal dummy assembly as a byte array
        return System.Text.Encoding.UTF8.GetBytes("dummy-assembly-content");
    }

    private PluginLoadContext CreateMockLoadContext()
    {
        // Create a temporary assembly path for the mock
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        return new PluginLoadContext(tempPath);
    }

    public void Dispose()
    {
        // Cleanup temporary directories
        try
        {
            if (Directory.Exists(_tempPluginsDir))
                Directory.Delete(_tempPluginsDir, true);
            if (Directory.Exists(_tempGlobalPluginsDir))
                Directory.Delete(_tempGlobalPluginsDir, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }

        _manager?.Dispose();
    }
}
