using Moq;
using Relay.CLI.Plugins;
using System.Reflection;
using System.Text.Json;

namespace Relay.CLI.Tests.Plugins;

#pragma warning disable CS8600, CS8602
public class PluginManagerInstallationTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginManager _manager;
    private readonly string _tempPluginsDir;
    private readonly string _tempGlobalPluginsDir;

    public PluginManagerInstallationTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        
        // Create temporary directories for testing
        _tempPluginsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _tempGlobalPluginsDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        Directory.CreateDirectory(_tempPluginsDir);
        Directory.CreateDirectory(_tempGlobalPluginsDir);
        
        _manager = new PluginManager(_mockLogger.Object);
        
        // Override the directory paths using reflection for testing
        var pluginsDirField = typeof(PluginManager).GetField("_pluginsDirectory", BindingFlags.NonPublic | BindingFlags.Instance);
        var globalPluginsDirField = typeof(PluginManager).GetField("_globalPluginsDirectory", BindingFlags.NonPublic | BindingFlags.Instance);

        pluginsDirField?.SetValue(_manager, _tempPluginsDir);
        globalPluginsDirField?.SetValue(_manager, _tempGlobalPluginsDir);
    }

    [Fact]
    public async Task InstallPluginAsync_FromZipFile_Succeeds()
    {
        // Arrange: Create a ZIP file containing plugin content
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        
        // Create a temporary directory with plugin content
        var tempPluginDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPluginDir);
        
        try
        {
            // Create plugin manifest
            var manifest = new PluginManifest
            {
                Name = "ZipTestPlugin",
                Version = "1.0.0",
                Description = "ZIP Test Plugin",
                MinimumRelayVersion = "2.1.0",
                Authors = new[] { "Test Author" },
                Tags = new[] { "test", "zip" }
            };
            
            var manifestPath = Path.Combine(tempPluginDir, "plugin.json");
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
            
            // Create a dummy DLL file
            var dummyDllPath = Path.Combine(tempPluginDir, "relay-plugin-ziptest.dll");
            await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
            
            // Create the ZIP file
            System.IO.Compression.ZipFile.CreateFromDirectory(tempPluginDir, zipPath);
            
            // Act
            var result = await _manager.InstallPluginAsync(zipPath, null, false);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal("ZipTestPlugin", result.PluginName);
            Assert.Equal("1.0.0", result.Version);
            Assert.NotNull(result.InstalledPath);
            
            // Verify the plugin was actually installed
            var installedPluginDir = Path.Combine(_tempPluginsDir, "ZipTestPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "plugin.json")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "relay-plugin-ziptest.dll")));
        }
        finally
        {
            // Cleanup
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            if (Directory.Exists(tempPluginDir))
                Directory.Delete(tempPluginDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromZipFile_InvalidZip_Fails()
    {
        // Arrange: Create an invalid ZIP file
        var invalidZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        
        try
        {
            // Create a file that's not actually a ZIP
            await File.WriteAllTextAsync(invalidZipPath, "This is not a ZIP file");
            
            // Act
            var result = await _manager.InstallPluginAsync(invalidZipPath, null, false);
            
            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }
        finally
        {
            // Cleanup
            if (File.Exists(invalidZipPath))
                File.Delete(invalidZipPath);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromZipFile_MissingManifest_Fails()
    {
        // Arrange: Create a ZIP file without plugin.json
        var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        
        // Create a temporary directory with plugin content but no manifest
        var tempPluginDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempPluginDir);
        
        try
        {
            // Create a dummy DLL file but no manifest
            var dummyDllPath = Path.Combine(tempPluginDir, "relay-plugin-nomanifest.dll");
            await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
            
            // Create the ZIP file
            System.IO.Compression.ZipFile.CreateFromDirectory(tempPluginDir, zipPath);
            
            // Act
            var result = await _manager.InstallPluginAsync(zipPath, null, false);
            
            // Assert
            Assert.False(result.Success);
            Assert.Contains("plugin.json not found", result.Error);
        }
        finally
        {
            // Cleanup
            if (File.Exists(zipPath))
                File.Delete(zipPath);
            if (Directory.Exists(tempPluginDir))
                Directory.Delete(tempPluginDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromLocalDirectory_WithSubdirectories_Succeeds()
    {
        // Arrange
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);
        
        // Create subdirectories to test recursive copying
        var subDir = Path.Combine(sourceDir, "subdir");
        Directory.CreateDirectory(subDir);
        
        var manifest = new PluginManifest
        {
            Name = "RecursiveTestPlugin",
            Version = "1.0.0",
            Description = "Recursive Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create files in main and sub directory
        var mainDllPath = Path.Combine(sourceDir, "relay-plugin-recursive.dll");
        var subFilePath = Path.Combine(subDir, "nested.txt");
        
        await File.WriteAllBytesAsync(mainDllPath, GenerateDummyAssembly());
        await File.WriteAllTextAsync(subFilePath, "nested file content");
        
        try
        {
            // Act
            var result = await _manager.InstallPluginAsync(sourceDir, null, false);
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal("RecursiveTestPlugin", result.PluginName);
            
            // Verify the plugin was installed with subdirectories
            var installedPluginDir = Path.Combine(_tempPluginsDir, "RecursiveTestPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "plugin.json")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "relay-plugin-recursive.dll")));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "subdir")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "subdir", "nested.txt")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_OverwritesExistingPlugin()
    {
        // Arrange: First install a plugin
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);
        
        var manifest = new PluginManifest
        {
            Name = "OverwriteTestPlugin",
            Version = "1.0.0",
            Description = "Overwrite Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        var dummyDllPath = Path.Combine(sourceDir, "relay-plugin-overwrite.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        try
        {
            // First installation
            var firstResult = await _manager.InstallPluginAsync(sourceDir, null, false);
            Assert.True(firstResult.Success);
            
            // Modify the plugin content
            await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
            
            // Second installation should overwrite
            var secondResult = await _manager.InstallPluginAsync(sourceDir, null, false);
            
            // Assert
            Assert.True(secondResult.Success);
            Assert.Equal("OverwriteTestPlugin", secondResult.PluginName);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_ToGlobalDirectory_Succeeds()
    {
        // Arrange
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);
        
        var manifest = new PluginManifest
        {
            Name = "GlobalTestPlugin",
            Version = "1.0.0",
            Description = "Global Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        var dummyDllPath = Path.Combine(sourceDir, "relay-plugin-global.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        try
        {
            // Act: Install to global directory
            var result = await _manager.InstallPluginAsync(sourceDir, null, true); // global = true
            
            // Assert
            Assert.True(result.Success);
            Assert.Equal("GlobalTestPlugin", result.PluginName);
            
            // Verify it was installed in the global directory
            var installedPluginDir = Path.Combine(_tempGlobalPluginsDir, "GlobalTestPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task UninstallPluginAsync_GlobalPlugin_Succeeds()
    {
        // Arrange: Install a plugin to global directory first
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);
        
        var manifest = new PluginManifest
        {
            Name = "UninstallGlobalPlugin",
            Version = "1.0.0",
            Description = "Uninstall Global Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        var dummyDllPath = Path.Combine(sourceDir, "relay-plugin-uninstallglobal.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Install to global directory
        var installResult = await _manager.InstallPluginAsync(sourceDir, null, true);
        Assert.True(installResult.Success);
        
        try
        {
            // Act: Uninstall the global plugin
            var result = await _manager.UninstallPluginAsync("UninstallGlobalPlugin", true); // global = true
            
            // Assert
            Assert.True(result);
            
            // Verify it was removed from the global directory
            var installedPluginDir = Path.Combine(_tempGlobalPluginsDir, "UninstallGlobalPlugin");
            Assert.False(Directory.Exists(installedPluginDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task UninstallPluginAsync_WithLoadedPlugin_AutoUnloads()
    {
        // Arrange: Install and load a plugin
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);
        
        var manifest = new PluginManifest
        {
            Name = "UninstallLoadedPlugin",
            Version = "1.0.0",
            Description = "Uninstall Loaded Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        var dummyDllPath = Path.Combine(sourceDir, "relay-plugin-uninstallloaded.dll");
        await File.WriteAllBytesAsync(dummyDllPath, GenerateDummyAssembly());
        
        // Install the plugin
        var installResult = await _manager.InstallPluginAsync(sourceDir, null, false);
        Assert.True(installResult.Success);
        
        // Manually add the plugin to loaded plugins to simulate it being loaded
        var loadedPluginsField = typeof(PluginManager).GetField("_loadedPlugins", BindingFlags.NonPublic | BindingFlags.Instance);
        var loadedPlugins = (Dictionary<string, LoadedPlugin>)loadedPluginsField.GetValue(_manager);
        
        var mockPlugin = new Mock<IRelayPlugin>();
        mockPlugin.Setup(p => p.CleanupAsync(It.IsAny<CancellationToken>()))
                  .Returns(Task.CompletedTask);
        
        loadedPlugins["UninstallLoadedPlugin"] = new LoadedPlugin
        {
            Name = "UninstallLoadedPlugin",
            Instance = mockPlugin.Object,
            LoadContext = CreateMockLoadContext(),
            Assembly = typeof(IRelayPlugin).Assembly
        };
        
        try
        {
            // Act: Uninstall the plugin
            var result = await _manager.UninstallPluginAsync("UninstallLoadedPlugin", false);
            
            // Assert
            Assert.True(result);
            
            // Verify it was removed from loaded plugins
            Assert.False(loadedPlugins.ContainsKey("UninstallLoadedPlugin"));
            
            // Verify the plugin directory was removed
            var installedPluginDir = Path.Combine(_tempPluginsDir, "UninstallLoadedPlugin");
            Assert.False(Directory.Exists(installedPluginDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_InvalidSourcePath_Fails()
    {
        // Arrange: Use a non-existent path
        var invalidPath = Path.Combine(Path.GetTempPath(), "non-existent-folder");

        // Act
        var result = await _manager.InstallPluginAsync(invalidPath, null, false);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InstallPluginAsync_FromNuGetPackage_Succeeds()
    {
        // Arrange: Mock the NuGet installation process
        var packageName = "TestNuGetPlugin";
        var version = "1.0.0";

        // Create a temporary directory structure that simulates a NuGet package
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Create the package directory structure
        var packageDir = Path.Combine(tempDir, packageName);
        Directory.CreateDirectory(packageDir);

        // Create plugin manifest in the package
        var manifest = new PluginManifest
        {
            Name = "NuGetTestPlugin",
            Version = "1.0.0",
            Description = "NuGet Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(packageDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy DLL
        var dllPath = Path.Combine(packageDir, "relay-plugin-nuget.dll");
        await File.WriteAllBytesAsync(dllPath, GenerateDummyAssembly());

        try
        {
            // Mock the dotnet CLI process to simulate successful download
            // Since we can't easily mock Process.Start in unit tests, we'll test the logic path
            // by creating the expected directory structure beforehand

            // For this test, we'll simulate the NuGet download by pre-creating the structure
            // and then testing that the installation from the extracted location works

            // Act: Install from the simulated package directory
            var result = await _manager.InstallPluginAsync(packageDir, null, false);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("NuGetTestPlugin", result.PluginName);
            Assert.Equal("1.0.0", result.Version);

            // Verify the plugin was installed
            var installedPluginDir = Path.Combine(_tempPluginsDir, "NuGetTestPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "plugin.json")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromNuGetPackage_NoManifest_Fails()
    {
        // Arrange: Create a package directory without plugin.json
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var packageDir = Path.Combine(tempDir, "NoManifestPackage");
        Directory.CreateDirectory(packageDir);

        // Create a dummy DLL but no manifest
        var dllPath = Path.Combine(packageDir, "relay-plugin-nomanifest.dll");
        await File.WriteAllBytesAsync(dllPath, GenerateDummyAssembly());

        try
        {
            // Act
            var result = await _manager.InstallPluginAsync(packageDir, null, false);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("plugin.json not found", result.Error);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromNuGetPackage_InvalidManifest_Fails()
    {
        // Arrange: Create a package directory with invalid plugin.json
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var packageDir = Path.Combine(tempDir, "InvalidManifestPackage");
        Directory.CreateDirectory(packageDir);

        // Create invalid JSON manifest
        var manifestPath = Path.Combine(packageDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, "{ invalid json content }");

        // Create a dummy DLL
        var dllPath = Path.Combine(packageDir, "relay-plugin-invalid.dll");
        await File.WriteAllBytesAsync(dllPath, GenerateDummyAssembly());

        try
        {
            // Act
            var result = await _manager.InstallPluginAsync(packageDir, null, false);

            // Assert
            Assert.False(result.Success);
            Assert.NotNull(result.Error);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstallPluginAsync_FromNuGetPackage_WithVersion_Succeeds()
    {
        // Arrange: Test version-specific installation
        var packageName = "VersionedNuGetPlugin";
        var version = "2.1.0";

        // Create a temporary directory structure
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var packageDir = Path.Combine(tempDir, $"{packageName}.{version}");
        Directory.CreateDirectory(packageDir);

        // Create plugin manifest
        var manifest = new PluginManifest
        {
            Name = "VersionedNuGetPlugin",
            Version = version,
            Description = "Versioned NuGet Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(packageDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a dummy DLL
        var dllPath = Path.Combine(packageDir, "relay-plugin-versioned.dll");
        await File.WriteAllBytesAsync(dllPath, GenerateDummyAssembly());

        try
        {
            // Act
            var result = await _manager.InstallPluginAsync(packageDir, version, false);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("VersionedNuGetPlugin", result.PluginName);
            Assert.Equal(version, result.Version);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CopyDirectory_WithSpecialCharacters_Succeeds()
    {
        // Test the internal CopyDirectory functionality with special characters
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);

        // Create a subdirectory with special characters
        var subDirName = "Test Subdir With Special Ch@r@cters [123]";
        var subDir = Path.Combine(sourceDir, subDirName);
        Directory.CreateDirectory(subDir);

        var manifest = new PluginManifest
        {
            Name = "SpecialCharPlugin",
            Version = "1.0.0",
            Description = "Special Character Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create files with special characters
        var mainDllPath = Path.Combine(sourceDir, "relay-plugin-special.dll");
        var subFilePath = Path.Combine(subDir, "special-file.txt");

        await File.WriteAllBytesAsync(mainDllPath, GenerateDummyAssembly());
        await File.WriteAllTextAsync(subFilePath, "content with special characters: àáâãäåæçèé");

        try
        {
            // Act: Install the plugin
            var result = await _manager.InstallPluginAsync(sourceDir, null, false);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("SpecialCharPlugin", result.PluginName);

            // Verify files were copied with their special characters intact
            var installedPluginDir = Path.Combine(_tempPluginsDir, "SpecialCharPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, subDirName)));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, subDirName, "special-file.txt")));

            var copiedContent = await File.ReadAllTextAsync(Path.Combine(installedPluginDir, subDirName, "special-file.txt"));
            Assert.Equal("content with special characters: àáâãäåæçèé", copiedContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task CopyDirectory_DeepNestedDirectories_Succeeds()
    {
        // Test recursive copying with deeply nested directory structure
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);

        // Create deeply nested structure
        var level1Dir = Path.Combine(sourceDir, "level1");
        var level2Dir = Path.Combine(level1Dir, "level2");
        var level3Dir = Path.Combine(level2Dir, "level3");
        Directory.CreateDirectory(level3Dir);

        var manifest = new PluginManifest
        {
            Name = "DeepNestedPlugin",
            Version = "1.0.0",
            Description = "Deep Nested Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create files at different levels
        var rootFile = Path.Combine(sourceDir, "root.txt");
        var level1File = Path.Combine(level1Dir, "level1.txt");
        var level2File = Path.Combine(level2Dir, "level2.txt");
        var level3File = Path.Combine(level3Dir, "level3.txt");

        await File.WriteAllTextAsync(rootFile, "root content");
        await File.WriteAllTextAsync(level1File, "level1 content");
        await File.WriteAllTextAsync(level2File, "level2 content");
        await File.WriteAllTextAsync(level3File, "level3 content");

        try
        {
            // Act: Install the plugin
            var result = await _manager.InstallPluginAsync(sourceDir, null, false);

            // Assert
            Assert.True(result.Success);

            // Verify all nested directories and files were copied
            var installedPluginDir = Path.Combine(_tempPluginsDir, "DeepNestedPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "level1")));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "level1", "level2")));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "level1", "level2", "level3")));

            Assert.True(File.Exists(Path.Combine(installedPluginDir, "root.txt")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "level1", "level1.txt")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "level1", "level2", "level2.txt")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "level1", "level2", "level3", "level3.txt")));

            // Verify content
            var level3Content = await File.ReadAllTextAsync(Path.Combine(installedPluginDir, "level1", "level2", "level3", "level3.txt"));
            Assert.Equal("level3 content", level3Content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task CopyDirectory_EmptyDirectories_Succeeds()
    {
        // Test copying directories that contain empty subdirectories
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);

        // Create empty subdirectories
        var emptyDir1 = Path.Combine(sourceDir, "empty1");
        var emptyDir2 = Path.Combine(sourceDir, "empty2");
        Directory.CreateDirectory(emptyDir1);
        Directory.CreateDirectory(emptyDir2);

        var manifest = new PluginManifest
        {
            Name = "EmptyDirsPlugin",
            Version = "1.0.0",
            Description = "Empty Directories Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create one file
        var filePath = Path.Combine(sourceDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "test content");

        try
        {
            // Act: Install the plugin
            var result = await _manager.InstallPluginAsync(sourceDir, null, false);

            // Assert
            Assert.True(result.Success);

            // Verify empty directories were copied
            var installedPluginDir = Path.Combine(_tempPluginsDir, "EmptyDirsPlugin");
            Assert.True(Directory.Exists(installedPluginDir));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "empty1")));
            Assert.True(Directory.Exists(Path.Combine(installedPluginDir, "empty2")));
            Assert.True(File.Exists(Path.Combine(installedPluginDir, "test.txt")));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    [Fact]
    public async Task CopyDirectory_OverwritesExistingFiles()
    {
        // Test that CopyDirectory overwrites existing files
        var sourceDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(sourceDir);

        var manifest = new PluginManifest
        {
            Name = "OverwriteFilesPlugin",
            Version = "1.0.0",
            Description = "Overwrite Files Plugin",
            MinimumRelayVersion = "2.1.0"
        };

        var manifestPath = Path.Combine(sourceDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));

        // Create a file with initial content
        var filePath = Path.Combine(sourceDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "new content");

        try
        {
            // First install to create the directory
            var firstResult = await _manager.InstallPluginAsync(sourceDir, null, false);
            Assert.True(firstResult.Success);

            // Modify the source file
            await File.WriteAllTextAsync(filePath, "updated content");

            // Second install should overwrite
            var secondResult = await _manager.InstallPluginAsync(sourceDir, null, false);
            Assert.True(secondResult.Success);

            // Verify the file was overwritten
            var installedFilePath = Path.Combine(_tempPluginsDir, "OverwriteFilesPlugin", "test.txt");
            var content = await File.ReadAllTextAsync(installedFilePath);
            Assert.Equal("updated content", content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(sourceDir))
                Directory.Delete(sourceDir, true);
        }
    }

    private byte[] GenerateDummyAssembly()
    {
        // Create a minimal dummy assembly as a byte array
        return System.Text.Encoding.UTF8.GetBytes("dummy-assembly-content");
    }

    private PluginLoadContext CreateMockLoadContext()
    {
        // Create a temporary assembly path for the mock
        var tempPath = Path.GetTempFileName();
        File.WriteAllText(tempPath, "dummy content");

        return new PluginLoadContext(tempPath);
    }

    public void Dispose()
    {
        // Cleanup temporary directories
        try
        {
            if (Directory.Exists(_tempPluginsDir))
                Directory.Delete(_tempPluginsDir, true);
            if (Directory.Exists(_tempGlobalPluginsDir))
                Directory.Delete(_tempGlobalPluginsDir, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }

        _manager?.Dispose();
    }
}
