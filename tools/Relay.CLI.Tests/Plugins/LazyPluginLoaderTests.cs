using Moq;
using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

#pragma warning disable CS0414
public class LazyPluginLoaderTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly Mock<IPluginContext> _mockContext;
    private readonly Mock<IRelayPlugin> _mockPlugin;
    private readonly LazyPluginLoader _loader;
    private readonly string _testPluginName = "TestPlugin";

    public LazyPluginLoaderTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _mockContext = new Mock<IPluginContext>();
        _mockPlugin = new Mock<IRelayPlugin>();

        // Create a real PluginManager for testing - the circular dependency is handled internally
        var pluginManager = new PluginManager(_mockLogger.Object);
        _loader = new LazyPluginLoader(pluginManager, _mockLogger.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Assert
        Assert.NotNull(_loader);
    }

    [Fact]
    public async Task GetPluginAsync_PluginNotFound_ReturnsNullAndLogsError()
    {
        // Act
        var result = await _loader.GetPluginAsync("NonExistentPlugin", _mockContext.Object);

        // Assert
        Assert.Null(result);
        _mockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("NonExistentPlugin")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task GetPluginAsync_SamePluginRequestedTwice_CachesResult()
    {
        // Act - Both calls should result in the same behavior (both fail or both succeed)
        var result1 = await _loader.GetPluginAsync("NonExistentPlugin", _mockContext.Object);
        var result2 = await _loader.GetPluginAsync("NonExistentPlugin", _mockContext.Object);

        // Assert - Both should be null since the plugin doesn't exist
        Assert.Null(result1);
        Assert.Null(result2);
        // The logger should only be called once due to caching
        _mockLogger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public void PreloadPlugin_AddsPluginToCache()
    {
        // Act
        _loader.PreloadPlugin("NonExistentPlugin", _mockContext.Object);

        // Assert
        var cacheInfo = _loader.GetCacheInfo();
        Assert.True(cacheInfo.ContainsKey("NonExistentPlugin"));
        Assert.False(cacheInfo["NonExistentPlugin"]); // Should not be loaded yet
    }

    [Fact]
    public async Task PreloadPlugin_ThenGetPlugin_UsesCache()
    {
        // Act
        _loader.PreloadPlugin("NonExistentPlugin", _mockContext.Object);
        var result = await _loader.GetPluginAsync("NonExistentPlugin", _mockContext.Object);

        // Assert
        Assert.Null(result);
        var cacheInfo = _loader.GetCacheInfo();
        Assert.True(cacheInfo.ContainsKey("NonExistentPlugin"));
        Assert.True(cacheInfo["NonExistentPlugin"]); // Should be loaded now
    }

    [Fact]
    public void ClearCache_RemovesAllCachedPlugins()
    {
        // Arrange
        _loader.PreloadPlugin("Plugin1", _mockContext.Object);
        _loader.PreloadPlugin("Plugin2", _mockContext.Object);

        // Act
        _loader.ClearCache();

        // Assert
        var cacheInfo = _loader.GetCacheInfo();
        Assert.Empty(cacheInfo);
    }

    [Fact]
    public void RemoveFromCache_ExistingPlugin_ReturnsTrue()
    {
        // Arrange
        _loader.PreloadPlugin("TestPlugin", _mockContext.Object);

        // Act
        var result = _loader.RemoveFromCache("TestPlugin");

        // Assert
        Assert.True(result);
        var cacheInfo = _loader.GetCacheInfo();
        Assert.DoesNotContain("TestPlugin", cacheInfo.Keys);
    }

    [Fact]
    public void RemoveFromCache_NonExistentPlugin_ReturnsFalse()
    {
        // Act
        var result = _loader.RemoveFromCache("NonExistentPlugin");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCacheInfo_EmptyCache_ReturnsEmptyDictionary()
    {
        // Act
        var result = _loader.GetCacheInfo();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCacheInfo_WithLoadedAndUnloadedPlugins_ReturnsCorrectStates()
    {
        // Arrange
        var unloadedPlugin = "UnloadedPlugin";

        _loader.PreloadPlugin(unloadedPlugin, _mockContext.Object);
        await _loader.GetPluginAsync("AnotherPlugin", _mockContext.Object);

        // Act
        var result = _loader.GetCacheInfo();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result["AnotherPlugin"]); // Should be loaded
        Assert.False(result[unloadedPlugin]); // Should not be loaded
    }

    [Fact]
    public async Task ExecutePluginAsync_PluginNotFound_ReturnsNegativeOne()
    {
        // Act
        var result = await _loader.ExecutePluginAsync("NonExistentPlugin", Array.Empty<string>(), _mockContext.Object);

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public async Task ExecutePluginAsync_WithTimeout_Executes()
    {
        // Arrange
        var customTimeout = TimeSpan.FromSeconds(30);

        // Act
        var result = await _loader.ExecutePluginAsync("NonExistentPlugin", Array.Empty<string>(), _mockContext.Object, customTimeout);

        // Assert
        Assert.Equal(-1, result); // Plugin doesn't exist, so should return -1
    }

    [Fact]
    public async Task ConcurrentAccess_MultipleThreadsAccessingSamePlugin_WorksCorrectly()
    {
        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => _loader.GetPluginAsync("ConcurrentPlugin", _mockContext.Object)));

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.Null(r)); // All should be null since plugin doesn't exist
        // The logger should only be called once due to caching
        _mockLogger.Verify(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentAccess_DifferentPlugins_WorkSeparately()
    {
        // Act
        var task1 = _loader.GetPluginAsync("Plugin1", _mockContext.Object);
        var task2 = _loader.GetPluginAsync("Plugin2", _mockContext.Object);

        await Task.WhenAll(task1, task2);

        // Assert - both should complete without errors
        var cacheInfo = _loader.GetCacheInfo();
        Assert.Equal(2, cacheInfo.Count);
        Assert.True(cacheInfo["Plugin1"]);
        Assert.True(cacheInfo["Plugin2"]);
    }
}
