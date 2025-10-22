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
    public async Task LoadPluginAsync_PluginTypeNotFound_ReturnsNull()
    {
        // Arrange
        var pluginName = "NoPluginTypePlugin";
        
        // Create plugin directory with manifest and dummy DLL
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "No Plugin Type Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL that doesn't contain a valid plugin type
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-noplugintype.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
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
                      // Simulate timeout by creating a task that will never complete
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
