using Moq;
using Relay.CLI.Plugins;

namespace Relay.CLI.Tests.Plugins;

#pragma warning disable CS8625
public class PluginContextTests
{
    private readonly Mock<IPluginLogger> _mockLogger;
    private readonly Mock<IFileSystem> _mockFileSystem;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IServiceProvider> _mockServices;

    public PluginContextTests()
    {
        _mockLogger = new Mock<IPluginLogger>();
        _mockFileSystem = new Mock<IFileSystem>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockServices = new Mock<IServiceProvider>();
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        const string cliVersion = "1.0.0";
        const string workingDirectory = "/test/dir";

        // Act
        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            cliVersion,
            workingDirectory);

        // Assert
        Assert.Equal(_mockLogger.Object, context.Logger);
        Assert.Equal(_mockFileSystem.Object, context.FileSystem);
        Assert.Equal(_mockConfiguration.Object, context.Configuration);
        Assert.Equal(_mockServices.Object, context.Services);
        Assert.Equal(cliVersion, context.CliVersion);
        Assert.Equal(workingDirectory, context.WorkingDirectory);
    }

    [Fact]
    public void Constructor_CreatesPluginFileSystem_WhenFileSystemIsNull()
    {
        // Arrange
        const string cliVersion = "1.0.0";
        const string workingDirectory = "/test/dir";
        var sandbox = new PluginSandbox(_mockLogger.Object);

        // Act
        var context = new PluginContext(
            _mockLogger.Object,
            null,
            _mockConfiguration.Object,
            _mockServices.Object,
            cliVersion,
            workingDirectory,
            sandbox);

        // Assert
        Assert.IsType<PluginFileSystem>(context.FileSystem);
    }

    [Fact]
    public async Task GetServiceAsync_ReturnsService_WhenAvailable()
    {
        // Arrange
        var expectedService = new TestService();
        _mockServices.Setup(s => s.GetService(typeof(TestService))).Returns(expectedService);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        var result = await context.GetServiceAsync<TestService>();

        // Assert
        Assert.Equal(expectedService, result);
    }

    [Fact]
    public async Task GetServiceAsync_ReturnsNull_WhenServiceNotAvailable()
    {
        // Arrange
        _mockServices.Setup(s => s.GetService(typeof(TestService))).Returns(null);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        var result = await context.GetServiceAsync<TestService>();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRequiredServiceAsync_ReturnsService_WhenAvailable()
    {
        // Arrange
        var expectedService = new TestService();
        _mockServices.Setup(s => s.GetService(typeof(TestService))).Returns(expectedService);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        var result = await context.GetRequiredServiceAsync<TestService>();

        // Assert
        Assert.Equal(expectedService, result);
    }

    [Fact]
    public async Task GetRequiredServiceAsync_Throws_WhenServiceNotAvailable()
    {
        // Arrange
        _mockServices.Setup(s => s.GetService(typeof(TestService))).Returns(null);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.GetRequiredServiceAsync<TestService>());
        Assert.Contains("TestService not found", exception.Message);
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsValueFromConfiguration()
    {
        // Arrange
        const string key = "testKey";
        const string expectedValue = "testValue";
        _mockConfiguration.Setup(c => c[key]).Returns(expectedValue);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        var result = await context.GetSettingAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    [Fact]
    public async Task GetSettingAsync_ReturnsNull_WhenKeyNotFound()
    {
        // Arrange
        const string key = "nonExistentKey";
        _mockConfiguration.Setup(c => c[key]).Returns((string?)null);

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        var result = await context.GetSettingAsync(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetSettingAsync_SetsValueInConfiguration()
    {
        // Arrange
        const string key = "testKey";
        const string value = "testValue";

        var context = new PluginContext(
            _mockLogger.Object,
            _mockFileSystem.Object,
            _mockConfiguration.Object,
            _mockServices.Object,
            "1.0.0",
            "/test");

        // Act
        await context.SetSettingAsync(key, value);

        // Assert
        _mockConfiguration.VerifySet(c => c[key] = value, Times.Once);
    }

    private class TestService
    {
        // Test service class
    }
}