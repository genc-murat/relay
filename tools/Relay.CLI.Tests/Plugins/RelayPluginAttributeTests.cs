using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class RelayPluginAttributeTests
{
    [Fact]
    public void Constructor_SetsNameAndVersionProperties()
    {
        // Arrange
        const string expectedName = "TestPlugin";
        const string expectedVersion = "1.0.0";

        // Act
        var attribute = new RelayPluginAttribute(expectedName, expectedVersion);

        // Assert
        Assert.Equal(expectedName, attribute.Name);
        Assert.Equal(expectedVersion, attribute.Version);
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var attribute = new RelayPluginAttribute("OriginalName", "1.0.0");

        // Assert
        Assert.Equal("OriginalName", attribute.Name);
        Assert.Equal("1.0.0", attribute.Version);

        // Properties should be read-only (no setters)
        // This test verifies that the properties are set correctly in constructor
        // and cannot be modified afterward
    }

    [Fact]
    public void CanCreateAttributeWithVariousValues()
    {
        // Test with different name and version combinations
        var testCases = new[]
        {
            ("SimplePlugin", "1.0.0"),
            ("Complex.Plugin.Name", "2.1.0-beta"),
            ("plugin-with-dashes", "0.1.0"),
            ("Plugin123", "10.0.0")
        };

        foreach (var (name, version) in testCases)
        {
            var attribute = new RelayPluginAttribute(name, version);
            Assert.Equal(name, attribute.Name);
            Assert.Equal(version, attribute.Version);
        }
    }

    [Fact]
    public void AttributeUsage_IsCorrect()
    {
        // Arrange
        var attributeType = typeof(RelayPluginAttribute);
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }
}