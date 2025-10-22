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
