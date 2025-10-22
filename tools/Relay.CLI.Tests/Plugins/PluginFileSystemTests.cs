using Moq;
using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

#pragma warning disable CS8604, CS8619, CS8625
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

    [Fact]
    public async Task DirectoryExistsAsync_WithValidPermissions_ReturnsResult()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_dir_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempDir) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            var result = await fileSystem.DirectoryExistsAsync(tempDir);

            // Assert
            Assert.True(result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task DirectoryExistsAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_dir_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false // Read not allowed
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.DirectoryExistsAsync(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task GetFilesAsync_WithValidPermissions_ReturnsFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_files_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);
        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(tempDir, "file2.txt");
        await File.WriteAllTextAsync(file1, "content1");
        await File.WriteAllTextAsync(file2, "content2");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { tempDir }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            var result = await fileSystem.GetFilesAsync(tempDir, "*.txt");

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(file1, result);
            Assert.Contains(file2, result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetFilesAsync_WithRecursiveSearch_ReturnsNestedFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_recursive_" + Guid.NewGuid());
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir);

        var file1 = Path.Combine(tempDir, "file1.txt");
        var file2 = Path.Combine(subDir, "file2.txt");
        await File.WriteAllTextAsync(file1, "content1");
        await File.WriteAllTextAsync(file2, "content2");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { tempDir }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            var result = await fileSystem.GetFilesAsync(tempDir, "*.txt", recursive: true);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(file1, result);
            Assert.Contains(file2, result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetFilesAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_files_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false // Read not allowed
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.GetFilesAsync(tempDir, "*.txt"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task GetDirectoriesAsync_WithValidPermissions_ReturnsDirectories()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_dirs_" + Guid.NewGuid());
        var subDir1 = Path.Combine(tempDir, "subdir1");
        var subDir2 = Path.Combine(tempDir, "subdir2");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir1);
        Directory.CreateDirectory(subDir2);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { tempDir }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            var result = await fileSystem.GetDirectoriesAsync(tempDir);

            // Assert
            Assert.Equal(2, result.Length);
            Assert.Contains(subDir1, result);
            Assert.Contains(subDir2, result);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetDirectoriesAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_dirs_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false // Read not allowed
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.GetDirectoriesAsync(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithValidPermissions_CreatesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_create_" + Guid.NewGuid());

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempDir) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            await fileSystem.CreateDirectoryAsync(tempDir);

            // Assert
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task CreateDirectoryAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_create_" + Guid.NewGuid());

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
            () => fileSystem.CreateDirectoryAsync(tempDir));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_WithValidPermissions_DeletesDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_delete_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Delete = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempDir) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act
        await fileSystem.DeleteDirectoryAsync(tempDir);

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_WithRecursiveDelete_DeletesNonEmptyDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_delete_recursive_" + Guid.NewGuid());
        var subDir = Path.Combine(tempDir, "subdir");
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(subDir);
        var file = Path.Combine(subDir, "file.txt");
        await File.WriteAllTextAsync(file, "content");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Delete = true,
                AllowedPaths = new[] { Path.GetDirectoryName(tempDir) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act
        await fileSystem.DeleteDirectoryAsync(tempDir, recursive: true);

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_WithoutPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "test_delete_" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Delete = false // Delete not allowed
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.DeleteDirectoryAsync(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [Fact]
    public async Task MoveFileAsync_WithValidPermissions_MovesFile()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "moved_file.txt");
        var content = "test move content";
        await File.WriteAllTextAsync(sourceFile, content);

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = true,
                AllowedPaths = new[] { Path.GetDirectoryName(sourceFile) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act
            await fileSystem.MoveFileAsync(sourceFile, destFile);

            // Assert
            Assert.False(File.Exists(sourceFile));
            Assert.True(File.Exists(destFile));
            var movedContent = await File.ReadAllTextAsync(destFile);
            Assert.Equal(content, movedContent);
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
    public async Task MoveFileAsync_WithoutSourcePermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "moved_file.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = false // Write not allowed for source
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.MoveFileAsync(sourceFile, destFile));
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
    public async Task MoveFileAsync_WithoutDestinationPermissions_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var sourceFile = Path.GetTempFileName();
        var destFile = Path.Combine(Path.GetDirectoryName(sourceFile), "moved_file.txt");
        await File.WriteAllTextAsync(sourceFile, "test content");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Write = true,
                AllowedPaths = new[] { @"C:\AllowedPath" } // Different path for destination
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.MoveFileAsync(sourceFile, destFile));
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
    public async Task FileExistsAsync_WithDeniedPath_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { @"C:\AllowedPath" },
                DeniedPaths = new[] { Path.GetDirectoryName(tempFile) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => fileSystem.FileExistsAsync(tempFile));
        }
        finally
        {
            // Cleanup
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".txt");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { Path.GetDirectoryName(nonExistentFile) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => fileSystem.ReadFileAsync(nonExistentFile));
    }

    [Fact]
    public async Task WriteFileAsync_OverwritesExistingFile()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test_overwrite.txt");
        await File.WriteAllTextAsync(tempFile, "original content");
        var newContent = "new content";

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
            await fileSystem.WriteFileAsync(tempFile, newContent);

            // Assert
            var writtenContent = await File.ReadAllTextAsync(tempFile);
            Assert.Equal(newContent, writtenContent);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task DeleteFileAsync_WithNonExistentFile_DoesNotThrow()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".txt");

        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Delete = true,
                AllowedPaths = new[] { Path.GetDirectoryName(nonExistentFile) }
            }
        };

        var sandboxWithPerms = new PluginSandbox(_mockLogger.Object, permissions);
        var fileSystem = new PluginFileSystem(_mockLogger.Object, sandboxWithPerms);

        // Act & Assert - Should not throw
        await fileSystem.DeleteFileAsync(nonExistentFile);
    }

    [Fact]
    public void Constructor_WithNullLogger_Works()
    {
        // Act
        var fileSystem = new PluginFileSystem(null);

        // Assert
        Assert.NotNull(fileSystem);
        // Note: The constructor doesn't validate logger parameter
    }

    [Fact]
    public void Constructor_WithLoggerAndNullSandbox_Works()
    {
        // Act
        var fileSystem = new PluginFileSystem(_mockLogger.Object, null);

        // Assert
        Assert.NotNull(fileSystem);
    }
}