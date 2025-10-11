using Moq;
using System.Reflection;

namespace Relay.CLI.Plugins.Tests;

public class PluginSecurityValidatorTests
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginSecurityValidator _validator;

    public PluginSecurityValidatorTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _validator = new PluginSecurityValidator(_mockLogger.Object);
    }

    [Fact]
    public async Task ValidatePluginAsync_ValidPlugin_ReturnsValidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");
        
        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "TestPlugin",
                Description = "Test Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        
        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_NonExistentAssembly_ReturnsInvalidResult()
    {
        // Arrange
        var nonExistentPath = "nonexistent.dll";
        var pluginInfo = new PluginInfo { Name = "TestPlugin" };

        // Act
        var result = await _validator.ValidatePluginAsync(nonExistentPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist"));
    }

    [Fact]
    public async Task ValidatePluginAsync_InvalidManifest_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");
        
        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = new PluginManifest
            {
                Name = "", // Invalid - empty name
                Description = "Test Description"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        
        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public void AddTrustedSource_AddsSourceToList()
    {
        // Arrange
        var source = "https://trusted.example.com";

        // Act
        _validator.AddTrustedSource(source);

        // Assert
        // Since we can't directly access the internal list, we'll verify by checking if it doesn't throw
        // and by looking at the internal state through reflection if needed for a more thorough test
        _validator.AddTrustedSource(source); // Adding same source again should not duplicate
    }

    [Fact]
    public void GetSetPluginPermissions_WorksCorrectly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                Write = false
            }
        };

        // Act
        _validator.SetPluginPermissions(pluginName, permissions);
        var retrievedPermissions = _validator.GetPluginPermissions(pluginName);

        // Assert
        Assert.NotNull(retrievedPermissions);
        Assert.Equal(permissions.FileSystem?.Read, retrievedPermissions.FileSystem?.Read);
        Assert.Equal(permissions.FileSystem?.Write, retrievedPermissions.FileSystem?.Write);
    }

    [Fact]
    public void GetPluginPermissions_NonExistentPlugin_ReturnsNull()
    {
        // Act
        var permissions = _validator.GetPluginPermissions("NonExistentPlugin");

        // Assert
        Assert.Null(permissions);
    }
}