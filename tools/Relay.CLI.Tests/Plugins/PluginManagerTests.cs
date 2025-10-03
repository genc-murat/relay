using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginManagerTests
{
    private readonly PluginManager _pluginManager;

    public PluginManagerTests()
    {
        _pluginManager = new PluginManager();
    }

    [Fact]
    public async Task GetInstalledPluginsAsync_WithNoPlugins_ReturnsEmptyList()
    {
        // Act
        var plugins = await _pluginManager.GetInstalledPluginsAsync();

        // Assert
        plugins.Should().NotBeNull();
    }
}
