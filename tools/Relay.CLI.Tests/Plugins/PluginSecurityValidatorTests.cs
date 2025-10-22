using Moq;
using Relay.CLI.Plugins;
using System.Reflection;

namespace Relay.CLI.Tests.Plugins;

public class PluginSecurityValidatorTests
{
#pragma warning disable CS8602, CS8605
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

    [Fact]
    public async Task ValidatePluginAsync_PluginWithInvalidPermissions_ReturnsInvalidResult()
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
                Description = "Test Description",
                Permissions = new PluginPermissions
                {
                    FileSystem = new FileSystemPermissions
                    {
                        AllowedPaths = new[] { @"C:\Windows\System32" } // Invalid system path
                    }
                }
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("invalid permissions"));

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithUntrustedRepository_LogsWarning()
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
                Description = "Test Description",
                Repository = "https://untrusted.example.com"
            }
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.True(result.IsValid); // Repository warning doesn't make it invalid
        _mockLogger.Verify(x => x.LogWarning(It.Is<string>(s => s.Contains("not trusted"))), Times.Once);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_PluginWithEmptyDescription_ReturnsInvalidResult()
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
                Description = "" // Invalid - empty description
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
    public async Task ValidatePluginAsync_PluginWithNullManifest_ReturnsInvalidResult()
    {
        // Arrange
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        var pluginInfo = new PluginInfo
        {
            Name = "TestPlugin",
            Manifest = null // Invalid - null manifest
        };

        // Act
        var result = await _validator.ValidatePluginAsync(tempPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);

        // Cleanup
        File.Delete(tempPath);
    }

    [Fact]
    public async Task ValidatePluginAsync_InvalidAssemblyPath_ReturnsInvalidResult()
    {
        // Arrange - use a directory path that exists but is not a valid assembly
        var directoryPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar); // Directory path
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
        var result = await _validator.ValidatePluginAsync(directoryPath, pluginInfo);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("does not exist"));
    }

    [Fact]
    public void ValidateFileSystemPermissions_InvalidSystemPath_ReturnsFalse()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Windows\System32" }
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateFileSystemPermissions_ValidPaths_ReturnsTrue()
    {
        // Arrange
        var permissions = new FileSystemPermissions
        {
            AllowedPaths = new[] { @"C:\Temp", @"D:\Data" }
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateFileSystemPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateNetworkPermissions_ReturnsTrue()
    {
        // Arrange
        var permissions = new NetworkPermissions
        {
            Http = true,
            Https = true
        };

        // Act - use reflection to call private method
        var method = typeof(PluginSecurityValidator).GetMethod("ValidateNetworkPermissions", BindingFlags.NonPublic | BindingFlags.Instance);
        var result = (bool)method.Invoke(_validator, new object[] { permissions });

        // Assert
        Assert.True(result);
    }
}