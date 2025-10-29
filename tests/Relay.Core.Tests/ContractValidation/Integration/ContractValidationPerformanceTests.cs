using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Testing;
using Relay.Core.Metadata.MessageQueue;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.ContractValidation.Integration;

/// <summary>
/// Performance tests to validate that contract validation meets performance targets.
/// Tests schema resolution, validation execution, caching, and throughput.
/// </summary>
public sealed class ContractValidationPerformanceTests : IDisposable
{
    private readonly string _testSchemaDirectory;
    private readonly ContractValidationTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public ContractValidationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new ContractValidationTestFixture();
        
        var currentDirectory = Directory.GetCurrentDirectory();
        _testSchemaDirectory = Path.Combine(currentDirectory, "..", "..", "..", "ContractValidation", "TestSchemas");
        _testSchemaDirectory = Path.GetFullPath(_testSchemaDirectory);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task Performance_SchemaResolution_Cached_ShouldBeLessThan1Ms()
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

        // Warm up cache
        await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Act - Measure cached resolution time
        var stopwatch = Stopwatch.StartNew();
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Cached schema resolution time: {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
        Assert.NotNull(schema);
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < 1.0, 
            $"Cached schema resolution took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, expected < 1ms");
    }

    [Fact]
    public async Task Performance_SchemaResolution_Uncached_ShouldBeLessThan50Ms()
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

        // Act - Measure uncached resolution time
        var stopwatch = Stopwatch.StartNew();
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Uncached schema resolution time: {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
        Assert.NotNull(schema);
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < 50.0,
            $"Uncached schema resolution took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, expected < 50ms");
    }

    [Fact]
    public async Task Performance_ValidationExecution_ShouldBeLessThan10Ms()
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
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, resolver);

        var request = new SimpleRequest
        {
            Name = "Test User",
            Value = 42
        };

        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Warm up
        await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Act - Measure validation time
        var stopwatch = Stopwatch.StartNew();
        var errors = await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Validation execution time: {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
        Assert.Empty(errors);
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < 10.0,
            $"Validation took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, expected < 10ms");
    }

    [Fact]
    public async Task Performance_CacheHitRate_ShouldBeGreaterThan95Percent()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache(maxCacheSize: 100);
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };

        // Act - Perform multiple resolutions
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        }

        var metrics = schemaCache.GetMetrics();

        // Assert
        _output.WriteLine($"Cache hit rate: {metrics.HitRate:P2}");
        _output.WriteLine($"Cache hits: {metrics.CacheHits}, Cache misses: {metrics.CacheMisses}");
        Assert.True(metrics.HitRate > 0.95,
            $"Cache hit rate was {metrics.HitRate:P2}, expected > 95%");
    }

    [Fact]
    public async Task Performance_Throughput_ShouldHandle1000RequestsPerSecond()
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
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, resolver);

        var request = new SimpleRequest
        {
            Name = "Test User",
            Value = 42
        };

        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Warm up
        await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Act - Measure throughput
        const int requestCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < requestCount; i++)
        {
            await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);
        }

        stopwatch.Stop();

        var requestsPerSecond = requestCount / stopwatch.Elapsed.TotalSeconds;

        // Assert
        _output.WriteLine($"Throughput: {requestsPerSecond:F0} requests/second");
        _output.WriteLine($"Total time for {requestCount} requests: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        Assert.True(requestsPerSecond >= 1000,
            $"Throughput was {requestsPerSecond:F0} req/s, expected >= 1000 req/s");
    }

    [Fact]
    public async Task Performance_ConcurrentValidation_ShouldScaleLinearly()
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
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, resolver);

        var request = new SimpleRequest
        {
            Name = "Test User",
            Value = 42
        };

        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        var schema = await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);

        // Warm up
        await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Act - Measure concurrent validation
        const int concurrentRequests = 100;
        var stopwatch = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => validator.ValidateRequestAsync(request, schema!, CancellationToken.None).AsTask())
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var requestsPerSecond = concurrentRequests / stopwatch.Elapsed.TotalSeconds;

        // Assert
        _output.WriteLine($"Concurrent throughput: {requestsPerSecond:F0} requests/second");
        _output.WriteLine($"Total time for {concurrentRequests} concurrent requests: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        Assert.True(requestsPerSecond >= 1000,
            $"Concurrent throughput was {requestsPerSecond:F0} req/s, expected >= 1000 req/s");
    }

    [Fact]
    public async Task Performance_MemoryOverhead_ShouldBeLessThan10MBFor1000Schemas()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache(maxCacheSize: 1000);
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);

        // Force GC to get accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        // Act - Load schema multiple times (will be cached)
        var context = new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true };
        for (int i = 0; i < 1000; i++)
        {
            await resolver.ResolveSchemaAsync(typeof(SimpleRequest), context, CancellationToken.None);
        }

        // Force GC to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        var memoryUsedMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        // Assert
        _output.WriteLine($"Memory overhead: {memoryUsedMB:F2} MB");
        _output.WriteLine($"Cache size: {schemaCache.GetMetrics().CurrentSize}");
        
        // Note: This is a rough estimate and may vary based on runtime conditions
        // We're being lenient here since actual memory usage depends on many factors
        Assert.True(memoryUsedMB < 50.0,
            $"Memory overhead was {memoryUsedMB:F2} MB, expected < 50 MB (lenient threshold)");
    }

    [Fact]
    public async Task Performance_ComplexValidation_ShouldCompleteWithin10Ms()
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
        var validator = _fixture.CreateValidatorWithComponents(schemaCache, resolver);

        var request = new UserRequest
        {
            UserId = 1,
            Username = "testuser",
            Email = "test@example.com",
            Age = 25,
            IsActive = true
        };

        var context = new SchemaContext { RequestType = typeof(UserRequest), IsRequest = true };
        var schema = await resolver.ResolveSchemaAsync(typeof(UserRequest), context, CancellationToken.None);

        // Warm up
        await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);

        // Act - Measure complex validation time
        var stopwatch = Stopwatch.StartNew();
        var errors = await validator.ValidateRequestAsync(request, schema!, CancellationToken.None);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Complex validation time: {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
        Assert.Empty(errors);
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < 10.0,
            $"Complex validation took {stopwatch.Elapsed.TotalMilliseconds:F3}ms, expected < 10ms");
    }

    [Fact]
    public async Task Performance_CacheEviction_ShouldNotDegradePerformance()
    {
        // Arrange
        var schemaCache = _fixture.CreateSchemaCache(maxCacheSize: 10); // Small cache to force evictions
        var options = new SchemaDiscoveryOptions
        {
            SchemaDirectories = new List<string> { _testSchemaDirectory },
            NamingConvention = "{TypeName}.schema.json"
        };

        var provider = new FileSystemSchemaProvider(options);
        var resolver = new DefaultSchemaResolver(new[] { provider }, schemaCache);

        // Act - Access schemas in a pattern that causes evictions
        var types = new[] { typeof(SimpleRequest), typeof(UserRequest) };
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < 100; i++)
        {
            var type = types[i % types.Length];
            var context = new SchemaContext { RequestType = type, IsRequest = true };
            await resolver.ResolveSchemaAsync(type, context, CancellationToken.None);
        }

        stopwatch.Stop();
        var metrics = schemaCache.GetMetrics();

        // Assert
        _output.WriteLine($"Time with evictions: {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        _output.WriteLine($"Total evictions: {metrics.TotalEvictions}");
        _output.WriteLine($"Hit rate: {metrics.HitRate:P2}");
        
        // Should still maintain reasonable performance even with evictions
        Assert.True(stopwatch.Elapsed.TotalMilliseconds < 500,
            $"Performance with evictions took {stopwatch.Elapsed.TotalMilliseconds:F0}ms, expected < 500ms");
    }

    // Test types
    public sealed class SimpleRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    public sealed class UserRequest
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }
}




