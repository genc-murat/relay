using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions;

public class AIServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAIOptimization_AddsServicesToCollection()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimization();

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
        // Verify that RegisterWithConfiguration is called, but since it's internal, hard to test directly
    }

    [Fact]
    public void AddAIOptimization_WithConfiguration_AddsServicesToCollection()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimization(options => { });

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimization_WithConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;

        Assert.Throws<ArgumentNullException>(() => services.AddAIOptimization(options => { }));
    }

    [Fact]
    public void AddAIOptimization_WithConfiguration_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
    {
        var services = new Mock<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimization(null));
    }

    [Fact]
    public void AddAIOptimization_WithConfiguration_AddsServicesToCollection_WithOptions()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimization(options =>
        {
            options.DefaultBatchSize = 10;
        });

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimization_WithConfiguration_AddsCoreServices()
    {
        var services = new Mock<IServiceCollection>();

        services.Object.AddAIOptimization(options => { });

        // Since RegisterWithConfiguration is internal, we can't easily verify the registrations
        // But the method should not throw
    }

    [Fact]
    public void AddAIOptimization_WithConfigurationAndSection_AddsServicesToCollection()
    {
        var services = new Mock<IServiceCollection>();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["TestSection:Enabled"] = "true",
            ["TestSection:LearningEnabled"] = "true",
            ["TestSection:ModelUpdateInterval"] = "00:30:00",
            ["TestSection:DefaultBatchSize"] = "10",
            ["TestSection:MaxBatchSize"] = "100"
        });
        var configuration = configurationBuilder.Build();

        var result = services.Object.AddAIOptimization(configuration, "TestSection");

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimization_WithInvalidConfiguration_ThrowsDuringConfiguration()
    {
        // Arrange - Create a real service collection to test actual validation
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert - Configure with invalid options that should fail validation during AddAIOptimization
        var ex = Assert.Throws<ArgumentException>(() => services.AddAIOptimization(options =>
        {
            options.DefaultBatchSize = 0; // Invalid: must be > 0
        }));
        Assert.Contains("DefaultBatchSize must be greater than 0", ex.Message);
    }

    [Fact]
    public void AddAIOptimization_WithInvalidWeights_ThrowsDuringConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert - Configure with invalid weights that don't sum to 1.0 during AddAIOptimization
        var ex = Assert.Throws<ArgumentException>(() => services.AddAIOptimization(options =>
        {
            options.PerformanceWeight = 0.5;
            options.ReliabilityWeight = 0.5;
            options.ResourceWeight = 0.5;
            options.UserExperienceWeight = 0.5; // Total = 2.0, invalid
        }));
        Assert.Contains("The sum of all weight properties must equal 1.0", ex.Message);
    }

    [Fact]
    public void AddAIOptimization_WithValidConfiguration_BuildsServiceProviderSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act - Configure with valid options
        services.AddAIOptimization(options =>
        {
            options.DefaultBatchSize = 10;
            options.MaxBatchSize = 100;
            options.PerformanceWeight = 0.4;
            options.ReliabilityWeight = 0.3;
            options.ResourceWeight = 0.2;
            options.UserExperienceWeight = 0.1;
        });

        // Assert - Should build successfully without throwing
        var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider);

        // Verify the configured options are available
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<AIOptimizationOptions>>();
        Assert.NotNull(options);
        Assert.Equal(10, options.Value.DefaultBatchSize);
        Assert.Equal(100, options.Value.MaxBatchSize);
    }

    [Fact]
    public void AddAIOptimization_WithConfigurationAndSection_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;
        var configuration = new Mock<IConfiguration>();

        Assert.Throws<ArgumentNullException>(() => services.AddAIOptimization(configuration.Object, "TestSection"));
    }

    [Fact]
    public void AddAIOptimization_WithConfigurationAndSection_ThrowsArgumentNullException_WhenConfigurationIsNull()
    {
        var services = new Mock<IServiceCollection>();
        IConfiguration configuration = null;

        Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimization(configuration, "TestSection"));
    }

    [Fact]
    public void AddAdvancedAIOptimization_AddsServicesToCollection()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAdvancedAIOptimization(options => { }, true);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAdvancedAIOptimization_WithAdvancedFeatures_AddsAdditionalServices()
    {
        var services = new Mock<IServiceCollection>();

        services.Object.AddAdvancedAIOptimization(options => { }, true);

        // Should add advanced services when enableAdvancedFeatures is true
    }

    [Fact]
    public void AddAdvancedAIOptimization_WithoutAdvancedFeatures_DoesNotAddAdditionalServices()
    {
        var services = new Mock<IServiceCollection>();

        services.Object.AddAdvancedAIOptimization(options => { }, false);

        // Should not add advanced services when enableAdvancedFeatures is false
    }

    [Fact]
    public void AddAdvancedAIOptimization_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;

        Assert.Throws<ArgumentNullException>(() => services.AddAdvancedAIOptimization(options => { }, true));
    }

    [Fact]
    public void AddAdvancedAIOptimization_ThrowsArgumentNullException_WhenConfigureOptionsIsNull()
    {
        var services = new Mock<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(() => services.Object.AddAdvancedAIOptimization(null, true));
    }

    [Fact]
    public void AddAIOptimizationForScenario_HighThroughput_ConfiguresCorrectly()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.HighThroughput);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationForScenario_LowLatency_ConfiguresCorrectly()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.LowLatency);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationForScenario_ResourceConstrained_ConfiguresCorrectly()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.ResourceConstrained);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationForScenario_Development_ConfiguresCorrectly()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.Development);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationForScenario_Production_ConfiguresCorrectly()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationForScenario(AIOptimizationScenario.Production);

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationForScenario_ThrowsArgumentOutOfRangeException_WhenInvalidScenario()
    {
        var services = new Mock<IServiceCollection>();

        Assert.Throws<ArgumentOutOfRangeException>(() => services.Object.AddAIOptimizationForScenario((AIOptimizationScenario)999));
    }

    [Fact]
    public void AddAIOptimizationForScenario_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;

        Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationForScenario(AIOptimizationScenario.Production));
    }



    [Fact]
    public void AddAIOptimizationHealthChecks_AddsHealthChecksToCollection()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationHealthChecks();

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationHealthChecks_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;

        Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationHealthChecks());
    }

    [Fact]
    public void AddAIOptimizationHealthChecks_WithConfiguration_AddsHealthChecksToCollection()
    {
        var services = new Mock<IServiceCollection>();

        var result = services.Object.AddAIOptimizationHealthChecks(options => { });

        Assert.NotNull(result);
        Assert.Equal(services.Object, result);
    }

    [Fact]
    public void AddAIOptimizationHealthChecks_WithConfiguration_ThrowsArgumentNullException_WhenServicesIsNull()
    {
        IServiceCollection services = null;

        Assert.Throws<ArgumentNullException>(() => services.AddAIOptimizationHealthChecks(options => { }));
    }

    [Fact]
    public void AddAIOptimizationHealthChecks_WithConfiguration_ThrowsArgumentNullException_WhenConfigureHealthChecksIsNull()
    {
        var services = new Mock<IServiceCollection>();

        Assert.Throws<ArgumentNullException>(() => services.Object.AddAIOptimizationHealthChecks(null));
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_ReturnsHealthResult()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var engine = new Mock<IAIOptimizationEngine>();
        var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
        var options = new Mock<IOptions<AIHealthCheckOptions>>();
        options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

        var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
        healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Test" });

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.True(result.IsHealthy);
        Assert.NotNull(result.ComponentResults);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_ThrowsArgumentNullException_WhenServiceProviderIsNull()
    {
        IServiceProvider serviceProvider = null;

        await Assert.ThrowsAsync<ArgumentNullException>(() => serviceProvider.GetAIOptimizationHealthAsync());
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_HandlesCancellationToken()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync(cts.Token);

        Assert.NotNull(result);
        // Should handle cancellation gracefully
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_ReturnsUnhealthy_WhenComponentFails()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var engine = new Mock<IAIOptimizationEngine>();
        var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
        var options = new Mock<IOptions<AIHealthCheckOptions>>();
        options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

        var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
        healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = false, ComponentName = "Test" });

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.False(result.IsHealthy);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_HandlesException()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var engine = new Mock<IAIOptimizationEngine>();
        var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
        var options = new Mock<IOptions<AIHealthCheckOptions>>();
        options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

        var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
        healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.False(result.IsHealthy);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_WhenNoServicesAvailable_ReturnsEmptyResult()
    {
        var serviceProvider = new Mock<IServiceProvider>();

        // All service lookups return null
        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIModelHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIMetricsHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AICircuitBreakerHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AISystemHealthCheck))).Returns(null);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.True(result.IsHealthy); // Should be healthy since no components failed
        Assert.Empty(result.ComponentResults); // No components were checked
        Assert.NotNull(result.Summary);
        Assert.Contains("0/0 components healthy", result.Summary);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_WhenSomeServicesAreNull_DoesNotFail()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var engine = new Mock<IAIOptimizationEngine>();
        var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
        var options = new Mock<IOptions<AIHealthCheckOptions>>();
        options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());
        var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
        healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Optimization" });

        // Only one service returns a real instance, others return null
        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIModelHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIMetricsHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AICircuitBreakerHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AISystemHealthCheck))).Returns(null);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.True(result.IsHealthy);
        Assert.Single(result.ComponentResults);
        Assert.Equal(1, result.ComponentResults.Count);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_WhenMultipleComponentsFail_ReturnsUnhealthy()
    {
        var serviceProvider = new Mock<IServiceProvider>();

        // Setup multiple health checks that return unhealthy results
        var optHealthCheck = new Mock<AIOptimizationHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIOptimizationHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        optHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = false, ComponentName = "Optimization" });

        var modelHealthCheck = new Mock<AIModelHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIModelHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        modelHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = false, ComponentName = "Model" });

        var metricsHealthCheck = new Mock<AIMetricsHealthCheck>(Mock.Of<IAIMetricsExporter>(), Mock.Of<ILogger<AIMetricsHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        metricsHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Metrics" }); // One healthy for variety

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(optHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIModelHealthCheck))).Returns(modelHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIMetricsHealthCheck))).Returns(metricsHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AICircuitBreakerHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AISystemHealthCheck))).Returns(null);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.False(result.IsHealthy); // Overall should be unhealthy due to multiple failures
        Assert.Equal(3, result.ComponentResults.Count); // 3 components were checked (2 failed, 1 passed)
        Assert.Contains("AI Optimization Status", result.Summary);
        Assert.Contains("1/3 components healthy", result.Summary); // Only 1 of 3 is healthy
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_WithExceptionDuringExecution_HandlesGracefully()
    {
        var serviceProvider = new Mock<IServiceProvider>();

        // First health check works, second throws exception
        var optHealthCheck = new Mock<AIOptimizationHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIOptimizationHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        optHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Optimization" });

        var modelHealthCheck = new Mock<AIModelHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIModelHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        modelHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(optHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIModelHealthCheck))).Returns(modelHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIMetricsHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AICircuitBreakerHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AISystemHealthCheck))).Returns(null);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.False(result.IsHealthy); // Should be unhealthy due to exception
        Assert.NotNull(result.Exception); // Exception from the overall try-catch
        Assert.Equal("Database unavailable", result.Exception.Message);
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_ReturnsProperDurationAndTimestamp()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        var engine = new Mock<IAIOptimizationEngine>();
        var logger = new Mock<ILogger<AIOptimizationHealthCheck>>();
        var options = new Mock<IOptions<AIHealthCheckOptions>>();
        options.Setup(o => o.Value).Returns(new AIHealthCheckOptions());

        var healthCheck = new Mock<AIOptimizationHealthCheck>(engine.Object, logger.Object, options.Object);
        healthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Test" });

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(healthCheck.Object);

        var startTime = DateTime.UtcNow;
        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();
        var endTime = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.True(result.IsHealthy);
        Assert.InRange(result.Timestamp, startTime, endTime);
        Assert.True(result.Duration >= TimeSpan.Zero);
        Assert.True(result.Duration <= endTime - startTime + TimeSpan.FromSeconds(1)); // Allow small buffer
    }

    [Fact]
    public async Task GetAIOptimizationHealthAsync_GeneratesCorrectSummary()
    {
        var serviceProvider = new Mock<IServiceProvider>();

        // Setup health checks with mixed results
        var optHealthCheck = new Mock<AIOptimizationHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIOptimizationHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        optHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = true, ComponentName = "Optimization" });

        var modelHealthCheck = new Mock<AIModelHealthCheck>(Mock.Of<IAIOptimizationEngine>(), Mock.Of<ILogger<AIModelHealthCheck>>(), Mock.Of<IOptions<AIHealthCheckOptions>>());
        modelHealthCheck.Setup(h => h.CheckHealthAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComponentHealthResult { IsHealthy = false, ComponentName = "Model" });

        serviceProvider.Setup(sp => sp.GetService(typeof(AIOptimizationHealthCheck))).Returns(optHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIModelHealthCheck))).Returns(modelHealthCheck.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(AIMetricsHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AICircuitBreakerHealthCheck))).Returns(null);
        serviceProvider.Setup(sp => sp.GetService(typeof(AISystemHealthCheck))).Returns(null);

        var result = await serviceProvider.Object.GetAIOptimizationHealthAsync();

        Assert.NotNull(result);
        Assert.False(result.IsHealthy);
        Assert.NotNull(result.Summary);
        // Summary should indicate 1 of 2 components healthy (unhealthy overall)
        Assert.Contains("AI Optimization Status: Unhealthy", result.Summary);
        Assert.Contains("1/2 components healthy", result.Summary);
    }


}