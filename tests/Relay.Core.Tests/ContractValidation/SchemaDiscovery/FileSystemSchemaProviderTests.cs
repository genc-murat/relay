using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.SchemaDiscovery;

public class FileSystemSchemaProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly SchemaDiscoveryOptions _options;

    public FileSystemSchemaProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        _options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = { _tempDirectory }
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FileSystemSchemaProvider(null!));
    }

    [Fact]
    public void Priority_Returns100()
    {
        // Arrange
        var provider = new FileSystemSchemaProvider(_options);

        // Act
        var priority = provider.Priority;

        // Assert
        Assert.Equal(100, priority);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Arrange
        var provider = new FileSystemSchemaProvider(_options);
        var context = new SchemaContext { RequestType = typeof(string), IsRequest = true };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.TryGetSchemaAsync(null!, context).AsTask());
    }

    [Fact]
    public async Task TryGetSchemaAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Arrange
        var provider = new FileSystemSchemaProvider(_options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            provider.TryGetSchemaAsync(typeof(string), null!).AsTask());
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsNull_WhenNoDirectoriesConfigured()
    {
        // Arrange
        var emptyOptions = new SchemaDiscoveryOptions();
        var provider = new FileSystemSchemaProvider(emptyOptions);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsNull_WhenSchemaFileNotFound()
    {
        // Arrange
        var provider = new FileSystemSchemaProvider(_options);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsSchema_WhenSchemaFileExists()
    {
        // Arrange
        var schemaContent = "{\"type\": \"object\", \"properties\": {\"name\": {\"type\": \"string\"}}}";
        var schemaFilePath = Path.Combine(_tempDirectory, "TestRequest.schema.json");
        await File.WriteAllTextAsync(schemaFilePath, schemaContent);

        var provider = new FileSystemSchemaProvider(_options);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(schemaContent, result.Schema);
        Assert.Equal("http://json-schema.org/draft-07/schema#", result.SchemaVersion);
    }

    [Fact]
    public async Task TryGetSchemaAsync_UsesCustomSchemaVersion_WhenProvidedInContext()
    {
        // Arrange
        var schemaContent = "{\"type\": \"object\"}";
        var schemaFilePath = Path.Combine(_tempDirectory, "TestRequest.schema.json");
        await File.WriteAllTextAsync(schemaFilePath, schemaContent);

        var provider = new FileSystemSchemaProvider(_options);
        var context = new SchemaContext
        {
            RequestType = typeof(TestRequest),
            IsRequest = true,
            SchemaVersion = "http://json-schema.org/draft-04/schema#"
        };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("http://json-schema.org/draft-04/schema#", result.SchemaVersion);
    }

    [Fact]
    public async Task TryGetSchemaAsync_ReturnsNull_WhenSchemaFileIsEmpty()
    {
        // Arrange
        var schemaFilePath = Path.Combine(_tempDirectory, "TestRequest.schema.json");
        await File.WriteAllTextAsync(schemaFilePath, string.Empty);

        var provider = new FileSystemSchemaProvider(_options);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryGetSchemaAsync_SearchesMultipleDirectories()
    {
        // Arrange
        var secondDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(secondDirectory);

        try
        {
            var schemaContent = "{\"type\": \"object\"}";
            var schemaFilePath = Path.Combine(secondDirectory, "TestRequest.schema.json");
            await File.WriteAllTextAsync(schemaFilePath, schemaContent);

            var options = new SchemaDiscoveryOptions
            {
                SchemaDirectories = { _tempDirectory, secondDirectory }
            };

            var provider = new FileSystemSchemaProvider(options);
            var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

            // Act
            var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(schemaContent, result.Schema);
        }
        finally
        {
            if (Directory.Exists(secondDirectory))
            {
                Directory.Delete(secondDirectory, true);
            }
        }
    }

    [Fact]
    public async Task TryGetSchemaAsync_UsesCustomNamingConvention()
    {
        // Arrange
        var schemaContent = "{\"type\": \"object\"}";
        var schemaFilePath = Path.Combine(_tempDirectory, "TestRequest.request.schema.json");
        await File.WriteAllTextAsync(schemaFilePath, schemaContent);

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = { _tempDirectory },
            NamingConvention = "{TypeName}.{IsRequest}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(schemaContent, result.Schema);
    }

    [Fact]
    public async Task TryGetSchemaAsync_LogsDebugMessages_WhenLoggerProvided()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FileSystemSchemaProvider>>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var provider = new FileSystemSchemaProvider(_options, mockLogger.Object);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        await provider.TryGetSchemaAsync(typeof(TestRequest), context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Searching for schema file")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
