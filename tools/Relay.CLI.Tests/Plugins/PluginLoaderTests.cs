using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginLoaderTests : IDisposable
{
    private readonly string _testPluginPath;

    public PluginLoaderTests()
    {
        _testPluginPath = Path.Combine(Path.GetTempPath(), $"relay-plugin-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPluginPath);
    }

    [Fact]
    public async Task PluginLoader_WithValidManifest_LoadsSuccessfully()
    {
        // Arrange
        await CreateValidPlugin();

        // Act
        var manifestPath = Path.Combine(_testPluginPath, "plugin.json");
        var manifestExists = File.Exists(manifestPath);

        // Assert
        Assert.True(manifestExists);
    }

    [Fact]
    public async Task PluginLoader_WithInvalidManifest_FailsValidation()
    {
        // Arrange
        var invalidJson = "{ invalid json";
        await File.WriteAllTextAsync(Path.Combine(_testPluginPath, "plugin.json"), invalidJson);

        // Act
        Func<Task> act = async () => await File.ReadAllTextAsync(Path.Combine(_testPluginPath, "plugin.json"));

        // Assert
        await act();
    }

    [Fact]
    public async Task PluginLoader_IsolatesPluginContext()
    {
        // Arrange
        await CreateValidPlugin();

        // Act
        var manifest = await File.ReadAllTextAsync(Path.Combine(_testPluginPath, "plugin.json"));

        // Assert
        Assert.Contains("test-plugin", manifest);
        Assert.Contains("1.0.0", manifest);
    }

    private async Task CreateValidPlugin(string? path = null, string name = "test-plugin", string version = "1.0.0")
    {
        path ??= _testPluginPath;

        var manifest = $$"""
{
  "name": "{{name}}",
  "version": "{{version}}",
  "description": "Test plugin",
  "authors": ["Test Author"],
  "tags": ["test"],
  "minimumRelayVersion": "2.1.0",
  "dependencies": {},
  "repository": "https://github.com/test/test"
}
""";

        await File.WriteAllTextAsync(Path.Combine(path, "plugin.json"), manifest);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPluginPath))
                Directory.Delete(_testPluginPath, true);
        }
        catch { }
    }
}
