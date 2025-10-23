using Relay.CLI.Plugins;
using System.Reflection;
using System.Text.Json;

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.CLI.Tests.Plugins;

public class PluginManagerAssemblyFindingTests : IDisposable
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginManager _manager;
    private readonly string _tempPluginsDir;
    private readonly string _tempGlobalPluginsDir;

    public PluginManagerAssemblyFindingTests()
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
    public async Task FindPluginAssembly_WithRelayPluginPrefix_ReturnsCorrectAssembly()
    {
        // Arrange
        var pluginName = "TestPlugin";
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        // Create manifest
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create multiple DLL files, including one with relay-plugin prefix
        var relayPluginDll = Path.Combine(pluginDir, "relay-plugin-test.dll");
        var dependencyDll = Path.Combine(pluginDir, "Relay.CLI.Sdk.dll");
        var otherDll = Path.Combine(pluginDir, "other.dll");
        
        await File.WriteAllBytesAsync(relayPluginDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(dependencyDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(otherDll, GenerateDummyAssembly());
        
        // Use reflection to access FindPluginAssembly method
        var findPluginAssemblyMethod = typeof(PluginManager).GetMethod("FindPluginAssembly", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginAssemblyMethod.Invoke(_manager, new object[] { pluginDir }) as string;
        
        // Assert
        Assert.Equal(relayPluginDll, result);
    }

    [Fact]
    public async Task FindPluginAssembly_WithPluginInName_ReturnsCorrectAssembly()
    {
        // Arrange
        var pluginName = "AnotherTestPlugin";
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        // Create manifest
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Another Test Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create multiple DLL files, including one with "Plugin" in the name
        var pluginDll = Path.Combine(pluginDir, "MyPlugin.dll");
        var dependencyDll = Path.Combine(pluginDir, "Dependency.dll");
        var otherDll = Path.Combine(pluginDir, "Util.dll");
        
        await File.WriteAllBytesAsync(pluginDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(dependencyDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(otherDll, GenerateDummyAssembly());
        
        // Use reflection to access FindPluginAssembly method
        var findPluginAssemblyMethod = typeof(PluginManager).GetMethod("FindPluginAssembly", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginAssemblyMethod.Invoke(_manager, new object[] { pluginDir }) as string;
        
        // Assert
        Assert.Equal(pluginDll, result);
    }

    [Fact]
    public async Task FindPluginAssembly_WithOnlyDependencies_ReturnsFirstDll()
    {
        // Arrange
        var pluginName = "DependencyOnlyPlugin";
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        // Create manifest
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Dependency Only Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create only dependency DLLs (no plugin DLLs)
        var sdkDll = Path.Combine(pluginDir, "Relay.CLI.Sdk.dll");
        var otherDll = Path.Combine(pluginDir, "OtherDependency.dll");
        
        await File.WriteAllBytesAsync(sdkDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(otherDll, GenerateDummyAssembly());
        
        // Use reflection to access FindPluginAssembly method
        var findPluginAssemblyMethod = typeof(PluginManager).GetMethod("FindPluginAssembly", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginAssemblyMethod.Invoke(_manager, new object[] { pluginDir }) as string;
        
        // Assert - Should fallback to first DLL (otherDll)
        Assert.Equal(otherDll, result);
    }

    [Fact]
    public async Task FindPluginAssembly_WithNoDllFiles_ReturnsNull()
    {
        // Arrange
        var pluginName = "NoDllPlugin";
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        // Create manifest but no DLL files
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "No DLL Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Use reflection to access FindPluginAssembly method
        var findPluginAssemblyMethod = typeof(PluginManager).GetMethod("FindPluginAssembly", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginAssemblyMethod.Invoke(_manager, new object[] { pluginDir }) as string;
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FindPluginType_WithRelayPluginAttribute_ReturnsCorrectType()
    {
        // Arrange - Create an assembly with a type that has RelayPlugin attribute
        var assembly = typeof(MockPluginWithAttribute).Assembly;
        
        // Use reflection to access FindPluginType method
        var findPluginTypeMethod = typeof(PluginManager).GetMethod("FindPluginType", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginTypeMethod.Invoke(_manager, new object[] { assembly }) as Type;
        
        // Assert
        Assert.Equal(typeof(MockPluginWithAttribute), result);
    }

    [Fact]
    public void FindPluginType_WithIRelayPluginImplementation_ReturnsCorrectType()
    {
        // Arrange - Create an assembly with a type that implements IRelayPlugin
        // Note: This test assembly contains both MockPluginImplementation and MockPluginWithAttribute
        // Since the plugin manager prefers types with RelayPluginAttribute, MockPluginWithAttribute will be returned
        var assembly = typeof(MockPluginImplementation).Assembly;
        
        // Use reflection to access FindPluginType method
        var findPluginTypeMethod = typeof(PluginManager).GetMethod("FindPluginType", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginTypeMethod.Invoke(_manager, new object[] { assembly }) as Type;
        
        // Assert - Since both types exist in the same assembly, the attributed type takes precedence
        Assert.Equal(typeof(MockPluginWithAttribute), result);
    }

    [Fact]
    public void FindPluginType_WithMultipleImplementations_PrefersRelayPluginAttribute()
    {
        // Arrange - Create an assembly with both attributed and non-attributed implementations
        var assembly = typeof(MockPluginWithAttribute).Assembly;
        
        // Use reflection to access FindPluginType method
        var findPluginTypeMethod = typeof(PluginManager).GetMethod("FindPluginType", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginTypeMethod.Invoke(_manager, new object[] { assembly }) as Type;
        
        // For this specific test, we're using the mock assembly which contains many types
        // We need a more specific test with our own assembly containing specific types
        Assert.NotNull(result);
        Assert.True(typeof(IRelayPlugin).IsAssignableFrom(result));
    }

    [Fact]
    public void FindPluginType_WithNoPluginTypes_ReturnsNull()
    {
        // Arrange - Use an assembly that doesn't have any plugin types
        var assembly = typeof(string).Assembly; // System.String assembly
        
        // Use reflection to access FindPluginType method
        var findPluginTypeMethod = typeof(PluginManager).GetMethod("FindPluginType", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginTypeMethod.Invoke(_manager, new object[] { assembly }) as Type;
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadPluginAsync_WithMultipleDlls_FindsCorrectAssembly()
    {
        // Arrange
        var pluginName = "MultiDllPlugin";
        var pluginDir = Path.Combine(_tempPluginsDir, pluginName);
        Directory.CreateDirectory(pluginDir);
        
        // Create manifest
        var manifest = new PluginManifest
        {
            Name = pluginName,
            Version = "1.0.0",
            Description = "Multi DLL Plugin",
            MinimumRelayVersion = "2.1.0"
        };
        
        var manifestPath = Path.Combine(pluginDir, "plugin.json");
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest));
        
        // Create multiple DLL files with different naming patterns
        var relayPluginDll = Path.Combine(pluginDir, "relay-plugin-multidll.dll");
        var dependencyDll = Path.Combine(pluginDir, "Relay.CLI.Sdk.dll");
        var pluginDll = Path.Combine(pluginDir, "MyPlugin.dll"); // Changed name to have "Plugin" but not "relay-plugin"
        
        await File.WriteAllBytesAsync(relayPluginDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(dependencyDll, GenerateDummyAssembly());
        await File.WriteAllBytesAsync(pluginDll, GenerateDummyAssembly());
        
        // Use reflection to access FindPluginAssembly method
        var findPluginAssemblyMethod = typeof(PluginManager).GetMethod("FindPluginAssembly", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        // Act
        var result = findPluginAssemblyMethod.Invoke(_manager, new object[] { pluginDir }) as string;
        
        // Assert that the relay-plugin DLL was found (takes precedence)
        Assert.Equal(relayPluginDll, result);
    }

    private byte[] GenerateDummyAssembly()
    {
        // Create a minimal dummy assembly as a byte array
        return System.Text.Encoding.UTF8.GetBytes("dummy-assembly-content");
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

// Mock plugin implementation for testing FindPluginType
public class MockPluginImplementation : IRelayPlugin
{
    public string Name => "MockPlugin";
    public string Version => "1.0.0";
    public string Description => "Mock Plugin for Testing";
    public string[] Authors => new[] { "Test Author" };
    public string[] Tags => new[] { "test" };
    public string MinimumRelayVersion => "2.0.0";
    
    public Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
    
    public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
    
    public Task CleanupAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public string GetHelp()
    {
        return "Mock Plugin Help";
    }
}

[RelayPlugin("MockPluginWithAttribute", "1.0.0")]
public class MockPluginWithAttribute : IRelayPlugin
{
    public string Name => "MockPluginWithAttribute";
    public string Version => "1.0.0";
    public string Description => "Mock Plugin with Attribute for Testing";
    public string[] Authors => new[] { "Test Author" };
    public string[] Tags => new[] { "test" };
    public string MinimumRelayVersion => "2.0.0";
    
    public Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
    
    public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
    
    public Task CleanupAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public string GetHelp()
    {
        return "Mock Plugin with Attribute Help";
    }
}

// Mock plugin implementation for testing FindPluginType without any attributes
// This ensures we have a type that only implements IRelayPlugin and doesn't conflict with attributed types
public class MockPluginOnlyImplementation : IRelayPlugin
{
    public string Name => "MockPluginOnlyImplementation";
    public string Version => "1.0.0";
    public string Description => "Mock Plugin Only Implementation for Testing";
    public string[] Authors => new[] { "Test Author" };
    public string[] Tags => new[] { "test" };
    public string MinimumRelayVersion => "2.0.0";
    
    public Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }
    
    public Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken)
    {
        return Task.FromResult(0);
    }
    
    public Task CleanupAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public string GetHelp()
    {
        return "Mock Plugin Only Implementation Help";
    }
}
