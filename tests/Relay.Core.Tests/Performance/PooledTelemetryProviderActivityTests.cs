using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Performance.Extensions;
using System.Threading;
using Relay.Core.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderActivityTests
{
    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_SetCorrelationId()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "test-correlation-123";

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_SetCorrelationId_Should_HandleNullActivity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "test-correlation-456";

        // Ensure no current activity
        Activity.Current = null;

        // Act
        telemetryProvider.SetCorrelationId(correlationId);

        // Assert
        Assert.Equal(correlationId, telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_GetCorrelationId_Should_FallbackToActivityTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var correlationId = "activity-correlation-789";

        // Ensure no correlation ID is set in the context
        telemetryProvider.SetCorrelationId(null);

        // Create activity with correlation ID tag
        var activity = telemetryProvider.StartActivity("Test", typeof(string), correlationId);
        Assert.NotNull(activity);

        // Act
        var result = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, result);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_HandleNullCorrelationId()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), null);

        // Assert
        Assert.NotNull(activity);
        Assert.Null(telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_ReturnNullWhenSamplingDenies()
    {
        // Arrange - Add activity listener that denies sampling
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.None,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "test-id");

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public void PooledTelemetryProvider_GetCorrelationId_Should_ReturnNullWhenNoContextOrActivity()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Clear any existing activity
        Activity.Current = null;

        // Clear correlation ID context
        telemetryProvider.SetCorrelationId(null);

        // Act
        var result = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_SetActivityTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var operationName = "TestOperation";
        var requestType = typeof(string);
        var correlationId = "test-correlation-789";

        // Act
        var activity = telemetryProvider.StartActivity(operationName, requestType, correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(operationName, activity.OperationName);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(operationName, activity.GetTagItem("relay.operation"));
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleMultipleSequentialActivities()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act
        var activity1 = telemetryProvider.StartActivity("Operation1", typeof(string), "corr1");
        var activity2 = telemetryProvider.StartActivity("Operation2", typeof(int), "corr2");
        var activity3 = telemetryProvider.StartActivity("Operation3", typeof(bool), null);

        // Assert
        Assert.NotNull(activity1);
        Assert.NotNull(activity2);
        Assert.NotNull(activity3);
        Assert.Equal("Operation1", activity1.OperationName);
        Assert.Equal("Operation2", activity2.OperationName);
        Assert.Equal("Operation3", activity3.OperationName);
    }

    [Fact]
    public void PooledTelemetryProvider_StartActivity_Should_SetCorrelationIdTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Relay.Core",
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var correlationId = "test-correlation-123";

        // Act
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
        Assert.Equal("TestOperation", activity.OperationName);
        Assert.Equal(typeof(string).FullName, activity.GetTagItem("relay.request_type"));
    }

    [Fact]
    public async Task PooledTelemetryProvider_CorrelationId_Should_BeThreadSafe()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        var correlationIds = new[] { "corr1", "corr2", "corr3", "corr4", "corr5" };
        var results = new string[correlationIds.Length];

        // Act
        var tasks = correlationIds.Select(async (corrId, index) =>
        {
            await Task.Yield(); // Force context switch
            telemetryProvider.SetCorrelationId(corrId);
            await Task.Delay(10); // Small delay to increase chance of race condition
            results[index] = telemetryProvider.GetCorrelationId();
        });

        await Task.WhenAll(tasks);

        // Assert - Each task should have gotten back its own correlation ID
        for (int i = 0; i < correlationIds.Length; i++)
        {
            Assert.Equal(correlationIds[i], results[i]);
        }
    }
}