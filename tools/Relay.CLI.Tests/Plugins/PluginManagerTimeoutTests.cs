using Moq;
using Relay.CLI.Plugins;
using System.Reflection;
using System.Text.Json;

namespace Relay.CLI.Tests.Plugins;

public class PluginManagerTimeoutTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginManager _manager;
    private readonly string _tempPluginsDir;
    private readonly string _tempGlobalPluginsDir;

    public PluginManagerTimeoutTests()
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
    public async Task LoadPluginAsync_InitializationTimeout_ReturnsNull()
    {
        // Arrange
        var pluginName = "SlowInitPlugin";
        
        // Create plugin directory with manifest
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Slow Initialization Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create a dummy plugin DLL
        var dummyDllPath = Path.Combine(pluginDir, "relay-plugin-slowinit.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Use reflection to add a mock plugin that delays initialization
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);
        
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .Returns(async (IPluginContext ctx, CancellationToken ct) =>
                  {
                      // Simulate a long-running initialization that should timeout
                      try
                      {
                          await Task.Delay(TimeSpan.FromSeconds(5), ct);
                          return true;
                      }
                      catch (OperationCanceledException)
                      {
                          return false;
                      }
                  });
        
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
    }

    [Fact]
    public async Task UnloadPluginAsync_CleanupTimeout_ReturnsFalse()
    {
        // Arrange
        var pluginName = "SlowCleanupPlugin";
        
        // Create mock plugin with slow cleanup
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .Returns(async (CancellationToken ct) =>
                  {
                      // Simulate a long-running cleanup that should timeout
                      try
                      {
                          await Task.Delay(TimeSpan.FromSeconds(10), ct);
                      }
                      catch (OperationCanceledException)
                      {
                          // If cancelled, just return
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
    }

    [Fact]
    public async Task ExecutePluginAsync_ExecutionTimeout_ReturnsNegativeOne()
    {
        // Arrange
        var pluginName = "SlowExecutionPlugin";
        
        // Create mock plugin with slow execution
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Slow Execution Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .Returns(async (string[] args, CancellationToken ct) =>
                  {
                      // Simulate a long-running execution that should timeout
                      try
                      {
                          await Task.Delay(TimeSpan.FromSeconds(10), ct);
                          return 0;
                      }
                      catch (OperationCanceledException)
                      {
                          return -1;
                      }
                  });
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
        
        // Act - Using a very short timeout to ensure it times out
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object, TimeSpan.FromMilliseconds(100));
        
        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task ExecutePluginAsync_WithDefaultTimeout_SucceedsForFastPlugin()
    {
        // Arrange
        var pluginName = "FastExecutionPlugin";
        
        // Create mock plugin with fast execution
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Fast Execution Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(0); // Simulate successful execution
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
        
        // Act - Using default timeout (5 minutes)
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object, TimeSpan.FromMinutes(5));
        
        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task LoadPluginAsync_WithTokenCancellation_ReturnsNull()
    {
        // Arrange
        var pluginName = "CancelOnInitPlugin";
        
        // Use reflection to add a mock plugin that respects cancellation
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);
        
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .Returns(async (IPluginContext ctx, CancellationToken ct) =>
                  {
                      // Wait for cancellation
                      var tcs = new TaskCompletionSource<bool>();
                      using (ct.Register(() => tcs.SetResult(true)))
                      {
                          await tcs.Task;
                      }
                      return false; // Return false when cancelled
                  });
        
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
    }

    [Fact]
    public async Task ExecutePluginAsync_WithTokenCancellation_ReturnsNegativeOne()
    {
        // Arrange
        var pluginName = "CancelOnExecutePlugin";
        
        // Create mock plugin that respects cancellation
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.Name).Returns(pluginName);
        mockPlugin.Setup(p => p.Version).Returns("1.0.0");
        mockPlugin.Setup(p => p.Description).Returns("Cancel On Execute Plugin");
        mockPlugin.Setup(p => p.Authors).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.Tags).Returns(Array.Empty<string>());
        mockPlugin.Setup(p => p.MinimumRelayVersion).Returns("2.1.0");
        mockPlugin.Setup(p => p.InitializeAsync(It.IsAny<IPluginContext>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);
        mockPlugin.Setup(p => p.ExecuteAsync(It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
                  .Returns(async (string[] args, CancellationToken ct) =>
                  {
                      // Wait for cancellation
                      var tcs = new TaskCompletionSource<int>();
                      using (ct.Register(() => tcs.SetResult(-1)))
                      {
                          return await tcs.Task;
                      }
                  });
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
        
        // Act - Using a very short timeout to trigger cancellation
        var result = await _manager.ExecutePluginAsync(pluginName, new string[0], context.Object, TimeSpan.FromMilliseconds(1));
        
        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task UnloadPluginAsync_WithTokenCancellation_Fails()
    {
        // Arrange
        var pluginName = "CancelOnCleanupPlugin";
        
        // Create mock plugin with cancellation-sensitive cleanup
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
        
        // Note: In the real implementation, the method would time out after 30 seconds,
        // but for testing purposes, we're checking the logic flow
        // In a real scenario, this would return false due to the timeout
        
        // The test is confirming the method handles the scenario gracefully
        Assert.False(result);
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