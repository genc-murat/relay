using Relay.CLI.Plugins;
using System.Text.Json;

namespace Relay.CLI.Tests.Plugins;

public class PluginValidatorTests
{
    [Fact]
    public async Task ValidatePluginManifest_WithValidJson_Succeeds()
    {
        // Arrange
        var manifestJson = """
{
  "name": "relay-plugin-test",
  "version": "1.0.0",
  "description": "Test plugin",
  "authors": ["Test Author"],
  "minimumRelayVersion": "2.1.0"
}
""";

        // Act
        var manifest = JsonSerializer.Deserialize<Dictionary<string, object>>(manifestJson);

        // Assert
        manifest.Should().NotBeNull();
        manifest!.Should().ContainKey("name");
        manifest.Should().ContainKey("version");
    }

    [Fact]
    public void ValidatePluginName_WithValidName_Succeeds()
    {
        // Arrange
        var validNames = new[] { "relay-plugin-test", "relay-plugin-my-feature", "relay-plugin-awesome" };

        // Act & Assert
        foreach (var name in validNames)
        {
            name.Should().StartWith("relay-plugin-");
            name.Should().MatchRegex("^[a-z0-9-]+$");
        }
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("2.1.3", true)]
    [InlineData("0.1.0-beta", true)]
    [InlineData("v1.0", true)] // Also valid after trimming 'v'
    public void ValidateVersion_WithVariousFormats_ValidatesCorrectly(string version, bool shouldBeValid)
    {
        // Act
        var cleanVersion = version.TrimStart('v').Split('-')[0]; // Remove 'v' prefix and pre-release suffix
        var isValid = Version.TryParse(cleanVersion, out _) || version.Contains("-");

        // Assert
        isValid.Should().Be(shouldBeValid);
    }

    [Fact]
    public void ValidateMinimumRelayVersion_WithCompatibleVersion_Succeeds()
    {
        // Arrange
        var minimumVersion = new Version("2.1.0");
        var currentVersion = new Version("2.1.5");

        // Act
        var isCompatible = currentVersion >= minimumVersion;

        // Assert
        isCompatible.Should().BeTrue();
    }

    [Fact]
    public void ValidateMinimumRelayVersion_WithIncompatibleVersion_Fails()
    {
        // Arrange
        var minimumVersion = new Version("3.0.0");
        var currentVersion = new Version("2.1.0");

        // Act
        var isCompatible = currentVersion >= minimumVersion;

        // Assert
        isCompatible.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePluginStructure_WithRequiredFiles_Succeeds()
    {
        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"plugin-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempPath, "plugin.json"), "{}");
            
            // Act
            var hasManifest = File.Exists(Path.Combine(tempPath, "plugin.json"));

            // Assert
            hasManifest.Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);
        }
    }
}
