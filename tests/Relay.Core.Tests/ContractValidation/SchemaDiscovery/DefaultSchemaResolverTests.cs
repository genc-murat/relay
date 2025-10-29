using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.SchemaDiscovery;

public class DefaultSchemaResolverTests
{
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenProvidersIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultSchemaResolver(null!));
    }

    [Fact]
    public async Task ResolveSchemaAsync_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(string), IsRequest = true };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resolver.ResolveSchemaAsync(null!, context).AsTask());
    }

    [Fact]
    public async Task ResolveSchemaAsync_ThrowsArgumentNullException_WhenContextIsNull()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            resolver.ResolveSchemaAsync(typeof(string), null!).AsTask());
    }

    [Fact]
    public async Task ResolveSchemaAsync_ReturnsNull_WhenNoProvidersConfigured()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveSchemaAsync_ReturnsNull_WhenNoProviderFindsSchema()
    {
        // Arrange
        var mockProvider = new Mock<ISchemaProvider>();
        mockProvider.Setup(p => p.Priority).Returns(100);
        mockProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSchemaContract?)null);

        var providers = new List<ISchemaProvider> { mockProvider.Object };
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.Null(result);
        mockProvider.Verify(p => p.TryGetSchemaAsync(typeof(TestRequest), context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResolveSchemaAsync_ReturnsSchema_WhenProviderFindsSchema()
    {
        // Arrange
        var expectedSchema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            SchemaVersion = "http://json-schema.org/draft-07/schema#"
        };

        var mockProvider = new Mock<ISchemaProvider>();
        mockProvider.Setup(p => p.Priority).Returns(100);
        mockProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSchema);

        var providers = new List<ISchemaProvider> { mockProvider.Object };
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSchema.Schema, result.Schema);
        Assert.Equal(expectedSchema.SchemaVersion, result.SchemaVersion);
    }

    [Fact]
    public async Task ResolveSchemaAsync_UsesProviderPriority_HigherPriorityFirst()
    {
        // Arrange
        var lowPrioritySchema = new JsonSchemaContract { Schema = "{\"type\": \"string\"}" };
        var highPrioritySchema = new JsonSchemaContract { Schema = "{\"type\": \"object\"}" };

        var lowPriorityProvider = new Mock<ISchemaProvider>();
        lowPriorityProvider.Setup(p => p.Priority).Returns(50);
        lowPriorityProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lowPrioritySchema);

        var highPriorityProvider = new Mock<ISchemaProvider>();
        highPriorityProvider.Setup(p => p.Priority).Returns(100);
        highPriorityProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(highPrioritySchema);

        var providers = new List<ISchemaProvider> { lowPriorityProvider.Object, highPriorityProvider.Object };
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(highPrioritySchema.Schema, result.Schema);
        highPriorityProvider.Verify(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()), Times.Once);
        lowPriorityProvider.Verify(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResolveSchemaAsync_TriesNextProvider_WhenFirstProviderFails()
    {
        // Arrange
        var expectedSchema = new JsonSchemaContract { Schema = "{\"type\": \"object\"}" };

        var failingProvider = new Mock<ISchemaProvider>();
        failingProvider.Setup(p => p.Priority).Returns(100);
        failingProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Provider failed"));

        var successProvider = new Mock<ISchemaProvider>();
        successProvider.Setup(p => p.Priority).Returns(50);
        successProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSchema);

        var providers = new List<ISchemaProvider> { failingProvider.Object, successProvider.Object };
        var resolver = new DefaultSchemaResolver(providers);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        var result = await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSchema.Schema, result.Schema);
    }

    [Fact]
    public void InvalidateSchema_ThrowsArgumentNullException_WhenTypeIsNull()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resolver.InvalidateSchema(null!));
    }

    [Fact]
    public void InvalidateSchema_DoesNotThrow_WhenCacheIsNull()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);

        // Act & Assert - Should not throw
        resolver.InvalidateSchema(typeof(TestRequest));
    }

    [Fact]
    public void InvalidateSchema_RemovesSchemaFromCache_WhenCacheProvided()
    {
        // Arrange
        var mockCache = new Mock<ISchemaCache>();
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers, mockCache.Object);

        // Act
        resolver.InvalidateSchema(typeof(TestRequest));

        // Assert
        mockCache.Verify(c => c.Remove(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void InvalidateAll_DoesNotThrow_WhenCacheIsNull()
    {
        // Arrange
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers);

        // Act & Assert - Should not throw
        resolver.InvalidateAll();
    }

    [Fact]
    public void InvalidateAll_ClearsCache_WhenCacheProvided()
    {
        // Arrange
        var mockCache = new Mock<ISchemaCache>();
        var providers = new List<ISchemaProvider>();
        var resolver = new DefaultSchemaResolver(providers, mockCache.Object);

        // Act
        resolver.InvalidateAll();

        // Assert
        mockCache.Verify(c => c.Clear(), Times.Once);
    }

    [Fact]
    public async Task ResolveSchemaAsync_LogsInformation_WhenSchemaResolved()
    {
        // Arrange
        var expectedSchema = new JsonSchemaContract { Schema = "{\"type\": \"object\"}" };
        var mockProvider = new Mock<ISchemaProvider>();
        mockProvider.Setup(p => p.Priority).Returns(100);
        mockProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSchema);

        var mockLogger = new Mock<ILogger<DefaultSchemaResolver>>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var providers = new List<ISchemaProvider> { mockProvider.Object };
        var resolver = new DefaultSchemaResolver(providers, null, mockLogger.Object);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema resolved")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveSchemaAsync_LogsWarning_WhenNoSchemaFound()
    {
        // Arrange
        var mockProvider = new Mock<ISchemaProvider>();
        mockProvider.Setup(p => p.Priority).Returns(100);
        mockProvider.Setup(p => p.TryGetSchemaAsync(It.IsAny<Type>(), It.IsAny<SchemaContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSchemaContract?)null);

        var mockLogger = new Mock<ILogger<DefaultSchemaResolver>>();
        mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var providers = new List<ISchemaProvider> { mockProvider.Object };
        var resolver = new DefaultSchemaResolver(providers, null, mockLogger.Object);
        var context = new SchemaContext { RequestType = typeof(TestRequest), IsRequest = true };

        // Act
        await resolver.ResolveSchemaAsync(typeof(TestRequest), context);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No schema found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private class TestRequest
    {
        public string Name { get; set; } = string.Empty;
    }
}
