using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginInfoTests
{
    [Fact]
    public void Constructor_InitializesPropertiesWithDefaults()
    {
        // Act
        var pluginInfo = new PluginInfo();

        // Assert
        Assert.Equal("", pluginInfo.Name);
        Assert.Equal("", pluginInfo.Version);
        Assert.Equal("", pluginInfo.Description);
        Assert.Empty(pluginInfo.Authors);
        Assert.Empty(pluginInfo.Tags);
        Assert.Equal("", pluginInfo.Path);
        Assert.False(pluginInfo.IsGlobal);
        Assert.False(pluginInfo.Enabled);
        Assert.Null(pluginInfo.Manifest);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var pluginInfo = new PluginInfo();
        var expectedName = "TestPlugin";
        var expectedVersion = "1.0.0";
        var expectedDescription = "A test plugin";
        var expectedAuthors = new[] { "Author1", "Author2" };
        var expectedTags = new[] { "tag1", "tag2" };
        var expectedPath = "/path/to/plugin";
        var expectedIsGlobal = true;
        var expectedEnabled = true;
        var expectedManifest = new PluginManifest { Name = "TestPlugin" };

        // Act
        pluginInfo.Name = expectedName;
        pluginInfo.Version = expectedVersion;
        pluginInfo.Description = expectedDescription;
        pluginInfo.Authors = expectedAuthors;
        pluginInfo.Tags = expectedTags;
        pluginInfo.Path = expectedPath;
        pluginInfo.IsGlobal = expectedIsGlobal;
        pluginInfo.Enabled = expectedEnabled;
        pluginInfo.Manifest = expectedManifest;

        // Assert
        Assert.Equal(expectedName, pluginInfo.Name);
        Assert.Equal(expectedVersion, pluginInfo.Version);
        Assert.Equal(expectedDescription, pluginInfo.Description);
        Assert.Equal(expectedAuthors, pluginInfo.Authors);
        Assert.Equal(expectedTags, pluginInfo.Tags);
        Assert.Equal(expectedPath, pluginInfo.Path);
        Assert.Equal(expectedIsGlobal, pluginInfo.IsGlobal);
        Assert.Equal(expectedEnabled, pluginInfo.Enabled);
        Assert.Equal(expectedManifest, pluginInfo.Manifest);
    }

    [Fact]
    public void Authors_Property_HandlesNullAssignment()
    {
        // Arrange
        var pluginInfo = new PluginInfo();

        // Act
        pluginInfo.Authors = null!;

        // Assert
        Assert.Null(pluginInfo.Authors);
    }

    [Fact]
    public void Tags_Property_HandlesNullAssignment()
    {
        // Arrange
        var pluginInfo = new PluginInfo();

        // Act
        pluginInfo.Tags = null!;

        // Assert
        Assert.Null(pluginInfo.Tags);
    }

    [Fact]
    public void Manifest_Property_CanBeSetToNull()
    {
        // Arrange
        var pluginInfo = new PluginInfo();
        pluginInfo.Manifest = new PluginManifest();

        // Act
        pluginInfo.Manifest = null;

        // Assert
        Assert.Null(pluginInfo.Manifest);
    }

    [Fact]
    public void StringProperties_CanBeSetToNull()
    {
        // Arrange
        var pluginInfo = new PluginInfo();

        // Act
        pluginInfo.Name = null!;
        pluginInfo.Version = null!;
        pluginInfo.Description = null!;
        pluginInfo.Path = null!;

        // Assert
        Assert.Null(pluginInfo.Name);
        Assert.Null(pluginInfo.Version);
        Assert.Null(pluginInfo.Description);
        Assert.Null(pluginInfo.Path);
    }
}
