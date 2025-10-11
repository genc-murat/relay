using Moq;

namespace Relay.CLI.Plugins.Tests;

public class PluginFileSystemTests
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginSandbox _sandbox;

    public PluginFileSystemTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _sandbox = new PluginSandbox(_mockLogger.Object);
    }

    [Fact]
    public async Task FileExistsAsync_WithValidPermissions_ReturnsResult()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempFile) }
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act
        var result = await fileSystem.FileExistsAsync(tempFile);

        // Assert
        Assert.True(result);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task FileExistsAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false // Read not allowed
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fileSystem.FileExistsAsync(tempFile));

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task FileExistsAsync_WithPathNotAllowed_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { @"C:\AllowedPath" } // Different path
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fileSystem.FileExistsAsync(tempFile));

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ReadFileAsync_WithValidPermissions_ReturnsContent()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var expectedContent = "test content";
        await File.WriteAllTextAsync(tempFile, expectedContent);
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempFile) }
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act
        var result = await fileSystem.ReadFileAsync(tempFile);

        // Assert
        Assert.Equal(expectedContent, result);

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task ReadFileAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test content");
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false // Read not allowed
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fileSystem.ReadFileAsync(tempFile));

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public async Task WriteFileAsync_WithValidPermissions_WritesContent()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test_write.txt");
        var content = "test write content";
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempFile) }
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            await fileSystem.WriteFileAsync(tempFile, content);

            // Assert
            var writtenContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(content, writtenContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task WriteFileAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test_write_unauth.txt");
        var content = "test write content";
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = false // Write not allowed
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => fileSystem.WriteFileAsync(tempFile, content));
    }

    [Fact]
    public async Task DeleteFileAsync_WithValidPermissions_DeletesFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "test content");
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Delete = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempFile) }
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act
        await fileSystem.DeleteFileAsync(tempFile);

        // Assert
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task CopyFileAsync_WithValidPermissions_CopiesFile()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "dest_copy.txt");
        var content = "test copy content";
        await File.WriteAllTextAsync(sourceFile, content);
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                Write = true,
                AllowedPaths = new[] { Path.GetDirectoryName(sourceFile) }
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            await fileSystem.CopyFileAsync(sourceFile, destFile);

            // Assert
            Assert.True(File.Exists(destFile));
            var copiedContent = await File.ReadAllTextAsync(destFile);
            Assert.Equal(content, copiedContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }

    [Fact]
    public async Task CopyFileAsync_WithoutSourcePermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "dest_copy.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false, // Read not allowed for source
                Write = true  // Write allowed for destination
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.CopyFileAsync(sourceFile, destFile));
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }

    [Fact]
    public async Task CopyFileAsync_WithoutDestinationPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "dest_copy.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");
        
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,  // Read allowed for source
                Write = false  // Write not allowed for destination
            }
        };
        
        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.CopyFileAsync(sourceFile, destFile));
        }
        finally
        {
            // Cleanup
            if (File.Exists(sourceFile))
                File.Delete(sourceFile);
            if (File.Exists(destFile))
                File.Delete(destFile);
        }
    }
}