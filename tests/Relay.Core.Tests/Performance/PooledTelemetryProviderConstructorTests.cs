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
using Relay.Core.Testing;
using Relay.Core.Telemetry;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq;

namespace Relay.Core.Tests.Performance;

public class PooledTelemetryProviderConstructorTests
{
    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_HandleNullMetricsProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, null, null);

        // Assert
        Assert.NotNull(telemetryProvider.MetricsProvider);
        Assert.IsType<DefaultMetricsProvider>(telemetryProvider.MetricsProvider);
    }

    [Fact]
    public void PooledTelemetryProvider_WithLogger_Should_StartActivityAndRecordExecutionWithoutErrors()
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
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var logger = provider.GetRequiredService<ILogger<PooledTelemetryProvider>>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool, logger);

        // Act - Start activity and record execution
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "test-id");
        var exception = Record.Exception(() =>
            telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true));

        // Assert
        Assert.NotNull(activity);
        Assert.Null(exception);
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_Should_ThrowWhenContextPoolIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PooledTelemetryProvider(null!));
    }

    [Fact]
    public void PooledTelemetryProvider_Constructor_WithLogger_Should_CreateInstanceSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var logger = provider.GetRequiredService<ILogger<PooledTelemetryProvider>>();

        // Act
        var telemetryProvider = new PooledTelemetryProvider(contextPool, logger);

        // Assert
        Assert.NotNull(telemetryProvider);
    }
}
