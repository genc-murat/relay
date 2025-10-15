using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginManifestTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var manifest = new PluginManifest();

        // Assert
        Assert.Equal("", manifest.Name);
        Assert.Equal("", manifest.Version);
        Assert.Equal("", manifest.Description);
        Assert.Empty(manifest.Authors);
        Assert.Empty(manifest.Tags);
        Assert.Equal("2.1.0", manifest.MinimumRelayVersion);
        Assert.NotNull(manifest.Dependencies);
        Assert.Empty(manifest.Dependencies);
        Assert.Null(manifest.Repository);
        Assert.Null(manifest.License);
        Assert.Null(manifest.Permissions);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var manifest = new PluginManifest();

        // Act
        manifest.Name = "TestPlugin";
        manifest.Version = "1.0.0";
        manifest.Description = "A test plugin";
        manifest.Authors = new[] { "Author1", "Author2" };
        manifest.Tags = new[] { "tag1", "tag2" };
        manifest.MinimumRelayVersion = "3.0.0";
        manifest.Dependencies = new Dictionary<string, string> { { "dep1", "1.0.0" } };
        manifest.Repository = "https://github.com/test/repo";
        manifest.License = "MIT";
        manifest.Permissions = new PluginPermissions();

        // Assert
        Assert.Equal("TestPlugin", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("A test plugin", manifest.Description);
        Assert.Equal(new[] { "Author1", "Author2" }, manifest.Authors);
        Assert.Equal(new[] { "tag1", "tag2" }, manifest.Tags);
        Assert.Equal("3.0.0", manifest.MinimumRelayVersion);
        Assert.Equal(new Dictionary<string, string> { { "dep1", "1.0.0" } }, manifest.Dependencies);
        Assert.Equal("https://github.com/test/repo", manifest.Repository);
        Assert.Equal("MIT", manifest.License);
        Assert.NotNull(manifest.Permissions);
    }

    [Fact]
    public void CanCreateCompleteManifest()
    {
        // Act
        var manifest = new PluginManifest
        {
            Name = "MyAwesomePlugin",
            Version = "2.1.0",
            Description = "An awesome plugin for Relay CLI",
            Authors = new[] { "John Doe", "Jane Smith" },
            Tags = new[] { "utility", "productivity" },
            MinimumRelayVersion = "2.1.0",
            Dependencies = new Dictionary<string, string>
            {
                { "Newtonsoft.Json", "13.0.1" },
                { "System.Text.Json", "6.0.0" }
            },
            Repository = "https://github.com/company/my-awesome-plugin",
            License = "Apache-2.0",
            Permissions = new PluginPermissions
            {
                FileSystem = new FileSystemPermissions
                {
                    Read = true,
                    Write = false
                }
            }
        };

        // Assert
        Assert.Equal("MyAwesomePlugin", manifest.Name);
        Assert.Equal("2.1.0", manifest.Version);
        Assert.Equal("An awesome plugin for Relay CLI", manifest.Description);
        Assert.Equal(2, manifest.Authors.Length);
        Assert.Equal(2, manifest.Tags.Length);
        Assert.Equal(2, manifest.Dependencies.Count);
        Assert.Equal("https://github.com/company/my-awesome-plugin", manifest.Repository);
        Assert.Equal("Apache-2.0", manifest.License);
        Assert.NotNull(manifest.Permissions);
        Assert.NotNull(manifest.Permissions.FileSystem);
    }

    [Fact]
    public void DependenciesDictionary_IsMutable()
    {
        // Arrange
        var manifest = new PluginManifest();

        // Act
        manifest.Dependencies.Add("TestDep", "1.0.0");
        manifest.Dependencies["AnotherDep"] = "2.0.0";

        // Assert
        Assert.Equal(2, manifest.Dependencies.Count);
        Assert.Equal("1.0.0", manifest.Dependencies["TestDep"]);
        Assert.Equal("2.0.0", manifest.Dependencies["AnotherDep"]);
    }

    [Fact]
    public void Arrays_AreMutable()
    {
        // Arrange
        var manifest = new PluginManifest();

        // Act
        manifest.Authors = new[] { "Author1" };
        manifest.Tags = new[] { "Tag1" };

        // Assert
        Assert.Single(manifest.Authors);
        Assert.Single(manifest.Tags);
        Assert.Equal("Author1", manifest.Authors[0]);
        Assert.Equal("Tag1", manifest.Tags[0]);
    }
}