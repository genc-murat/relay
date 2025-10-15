using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginInstallResultTests
{
    [Fact]
    public void DefaultConstructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var result = new PluginInstallResult();

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PluginName);
        Assert.Null(result.Version);
        Assert.Null(result.InstalledPath);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var result = new PluginInstallResult();

        // Act
        result.Success = true;
        result.PluginName = "TestPlugin";
        result.Version = "1.0.0";
        result.InstalledPath = "/plugins/test";
        result.Error = null;

        // Assert
        Assert.True(result.Success);
        Assert.Equal("TestPlugin", result.PluginName);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal("/plugins/test", result.InstalledPath);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Properties_CanBeSetToNull()
    {
        // Arrange
        var result = new PluginInstallResult
        {
            Success = true,
            PluginName = "TestPlugin",
            Version = "1.0.0",
            InstalledPath = "/plugins/test",
            Error = "Some error"
        };

        // Act
        result.PluginName = null;
        result.Version = null;
        result.InstalledPath = null;
        result.Error = null;

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.PluginName);
        Assert.Null(result.Version);
        Assert.Null(result.InstalledPath);
        Assert.Null(result.Error);
    }

    [Fact]
    public void CanCreateSuccessfulInstallResult()
    {
        // Act
        var result = new PluginInstallResult
        {
            Success = true,
            PluginName = "MyPlugin",
            Version = "2.1.0",
            InstalledPath = "C:\\Program Files\\Relay\\Plugins\\MyPlugin"
        };

        // Assert
        Assert.True(result.Success);
        Assert.Equal("MyPlugin", result.PluginName);
        Assert.Equal("2.1.0", result.Version);
        Assert.Equal("C:\\Program Files\\Relay\\Plugins\\MyPlugin", result.InstalledPath);
        Assert.Null(result.Error);
    }

    [Fact]
    public void CanCreateFailedInstallResult()
    {
        // Act
        var result = new PluginInstallResult
        {
            Success = false,
            PluginName = "FailedPlugin",
            Error = "Installation failed due to missing dependencies"
        };

        // Assert
        Assert.False(result.Success);
        Assert.Equal("FailedPlugin", result.PluginName);
        Assert.Equal("Installation failed due to missing dependencies", result.Error);
        Assert.Null(result.Version);
        Assert.Null(result.InstalledPath);
    }
}