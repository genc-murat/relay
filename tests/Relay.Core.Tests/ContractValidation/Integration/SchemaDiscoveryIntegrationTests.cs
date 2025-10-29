using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Testing;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Integration;

/// <summary>
/// Integration tests for schema discovery from file system.
/// Tests the complete schema discovery pipeline with real file system operations.
/// </summary>
public sealed class SchemaDiscoveryIntegrationTests : IDisposable
{
    private readonly string _testSchemaDirectory;
    private readonly string _tempSchemaDirectory;
    private readonly ContractValidationTestFixture _fixture;

    public SchemaDiscoveryIntegrationTests()
    {
        _fixture = new ContractValidationTestFixture();
        
        // Use the existing TestSchemas directory
        var currentDirectory = Directory.GetCurrentDirectory();
        _testSchemaDirectory = Path.Combine(currentDirectory, "..", "..", "..", "ContractValidation", "TestSchemas");
        _testSchemaDirectory = Path.GetFullPath(_testSchemaDirectory);

        // Create a temporary directory for dynamic schema tests
        _tempSchemaDirectory = Path.Combine(Path.GetTempPath(), $"RelaySchemaTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempSchemaDirectory);
    }

    public void Dispose()
    {
        // Cleanup temporary directory
        if (Directory.Exists(_tempSchemaDirectory))
        {
            try
            {
                Directory.Delete(_tempSchemaDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task SchemaDiscovery_FromFileSystem_ShouldLoadExistingSchema()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.NotNull(schema.Schema);
        Assert.Contains("Simple Request Schema", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithMultipleDirectories_ShouldSearchAllDirectories()
    {
        // Arrange
        var schema1Path = Path.Combine(_tempSchemaDirectory, "TestType1.schema.json");
        await File.WriteAllTextAsync(schema1Path, @"{""type"": ""object"", ""title"": ""Test Type 1""}");

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory, _tempSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(TestType1), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(TestType1), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Test Type 1", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithCustomNamingConvention_ShouldFindSchema()
    {
        // Arrange
        var schemaPath = Path.Combine(_tempSchemaDirectory, "CustomRequest.request.json");
        await File.WriteAllTextAsync(schemaPath, @"{""type"": ""object"", ""title"": ""Custom Request""}");

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _tempSchemaDirectory },
            NamingConvention = "{TypeName}.{IsRequest}.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(CustomRequest), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(CustomRequest), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Custom Request", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithSchemaResolver_ShouldCacheResults()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act - First call (cache miss)
        var schema1 = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        var metrics1 = schemaCache.GetMetrics();

        // Act - Second call (cache hit)
        var schema2 = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        var metrics2 = schemaCache.GetMetrics();

        // Assert
        Assert.NotNull(schema1);
        Assert.NotNull(schema2);
        Assert.Equal(schema1.Schema, schema2.Schema);
        
        // Verify caching worked
        Assert.Equal(1, metrics1.CacheMisses);
        Assert.Equal(0, metrics1.CacheHits);
        Assert.Equal(1, metrics2.CacheHits);
        Assert.True(metrics2.HitRate > 0);
    }

    [Fact]
    public async Task SchemaDiscovery_WithMultipleProviders_ShouldUsePriority()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        
        // Create two providers with different priorities
        var options1 = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };
        var provider1 = new FileSystemSchemaProvider(options1); // Priority 100

        var options2 = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _tempSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };
        var provider2 = new FileSystemSchemaProvider(options2); // Priority 100

        // Create schema in temp directory
        var schemaPath = Path.Combine(_tempSchemaDirectory, "SimpleRequest.schema.json");
        await File.WriteAllTextAsync(schemaPath, @"{""type"": ""object"", ""title"": ""Temp Schema""}");

        var resolver = new DefaultSchemaResolver(new ISchemaProvider[] { provider1, provider2 }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        // Should find schema from first provider (test directory) since both have same priority
        Assert.Contains("Simple Request Schema", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithInvalidSchemaFile_ShouldReturnNull()
    {
        // Arrange
        var schemaPath = Path.Combine(_tempSchemaDirectory, "InvalidSchema.schema.json");
        await File.WriteAllTextAsync(schemaPath, ""); // Empty file

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _tempSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(InvalidSchema), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(InvalidSchema), context, CancellationToken.None);

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithNonExistentDirectory_ShouldReturnNull()
    {
        // Arrange
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { Path.Combine(Path.GetTempPath(), "NonExistentDirectory") },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Assert
        Assert.Null(schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithEmbeddedResourceProvider_ShouldLoadFromAssembly()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var options = new SchemaDiscoveryOptions
        {
            EnableEmbeddedResources = true
        };

        var provider = new EmbeddedResourceSchemaProvider(options, new[] { typeof(SchemaDiscoveryIntegrationTests).Assembly });
        var fileProvider = new FileSystemSchemaProvider(new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        });

        var resolver = new DefaultSchemaResolver(new ISchemaProvider[] { provider, fileProvider }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        // Should fall back to file system provider since embedded resource doesn't exist
        Assert.Contains("Simple Request Schema", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithCacheInvalidation_ShouldReloadSchema()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var schemaPath = Path.Combine(_tempSchemaDirectory, "DynamicSchema.schema.json");
        await File.WriteAllTextAsync(schemaPath, @"{""type"": ""object"", ""title"": ""Version 1""}");

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _tempSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(DynamicSchema), IsRequest = true };

        // Act - Load initial schema
        var schema1 = await resolver.ResolveSchemaAsync(typeof(DynamicSchema), context, CancellationToken.None);

        // Update schema file
        await File.WriteAllTextAsync(schemaPath, @"{""type"": ""object"", ""title"": ""Version 2""}");

        // Invalidate cache
        resolver.InvalidateSchema(typeof(DynamicSchema));

        // Load updated schema
        var schema2 = await resolver.ResolveSchemaAsync(typeof(DynamicSchema), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema1);
        Assert.NotNull(schema2);
        Assert.Contains("Version 1", schema1.Schema);
        Assert.Contains("Version 2", schema2.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_WithComplexNamingConvention_ShouldResolveCorrectly()
    {
        // Arrange
        var schemaPath = Path.Combine(_tempSchemaDirectory, "Relay.Core.Tests.ComplexType.request.schema.json");
        await File.WriteAllTextAsync(schemaPath, @"{""type"": ""object"", ""title"": ""Complex Type""}");

        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _tempSchemaDirectory },
            NamingConvention = "{TypeNamespace}.{TypeName}.{IsRequest}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var context = new SchemaContext { RequestType = typeof(ComplexType), IsRequest = true };

        // Act
        var schema = await provider.TryGetSchemaAsync(typeof(ComplexType), context, CancellationToken.None);

        // Assert
        Assert.NotNull(schema);
        Assert.Contains("Complex Type", schema.Schema);
    }

    [Fact]
    public async Task SchemaDiscovery_ConcurrentAccess_ShouldHandleThreadSafely()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache();
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act - Concurrent schema resolution
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None).AsTask()
        ).ToArray();

        var schemas = await Task.WhenAll(tasks);

        // Assert
        Assert.All(schemas, schema =>
        {
            Assert.NotNull(schema);
            Assert.Contains("Simple Request Schema", schema.Schema);
        });

        // Verify cache metrics
        var metrics = schemaCache.GetMetrics();
        Assert.True(metrics.CacheHits > 0); // Should have cache hits from concurrent access
    }

    // Test types
    public sealed class SimpleRequest { }
    public sealed class TestType1 { }
    public sealed class CustomRequest { }
    public sealed class InvalidSchema { }
    public sealed class DynamicSchema { }
    public sealed class ComplexType { }
}




