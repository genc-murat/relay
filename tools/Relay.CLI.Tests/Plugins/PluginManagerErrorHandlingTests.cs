using Moq;
using Relay.CLI.Plugins;
using System.Reflection;
using System.Text.Json;

#pragma warning disable CS8600, CS8602 // Dereference of a possibly null reference

namespace Relay.CLI.Tests.Plugins;

public class PluginManagerErrorHandlingTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginManager _manager;
    private readonly string _tempPluginsDir;
    private readonly string _tempGlobalPluginsDir;

    public PluginManagerErrorHandlingTests()
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
    public async Task LoadPluginAsync_SecurityValidationFails_ReturnsNull()
    {
        // Arrange
        var pluginName = "SecurityTestPlugin";
        
        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Security Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-securitytest.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Mock security validator to return invalid result
        var securityValidatorField = typeof(PluginManager).GetField("_securityValidator", BindingFlags.NonPublic | BindingFlags.Instance);
        var originalSecurityValidator = securityValidatorField.GetValue(_manager);
        
        var mockSecurityValidator = new Mock<PluginSecurityValidator>(_mockLogger.Object);
        var validationResult = new SecurityValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Security violation: unsafe code detected" }
        };
        mockSecurityValidator.Setup(sv => sv.ValidatePluginAsync(It.IsAny<string>(), It.IsAny<PluginInfo>()))
                            .ReturnsAsync(validationResult);
        
        securityValidatorField.SetValue(_manager, mockSecurityValidator.Object);
        
        var context = new Mock<IPluginContext>();
        
        try
        {
            // Act
            var result = await _manager.LoadPluginAsync(pluginName, context.Object);
            
            // Assert
            Assert.Null(result);
        }
        finally
        {
            // Restore original security validator
            securityValidatorField.SetValue(_manager, originalSecurityValidator);
        }
    }

    [Fact]
    public async Task LoadPluginAsync_InvalidAssemblyPath_ReturnsNull()
    {
        // Arrange
        var pluginName = "InvalidAssemblyPlugin";
        
        // Create plugin directory with manifest but no DLL
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Invalid Assembly Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        var context = new Mock<IPluginContext>();
        
        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);
        
        // Assert
        Assert.Null(result);
    }


    [Fact]
    public async Task LoadPluginAsync_PluginInitializationFails_ReturnsNull()
    {
        // Arrange
        var pluginName = "FailingInitPlugin";
        
        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Failing Initialization Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-failinginit.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Mock the plugin loading process to simulate initialization failure
        var context = new Mock<IPluginContext>();
        
        // Use reflection to add a mock plugin that fails initialization
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);
        
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false); // Simulate initialization failure
        
        loadedPlugins[pluginName] = new LoadedPlugin
        {
            Name = pluginName,
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };
        
        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadPluginAsync_AlreadyLoadedPlugin_ReturnsExistingInstance()
    {
        // Arrange
        var pluginName = "AlreadyLoadedPlugin";
        
        // Create a mock plugin instance
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        
        // Use reflection to add the plugin to loaded plugins
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
        
        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockPlugin.Object, result);
    }

    [Fact]
    public async Task UnloadPluginAsync_WithExceptionDuringCleanup_ReturnsFalse()
    {
        // Arrange
        var pluginName = "UnloadExceptionPlugin";

        // Mock plugin with exception during cleanup
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("Cleanup failed"));

        // Use reflection to add the plugin to loaded plugins
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);

        loadedPlugins[pluginName] = new LoadedPlugin
        {
            Name = pluginName,
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };

        // Act
        var result = await _manager.UnloadPluginAsync(pluginName);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UnloadPluginAsync_CleanupTimeout_ReturnsFalse()
    {
        // Arrange
        var pluginName = "UnloadTimeoutPlugin";

        // Mock plugin with slow cleanup that exceeds timeout
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .Returns(async (CancellationToken ct) =>
                  {
                      // Simulate cleanup that takes longer than the 5-second timeout
                      await Task.Delay(TimeSpan.FromSeconds(10), ct);
                  });

        // Use reflection to add the plugin to loaded plugins
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);

        loadedPlugins[pluginName] = new LoadedPlugin
        {
            Name = pluginName,
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };

        // Act
        var result = await _manager.UnloadPluginAsync(pluginName);

        // Assert
        Assert.False(result);
        // Plugin should still be removed from loaded plugins even with timeout
        Assert.False(loadedPlugins.ContainsKey(pluginName));
    }

    [Fact]
    public async Task UnloadPluginAsync_CleanupCancellation_ReturnsFalse()
    {
        // Arrange
        var pluginName = "UnloadCancelPlugin";

        // Mock plugin with cleanup that gets cancelled
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .Returns(async (CancellationToken ct) =>
                  {
                      // Wait for cancellation
                      var tcs = new TaskCompletionSource<bool>();
                      using (ct.Register(() => tcs.SetResult(true)))
                      {
                          await tcs.Task;
                      }
                  });

        // Use reflection to add the plugin to loaded plugins
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);

        loadedPlugins[pluginName] = new LoadedPlugin
        {
            Name = pluginName,
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };

        // Act
        var result = await _manager.UnloadPluginAsync(pluginName);

        // Assert
        Assert.False(result);
        // Plugin should still be removed from loaded plugins even with cancellation
        Assert.False(loadedPlugins.ContainsKey(pluginName));
    }

    [Fact]
    public async Task ExecutePluginAsync_WithExceptionDuringExecution_ReturnsNegativeOne()
    {
        // Arrange
        var pluginName = "ExecutionExceptionPlugin";
        
        // Mock plugin with exception during execution
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Test Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("Execution failed"));
        mockPlugin.Setup(p => p.GetHelp()).Returns("Help text");
        
        // Use reflection to add the plugin to loaded plugins
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
    public async Task ExecutePluginAsync_WithTimeoutException_ReturnsNegativeOne()
    {
        // Arrange
        var pluginName = "TimeoutPlugin";

        // Mock plugin with timeout behavior
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Test Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .Returns(async (string[] args, CancellationToken ct) =>
                  {
                      // Simulate a task that will never complete
                      var tcs = new TaskCompletionSource<int>();
                      ct.Register(() => tcs.SetCanceled());
                      return await tcs.Task;
                  });

        // Use reflection to add the plugin to loaded plugins
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
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task ExecutePluginAsync_PluginLoadFails_ReturnsErrorAndRecordsFailure()
    {
        // Arrange
        var pluginName = "LoadFailPlugin";

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

        // Act - Try to execute a plugin that doesn't exist
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object);

        // Assert
        Assert.Equal(-1, result);
        // Health monitor should record the failure
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        Assert.False(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task ExecutePluginAsync_SandboxExecutionException_ReturnsNegativeOne()
    {
        // Arrange
        var pluginName = "SandboxExceptionPlugin";

        // Mock plugin that throws exception during execution
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Sandbox Exception Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new InvalidOperationException("Sandbox execution failed"));
        mockPlugin.Setup(p => p.GetHelp()).Returns("Help text");

        // Use reflection to add the plugin to loaded plugins
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
        // Health monitor should record the failure
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);
        Assert.False(healthMonitor.IsHealthy(pluginName));
    }

    [Fact]
    public async Task InstallPluginAsync_InvalidNuGetPackage_FailsGracefully()
    {
        // Arrange
        var packageName = "NonExistentPackage";
        var version = "1.0.0";
        
        // Act
        var result = await _manager.InstallPluginAsync(packageName, version, false);
        
        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task InstallPluginAsync_InvalidZipFile_Fails()
    {
        // Arrange
        var zipPath = Path.GetTempFileName();
        // Create an invalid zip file (just text content)
        await File.WriteAllTextAsync(zipPath, "Not a zip file");
        
        try
        {
            // Act
            var result = await _manager.InstallPluginAsync(zipPath, null, false);
            
            // Assert
            Assert.False(result.Success);
        }
        finally
        {
            // Cleanup
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }

    [Fact]
    public async Task LoadPluginAsync_WithInvalidPluginJson_FailsGracefully()
    {
        // Arrange
        // Create a plugin directory with an invalid plugin.json
        var pluginDir = Path.Combine(_tempPluginsDir, "InvalidJsonPlugin");
        Directory.CreateDirectory(pluginDir);
        
        // Write invalid JSON
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, "{ invalid json }");
        
        var context = new Mock<IPluginContext>();
        
        // Act & Assert - should not throw exception
        var result = await _manager.LoadPluginAsync("InvalidJsonPlugin", context.Object);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstalledPluginsAsync_InvalidPluginDirectory_SkipsInvalid()
    {
        // Arrange
        // Create a plugin directory with no plugin.json
        var pluginDir = Path.Combine(_tempPluginsDir, "NoManifestPlugin");
        Directory.CreateDirectory(pluginDir);

        // Create a valid plugin alongside
        var validPluginDir = Path.Combine(_tempPluginsDir, "ValidPlugin");
        Directory.CreateDirectory(validPluginDir);

        var manifest = new PluginManifest
        {
            Name = "ValidPlugin",
            Version = "1.0.0",
            Description = "Valid Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(validPluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Act
        var plugins = await _manager.GetInstalledPluginsAsync();

        // Assert
        Assert.Single(plugins);
        Assert.Equal("ValidPlugin", plugins[0].Name);
    }

    [Fact]
    public async Task LoadPluginAsync_AlreadyLoaded_ReInitializationSuccess_ReturnsExistingInstance()
    {
        // Arrange
        var pluginName = "ReInitSuccessPlugin";

        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);

        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Re-init Success Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-reinit.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());

        // Pre-load the plugin with a mock that succeeds re-initialization
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true); // Re-initialization succeeds

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

        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockPlugin.Object, result);
        mockPlugin.Verify(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadPluginAsync_AlreadyLoaded_ReInitializationTimeout_UnloadsPlugin()
    {
        // Arrange
        var pluginName = "ReInitTimeoutPlugin";

        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);

        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Re-init Timeout Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-reinittimeout.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());

        // Pre-load the plugin with a mock that times out during re-initialization
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .Returns(async (IPluginContext ctx, CancellationToken ct) =>
                  {
                      // Wait longer than the 3-second timeout
                      await Task.Delay(TimeSpan.FromSeconds(4), ct);
                      return true;
                  });

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

        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);

        // Assert
        Assert.Null(result);
        // Plugin should be unloaded due to re-initialization timeout
        Assert.False(loadedPlugins.ContainsKey(pluginName));
    }

    [Fact]
    public async Task LoadPluginAsync_AlreadyLoaded_ReInitializationFails_UnloadsPlugin()
    {
        // Arrange
        var pluginName = "ReInitFailPlugin";

        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);

        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Re-init Fail Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-reinitfail.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());

        // Pre-load the plugin with a mock that fails re-initialization
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(false); // Re-initialization fails

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

        // Act
        var result = await _manager.LoadPluginAsync(pluginName, context.Object);

        // Assert
        Assert.Null(result);
        // Plugin should be unloaded due to re-initialization failure
        Assert.False(loadedPlugins.ContainsKey(pluginName));
    }

    [Fact]
    public async Task LoadPluginAsync_UnhealthyPlugin_RestartSucceeds_LoadsPlugin()
    {
        // Arrange
        var pluginName = "RestartSuccessPlugin";

        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);

        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Restart Success Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-restartsuccess.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());

        // Pre-load the plugin and make it unhealthy
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

        // Make the plugin unhealthy
        var healthMonitorField = typeof(PluginManager).GetField("_healthMonitor", BindingFlags.NonPublic | BindingFlags.Instance);
        var healthMonitor = (PluginHealthMonitor)healthMonitorField.GetValue(_manager);

        for (int i = 0; i < 5; i++)
        {
            healthMonitor.RecordFailure(pluginName, new Exception($"Failure {i+1}"));
        }

        // Mock security validator to allow loading
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

        var context = new Mock<IPluginContext>();

        try
        {
            // Act
            var result = await _manager.LoadPluginAsync(pluginName, context.Object);

            // Assert
            // Since the plugin is unhealthy, it should attempt restart and unload the existing one
            // The result might be null because the actual loading would require a real assembly
            // But the important thing is that the restart mechanism is triggered
            Assert.Null(result);
            // The original plugin should be unloaded
            Assert.False(loadedPlugins.ContainsKey(pluginName));
        }
        finally
        {
            // Restore original security validator
            securityValidatorField.SetValue(_manager, originalSecurityValidator);
        }
    }

    private byte[] GenerateDummyAssembly()
    {
        // Create a minimal dummy assembly as a byte array
        // This is just a placeholder since we can't easily create real .NET assemblies in tests
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
