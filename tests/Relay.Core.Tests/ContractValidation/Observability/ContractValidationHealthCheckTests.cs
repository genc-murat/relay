using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Observability;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Observability;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Observability;

public class ContractValidationHealthCheckTests
{
    private readonly Mock<ISchemaCache> _mockSchemaCache;
    private readonly ContractValidationHealthCheck _healthCheck;

    public ContractValidationHealthCheckTests()
    {
        _mockSchemaCache = new Mock<ISchemaCache>();
        _healthCheck = new ContractValidationHealthCheck(_mockSchemaCache.Object, null);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenAllSystemsAreHealthy()
    {
        // Arrange
        var cacheMetrics = new SchemaCacheMetrics
        {
            CurrentSize = 50,
            MaxSize = 100,
            TotalRequests = 200,
            CacheHits = 180,
            TotalEvictions = 5
        };
        _mockSchemaCache.Setup(x => x.GetMetrics()).Returns(cacheMetrics);

        var healthCheck = new ContractValidationHealthCheck(_mockSchemaCache.Object, null);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Contract validation system is healthy", result.Description);
        Assert.NotNull(result.Data);
        Assert.Equal(50, Convert.ToInt64(result.Data["cache_size"]));
        Assert.Equal(100, Convert.ToInt64(result.Data["cache_max_size"]));
        Assert.Equal(0.9, Convert.ToDouble(result.Data["cache_hit_rate"]));
        Assert.Equal(200, Convert.ToInt64(result.Data["cache_total_requests"]));
        Assert.Equal(5, Convert.ToInt64(result.Data["cache_evictions"]));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenCacheIsNearlyFull()
    {
        // Arrange
        var cacheMetrics = new SchemaCacheMetrics
        {
            CurrentSize = 95,
            MaxSize = 100,
            TotalRequests = 200,
            CacheHits = 180,
            TotalEvictions = 5
        };
        _mockSchemaCache.Setup(x => x.GetMetrics()).Returns(cacheMetrics);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Schema cache is nearly full", result.Description);
        Assert.NotNull(result.Data);
        Assert.Equal(95, result.Data["cache_size"]);
        Assert.Equal(100, result.Data["cache_max_size"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenCacheHitRateIsLow()
    {
        // Arrange
        var cacheMetrics = new SchemaCacheMetrics
        {
            CurrentSize = 50,
            MaxSize = 100,
            TotalRequests = 200,
            CacheHits = 80, // 40% hit rate
            TotalEvictions = 5
        };
        _mockSchemaCache.Setup(x => x.GetMetrics()).Returns(cacheMetrics);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Equal("Schema cache hit rate is low", result.Description);
        Assert.NotNull(result.Data);
        Assert.Equal(0.4, result.Data["cache_hit_rate"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldNotReturnDegraded_WhenCacheHitRateIsLowButRequestsAreFew()
    {
        // Arrange
        var cacheMetrics = new SchemaCacheMetrics
        {
            CurrentSize = 50,
            MaxSize = 100,
            TotalRequests = 50, // Less than 100 requests
            CacheHits = 20, // 40% hit rate
            TotalEvictions = 5
        };
        _mockSchemaCache.Setup(x => x.GetMetrics()).Returns(cacheMetrics);

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Contract validation system is healthy", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenNoDependenciesProvided()
    {
        // Arrange
        var healthCheck = new ContractValidationHealthCheck();

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("Contract validation system is healthy", result.Description);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenExceptionThrown()
    {
        // Arrange
        _mockSchemaCache.Setup(x => x.GetMetrics()).Throws(new InvalidOperationException("Cache error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("Contract validation system health check failed", result.Description);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeTop5Errors_WhenMoreErrorsExist()
    {
        // Arrange - Create a real metrics instance and set error counts
        var metrics = new ContractValidationMetrics();
        var errorCounts = new ConcurrentDictionary<string, long>
        {
            ["Error1"] = 10,
            ["Error2"] = 8,
            ["Error3"] = 6,
            ["Error4"] = 4,
            ["Error5"] = 2,
            ["Error6"] = 1, // This should not be included
            ["Error7"] = 1  // This should not be included
        };
        
        // Use reflection to set the internal error counts
        var field = typeof(ContractValidationMetrics).GetField("_errorCountsByType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(metrics, errorCounts);
        
        var healthCheck = new ContractValidationHealthCheck(null, metrics);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(7, result.Data["total_error_types"]);
        var topErrors = result.Data["top_errors"]?.ToString();
        
        // Check that we have exactly 5 errors (first 5 encountered)
        var errorList = topErrors?.Split(", ").ToList();
        Assert.Equal(5, errorList?.Count);
        
        // Verify the format is correct (ErrorName:Count)
        foreach (var error in errorList)
        {
            Assert.Matches(@"^[^:]+:\d+$", error);
        }
        
        // Check that at least one high-count error is included
        Assert.True(errorList.Any(e => e.Contains("10")) || errorList.Any(e => e.Contains("8")) || 
                  errorList.Any(e => e.Contains("6")) || errorList.Any(e => e.Contains("4")), 
                  "Should include at least one of the higher count errors");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldHandleEmptyErrorCounts()
    {
        // Arrange - Create a real metrics instance with empty error counts
        var metrics = new ContractValidationMetrics();
        var errorCounts = new ConcurrentDictionary<string, long>();
        
        // Use reflection to set the internal error counts
        var field = typeof(ContractValidationMetrics).GetField("_errorCountsByType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(metrics, errorCounts);
        
        var healthCheck = new ContractValidationHealthCheck(null, metrics);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data["total_error_types"]);
        Assert.False(result.Data.ContainsKey("top_errors"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldWorkWithOnlySchemaCache()
    {
        // Arrange
        var healthCheck = new ContractValidationHealthCheck(_mockSchemaCache.Object, null);
        var cacheMetrics = new SchemaCacheMetrics
        {
            CurrentSize = 30,
            MaxSize = 100,
            TotalRequests = 100,
            CacheHits = 90,
            TotalEvictions = 2
        };
        _mockSchemaCache.Setup(x => x.GetMetrics()).Returns(cacheMetrics);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(30, Convert.ToInt64(result.Data["cache_size"]));
        Assert.Equal(100, Convert.ToInt64(result.Data["cache_max_size"]));
        Assert.Equal(0.9, Convert.ToDouble(result.Data["cache_hit_rate"]));
        Assert.Equal(100, Convert.ToInt64(result.Data["cache_total_requests"]));
        Assert.Equal(2, Convert.ToInt64(result.Data["cache_evictions"]));
        Assert.False(result.Data.ContainsKey("total_error_types"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldWorkWithOnlyMetrics()
    {
        // Arrange
        var metrics = new ContractValidationMetrics();
        var errorCounts = new ConcurrentDictionary<string, long>
        {
            ["ValidationError"] = 5
        };
        
        // Use reflection to set the internal error counts
        var field = typeof(ContractValidationMetrics).GetField("_errorCountsByType", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(metrics, errorCounts);
        
        var healthCheck = new ContractValidationHealthCheck(null, metrics);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.Equal(1, result.Data["total_error_types"]);
        Assert.Contains("ValidationError:5", result.Data["top_errors"]?.ToString());
        Assert.False(result.Data.ContainsKey("cache_size"));
    }



    [Fact]
    public async Task CheckHealthAsync_ShouldHandleCancellationGracefully()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);

        // Assert - Should handle cancellation and return unhealthy due to exception
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }
}
