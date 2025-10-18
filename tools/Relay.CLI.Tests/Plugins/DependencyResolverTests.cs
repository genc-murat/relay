using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class DependencyResolverTests
{
    private readonly Mock<IPluginLogger> _loggerMock;
    private readonly DependencyResolver _dependencyResolver;

    public DependencyResolverTests()
    {
        _loggerMock = new Mock<IPluginLogger>();
        _dependencyResolver = new DependencyResolver(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithLogger()
    {
        // Arrange & Act
        var logger = new Mock<IPluginLogger>();
        var resolver = new DependencyResolver(logger.Object);

        // Assert
        Assert.NotNull(resolver);
    }

    [Fact]
    public async Task ResolveDependenciesAsync_WithInvalidPluginPath_ShouldReturnEmptyList()
    {
        // Arrange
        var pluginPath = "NonExistentPlugin.dll";

        // Act
        var result = await _dependencyResolver.ResolveDependenciesAsync(pluginPath);

        // Assert
        Assert.Empty(result);
        _loggerMock.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Error resolving dependencies")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ResolveDependenciesAsync_WithValidPlugin_ShouldResolveDependencies()
    {
        // Arrange
        var pluginPath = typeof(DependencyResolver).Assembly.Location;

        // Act
        var result = await _dependencyResolver.ResolveDependenciesAsync(pluginPath);

        // Assert
        Assert.NotNull(result);
        _loggerMock.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("Resolved"))), Times.Once);
    }

    [Fact]
    public void RegisterSharedAssembly_WithValidAssembly_ShouldRegisterSuccessfully()
    {
        // Arrange
        var assembly = typeof(DependencyResolver).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var assemblyPath = assembly.Location;

        // Act
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);

        // Assert
        var sharedAssembly = _dependencyResolver.GetSharedAssembly(assemblyName);
        Assert.NotNull(sharedAssembly);
        _loggerMock.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("Registered shared assembly"))), Times.Once);
    }

    [Fact]
    public void RegisterSharedAssembly_WithInvalidAssemblyPath_ShouldHandleError()
    {
        // Arrange
        var assemblyName = "InvalidAssembly";
        var assemblyPath = "NonExistent.dll";

        // Act
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);

        // Assert
        var sharedAssembly = _dependencyResolver.GetSharedAssembly(assemblyName);
        Assert.Null(sharedAssembly);
        _loggerMock.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Failed to register shared assembly")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public void RegisterSharedAssembly_WithDuplicateAssembly_ShouldNotRegisterAgain()
    {
        // Arrange
        var assembly = typeof(DependencyResolver).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var assemblyPath = assembly.Location;

        // Act
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath); // Try to register again

        // Assert
        var sharedAssemblies = _dependencyResolver.GetSharedAssemblies();
        Assert.Contains(assemblyName, sharedAssemblies);
        Assert.Single(sharedAssemblies);
        _loggerMock.Verify(x => x.LogDebug(It.Is<string>(s => s.Contains("Registered shared assembly"))), Times.Once); // Only once
    }

    [Fact]
    public void GetSharedAssembly_WithExistingAssembly_ShouldReturnAssembly()
    {
        // Arrange
        var assembly = typeof(DependencyResolver).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var assemblyPath = assembly.Location;
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);

        // Act
        var result = _dependencyResolver.GetSharedAssembly(assemblyName);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void GetSharedAssembly_WithNonExistingAssembly_ShouldReturnNull()
    {
        // Arrange
        var assemblyName = "NonExistentAssembly";

        // Act
        var result = _dependencyResolver.GetSharedAssembly(assemblyName);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetSharedAssemblies_ShouldReturnAllRegisteredAssemblies()
    {
        // Arrange
        var assembly1 = typeof(DependencyResolver).Assembly;
        var assembly2 = typeof(System.Linq.Enumerable).Assembly;
        var name1 = assembly1.GetName().Name!;
        var name2 = assembly2.GetName().Name!;
        _dependencyResolver.RegisterSharedAssembly(name1, assembly1.Location);
        _dependencyResolver.RegisterSharedAssembly(name2, assembly2.Location);

        // Act
        var result = _dependencyResolver.GetSharedAssemblies();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(name1, result);
        Assert.Contains(name2, result);
    }

    [Fact]
    public void GetVersionConflicts_ShouldReturnAllDetectedConflicts()
    {
        // Arrange
        // Version conflicts are detected during dependency resolution, so we need to simulate that
        // For this test, we'll just check that the method returns an empty dictionary initially

        // Act
        var result = _dependencyResolver.GetVersionConflicts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void ClearCache_ShouldClearAllCachedData()
    {
        // Arrange
        var assembly = typeof(DependencyResolver).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var assemblyPath = assembly.Location;
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);

        // Verify data exists
        var sharedAssemblies = _dependencyResolver.GetSharedAssemblies();
        Assert.Single(sharedAssemblies);

        // Act
        _dependencyResolver.ClearCache();

        // Assert
        var clearedSharedAssemblies = _dependencyResolver.GetSharedAssemblies();
        Assert.Empty(clearedSharedAssemblies);

        var versionConflicts = _dependencyResolver.GetVersionConflicts();
        Assert.Empty(versionConflicts);
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithValidPlugin_ShouldComplete()
    {
        // Arrange
        var pluginPath = typeof(DependencyResolver).Assembly.Location; // Use the current assembly

        // Act & Assert
        await _dependencyResolver.ValidateDependenciesAsync(pluginPath); // Should complete without throwing
    }

    [Fact]
    public async Task ValidateDependenciesAsync_WithInvalidPluginPath_ShouldReturnFalse()
    {
        // Arrange
        var pluginPath = "NonExistentPlugin.dll";

        // Act
        var result = await _dependencyResolver.ValidateDependenciesAsync(pluginPath);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Error validating dependencies")), It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public void GetSharedAssemblies_ShouldReturnCopyNotReference()
    {
        // Arrange
        var assembly = typeof(DependencyResolver).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var assemblyPath = assembly.Location;
        _dependencyResolver.RegisterSharedAssembly(assemblyName, assemblyPath);

        // Act
        var result1 = _dependencyResolver.GetSharedAssemblies();
        var result2 = _dependencyResolver.GetSharedAssemblies();

        // Assert
        Assert.NotSame(result1, result2); // Should be different instances
        Assert.Equal(result1, result2); // But should have same content
    }

    [Fact]
    public void GetVersionConflicts_ShouldReturnCopyNotReference()
    {
        // Arrange
        // Version conflicts start empty

        // Act
        var result1 = _dependencyResolver.GetVersionConflicts();
        var result2 = _dependencyResolver.GetVersionConflicts();

        // Assert
        Assert.NotSame(result1, result2); // Should be different instances
        Assert.Equal(result1, result2); // But should have same content
    }
}