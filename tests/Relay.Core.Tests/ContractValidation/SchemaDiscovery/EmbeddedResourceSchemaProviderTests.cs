using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.SchemaDiscovery;

public class EmbeddedResourceSchemaProviderTests
{
    private readonly SchemaDiscoveryOptions _options;

    public EmbeddedResourceSchemaProviderTests()
    {
        _options = new SchemaDiscoveryOptions
        {
            EnableEmbeddedResources = true
        };
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EmbeddedResourceSchemaProvider(null!));
    }

    [Fact]
    public void Priority_Returns50()
    {
        // Arrange
        var provider = new EmbeddedResourceSchemaProvider(_options);

        // Act
        var priority = provider.Priority;

        // Assert
        Assert.Equal(50, priority);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Arrange
        var provider = new EmbeddedResourceSchemaProvider(_options);
        var context = new SchemaContext { RequestType = typeof(string), IsRequest = true };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.TryGetSchemaAsync(null!, context).AsTask());
    }

    [Fact]
    public async Task TryGetSchemaAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Arrange
        var provider = new EmbeddedResourceSchemaProvider(_options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.TryGetSchemaAsync(typeof(string), null!).AsTask());
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsNull_WhenEmbeddedResourcesDisabled()
    {
        // Arrange
        var disabledOptions = new SchemaDiscoveryOptions
        {
            EnableEmbeddedResources = false
        };
        var provider = new EmbeddedResourceSchemaProvider(disabledOptions);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsNull_WhenResourceNotFound()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var provider = new EmbeddedResourceSchemaProvider(_options, assemblies);
        var context = new SchemaContext { RequestType = typeof(NonExistentRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(NonExistentRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetSchemaAsync_UsesCustomSchemaVersion_WhenProvidedInContext()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var provider = new EmbeddedResourceSchemaProvider(_options, assemblies);
        var context = new SchemaContext
        {
            RequestType = typeof(TestRequest),
            IsRequest = true,
            SchemaVersion = "http://json-schema.org/draft-04/schema#"
        };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert - Custom schema version should be used when schema is found
        Assert.NotNull(result);
        Assert.Equal("http://json-schema.org/draft-04/schema#", result.SchemaVersion);
    }

    [Fact]
    public async Task TryGetSchemaAsync_LogsDebugMessages_WhenLoggerProvided()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<EmbeddedResourceSchemaProvider>>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var provider = new EmbeddedResourceSchemaProvider(_options, assemblies, mockLogger.Object);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Searching for embedded resource")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TryGetSchemaAsync_SearchesMultipleAssemblies()
    {
        // Arrange
        var assemblies = new[]
        {
            Assembly.GetExecutingAssembly(),
            typeof(SchemaContext).Assembly
        };
        var provider = new EmbeddedResourceSchemaProvider(_options, assemblies);
        var context = new SchemaContext { RequestType = typeof(NonExistentRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(NonExistentRequest), context);

        // Assert
        Assert.Null(result); // No embedded resources for NonExistentRequest
    }

    [Fact]
    public async Task TryGetSchemaAsync_UsesCustomNamingConvention()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions
        {
            EnableEmbeddedResources = true,
            NamingConvention = "{TypeName}.{IsRequest}.schema.json"
        };

        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var provider = new EmbeddedResourceSchemaProvider(options, assemblies);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result); // No embedded resources match this pattern
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsSchema_WhenEmbeddedResourceFound()
    {
        // Arrange
        var assemblies = new[] { Assembly.GetExecutingAssembly() };
        var provider = new EmbeddedResourceSchemaProvider(_options, assemblies);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test Request Schema", result.Schema);
        Assert.Equal("http://json-schema.org/draft-07/schema#", result.SchemaVersion);
    }

    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    private class NonExistentRequest
    {
        public string Data { get; set; } = string.Empty;
    }
}
