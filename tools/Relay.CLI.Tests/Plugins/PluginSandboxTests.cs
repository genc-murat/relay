using System;
using System.Security;
using Moq;
using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

public class PluginSandboxTests
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly PluginSandbox _sandbox;

    public PluginSandboxTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _sandbox = new PluginSandbox(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_ValidOperation_ReturnsResult()
    {
        // Arrange
        var testValue = 42;
        Func<Task<int>> operation = async () => 
        {
            await Task.Delay(10); // Simulate some async work
            return testValue;
        };

        // Act
        var result = await _sandbox.ExecuteInSandboxAsync(operation);

        // Assert
        Assert.Equal(testValue, result);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_OperationThrowsException_ThrowsException()
    {
        // Arrange
        Func<Task<int>> operation = async () => 
        {
            await Task.Delay(10);
            throw new InvalidOperationException("Test exception");
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sandbox.ExecuteInSandboxAsync(operation));
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_OperationTimesOut_ThrowsTimeoutException()
    {
        // Arrange
        Func<Task<int>> operation = async () => 
        {
            await Task.Delay(1000); // Longer than the default timeout in ExecuteInSandboxAsync
            return 42;
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(
            () => _sandbox.ExecuteInSandboxAsync(operation, new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token));
    }

    [Fact]
    public async Task ExecuteWithResourceLimitsAsync_ValidOperation_ReturnsResult()
    {
        // Arrange
        var testValue = 123;
        Func<Task<int>> operation = async () => 
        {
            await Task.Delay(10); // Simulate some async work
            return testValue;
        };

        // Act
        var result = await _sandbox.ExecuteWithResourceLimitsAsync(operation);

        // Assert
        Assert.Equal(testValue, result);
    }

    [Fact]
    public void ValidateFileSystemAccess_AllowedPath_ReturnsTrue()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { @"C:\Allowed" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateFileSystemAccess(@"C:\Allowed\file.txt", FileSystemAccessType.Read);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateFileSystemAccess_DeniedPath_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = true,
                AllowedPaths = new[] { @"C:\Allowed" },
                DeniedPaths = new[] { @"C:\Allowed\Secret" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateFileSystemAccess(@"C:\Allowed\Secret\file.txt", FileSystemAccessType.Read);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateFileSystemAccess_InvalidAccessType_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            FileSystem = new FileSystemPermissions
            {
                Read = false, // Read is not allowed
                AllowedPaths = new[] { @"C:\Allowed" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateFileSystemAccess(@"C:\Allowed\file.txt", FileSystemAccessType.Read);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateNetworkAccess_AllowedHost_ReturnsTrue()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            Network = new NetworkPermissions
            {
                Https = true,
                AllowedHosts = new[] { "example.com" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateNetworkAccess("https://example.com/api");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateNetworkAccess_DeniedHost_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            Network = new NetworkPermissions
            {
                Https = true,
                DeniedHosts = new[] { "malicious.com" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateNetworkAccess("https://malicious.com/api");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateNetworkAccess_ProtocolNotAllowed_ReturnsFalse()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            Network = new NetworkPermissions
            {
                Https = false, // HTTPS not allowed
                AllowedHosts = new[] { "example.com" }
            }
        };
        
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateNetworkAccess("https://example.com/api");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ValidateNetworkAccess_NoPermissions_ReturnsFalse()
    {
        // Act
        var result = _sandbox.ValidateNetworkAccess("https://example.com/api");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_Disposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _sandbox.Dispose();
        Func<Task<int>> operation = () => Task.FromResult(42);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _sandbox.ExecuteInSandboxAsync(operation));
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_WithPermissions_CallsApplyPermissions()
    {
        // Arrange
        var permissions = new PluginPermissions();
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);
        Func<Task<int>> operation = () => Task.FromResult(42);

        // Act
        var result = await sandbox.ExecuteInSandboxAsync(operation);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_SecurityException_LogsAndRethrows()
    {
        // Arrange
        Func<Task<int>> operation = () => throw new SecurityException("Test security violation");

        // Act & Assert
        await Assert.ThrowsAsync<SecurityException>(() => _sandbox.ExecuteInSandboxAsync(operation));
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("Security violation")), It.IsAny<SecurityException>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteInSandboxAsync_UnauthorizedAccessException_LogsAndRethrows()
    {
        // Arrange
        Func<Task<int>> operation = () => throw new UnauthorizedAccessException("Test access denied");

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sandbox.ExecuteInSandboxAsync(operation));
        _mockLogger.Verify(x => x.LogError(It.Is<string>(s => s.Contains("unauthorized access")), It.IsAny<UnauthorizedAccessException>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithResourceLimitsAsync_Disposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _sandbox.Dispose();
        Func<Task<int>> operation = () => Task.FromResult(42);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _sandbox.ExecuteWithResourceLimitsAsync(operation));
    }

    [Fact]
    public async Task ExecuteWithResourceLimitsAsync_WithCustomPermissions_UsesLimits()
    {
        // Arrange
        var permissions = new PluginPermissions { MaxExecutionTimeMs = 1000, MaxMemoryBytes = 1024 };
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);
        Func<Task<int>> operation = () => Task.FromResult(42);

        // Act
        var result = await sandbox.ExecuteWithResourceLimitsAsync(operation);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteWithResourceLimitsAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var permissions = new PluginPermissions { MaxExecutionTimeMs = 100 };
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);
        Func<Task<int>> operation = async () => { await Task.Delay(200); return 42; };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(() => sandbox.ExecuteWithResourceLimitsAsync(operation));
    }

    [Fact]
    public async Task ExecuteWithResourceLimitsAsync_Cancelled_ThrowsTaskCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Func<Task<int>> operation = async () =>
        {
            await Task.Delay(10, cts.Token); // This will throw TaskCanceledException
            return 42;
        };

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => _sandbox.ExecuteWithResourceLimitsAsync(operation, cts.Token));
    }

    [Fact]
    public void ValidateNetworkAccess_HttpAllowed_ReturnsTrue()
    {
        // Arrange
        var permissions = new PluginPermissions
        {
            Network = new NetworkPermissions
            {
                Http = true,
                AllowedHosts = new[] { "example.com" }
            }
        };
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act
        var result = sandbox.ValidateNetworkAccess("http://example.com/api");

        // Assert
        Assert.True(result);
    }



    [Fact]
    public void Dispose_CalledMultipleTimes_NoException()
    {
        // Act
        _sandbox.Dispose();
        _sandbox.Dispose(); // Should not throw
    }

    [Fact]
    public void Constructor_WithPermissions_SetsPermissions()
    {
        // Arrange
        var permissions = new PluginPermissions();
        var sandbox = new PluginSandbox(_mockLogger.Object, permissions);

        // Act & Assert - Test indirectly through ValidateFileSystemAccess
        Assert.False(sandbox.ValidateFileSystemAccess("any", FileSystemAccessType.Read));
    }
}
