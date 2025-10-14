using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;
using Relay.Core.Performance.BufferManagement;
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

namespace Relay.Core.Tests.Performance;

public class ObjectPoolingTests
{
    [Fact]
    public void TelemetryContextPool_Should_ReuseObjects()
    {
        // Arrange
        var context1 = TelemetryContextPool.Get();
        var originalRequestId = context1.RequestId;

        // Act
        TelemetryContextPool.Return(context1);
        var context2 = TelemetryContextPool.Get();

        // Assert
        Assert.Same(context1, context2);
        Assert.NotEqual(originalRequestId, context2.RequestId); // Should have new ID
        Assert.Null(context2.CorrelationId); // Should be reset
        Assert.Empty(context2.Properties); // Should be cleared
    }

    [Fact]
    public void TelemetryContextPool_Create_Should_SetProperties()
    {
        // Arrange
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var correlationId = "test-correlation";

        // Act
        var context = TelemetryContextPool.Create(requestType, responseType, handlerName, correlationId);

        // Assert
        Assert.Equal(requestType, context.RequestType);
        Assert.Equal(responseType, context.ResponseType);
        Assert.Equal(handlerName, context.HandlerName);
        Assert.Equal(correlationId, context.CorrelationId);

        // Cleanup
        TelemetryContextPool.Return(context);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Should_IntegrateWithDI()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        // Act
        var pool = provider.GetRequiredService<ITelemetryContextPool>();
        var context = pool.Get();

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.RequestId);

        // Cleanup
        pool.Return(context);
    }

    [Fact]
    public void PooledBufferManager_Should_ReuseBuffers()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var buffer1 = bufferManager.RentBuffer(1024);
        bufferManager.ReturnBuffer(buffer1);
        var buffer2 = bufferManager.RentBuffer(1024);

        // Assert
        Assert.Same(buffer1, buffer2);

        // Cleanup
        bufferManager.ReturnBuffer(buffer2);
    }

    [Fact]
    public void PooledBufferManager_RentSpan_Should_ReturnCorrectSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();
        var requestedSize = 512;

        // Act
        var span = bufferManager.RentSpan(requestedSize);

        // Assert
        Assert.Equal(requestedSize, span.Length);

        // Note: We can't easily return the span's buffer in this test
        // as we don't have direct access to it
    }

    [Fact]
    public void PooledTelemetryProvider_Should_ReduceAllocations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - This test validates that the pooled provider works
        // Actual allocation measurement would require more sophisticated tooling
        var requestType = typeof(string);
        var responseType = typeof(int);
        var duration = TimeSpan.FromMilliseconds(100);

        // Should not throw and should complete successfully
        telemetryProvider.RecordHandlerExecution(requestType, responseType, "TestHandler", duration, true);
        telemetryProvider.RecordNotificationPublish(requestType, 3, duration, true);
        telemetryProvider.RecordStreamingOperation(requestType, responseType, "StreamHandler", duration, 100, true);

        Assert.True(true); // Test passes if no exceptions are thrown
    }

    [Fact]
    public void SpanExtensions_CopyToSpan_Should_CopyCorrectly()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[3];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(3, copied);
        Assert.Equal(1, destination[0]);
        Assert.Equal(2, destination[1]);
        Assert.Equal(3, destination[2]);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Should_HandleBounds()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act & Assert
        var slice1 = span.SafeSlice(5, 3);
        Assert.Equal(3, slice1.Length);

        var slice2 = span.SafeSlice(8, 5); // Should be clamped to available length
        Assert.Equal(2, slice2.Length);

        var slice3 = span.SafeSlice(15, 5); // Out of bounds
        Assert.True(slice3.IsEmpty);

        var slice4 = span.SafeSlice(-1, 5); // Negative start
        Assert.True(slice4.IsEmpty);
    }

    [Fact]
    public void TelemetryContextPooledObjectPolicy_Should_ResetState()
    {
        // Arrange
        var policy = new TelemetryContextPooledObjectPolicy();
        var context = policy.Create();

        // Modify the context
        context.CorrelationId = "test";
        context.HandlerName = "TestHandler";
        context.Properties["key"] = "value";

        // Act
        var canReturn = policy.Return(context);

        // Assert
        Assert.True(canReturn);
        Assert.Null(context.CorrelationId);
        Assert.Null(context.HandlerName);
        Assert.Empty(context.Properties);
    }

    [Fact]
    public void TelemetryContextPooledObjectPolicy_Should_HandleNullContext()
    {
        // Arrange
        var policy = new TelemetryContextPooledObjectPolicy();

        // Act
        var canReturn = policy.Return(null!);

        // Assert
        Assert.False(canReturn);
    }

    [Fact]
    public void TelemetryContextPool_Should_HandleMultipleGets()
    {
        // Arrange & Act
        var context1 = TelemetryContextPool.Get();
        var context2 = TelemetryContextPool.Get();
        var context3 = TelemetryContextPool.Get();

        // Assert
        Assert.NotSame(context1, context2);
        Assert.NotSame(context2, context3);
        Assert.NotSame(context1, context3);

        // Cleanup
        TelemetryContextPool.Return(context1);
        TelemetryContextPool.Return(context2);
        TelemetryContextPool.Return(context3);
    }

    [Fact]
    public void TelemetryContextPool_Should_GenerateUniqueRequestIds()
    {
        // Arrange
        var context1 = TelemetryContextPool.Get();
        var context2 = TelemetryContextPool.Get();

        // Act & Assert
        Assert.NotEqual(context1.RequestId, context2.RequestId);

        // Cleanup
        TelemetryContextPool.Return(context1);
        TelemetryContextPool.Return(context2);
    }

    [Fact]
    public void TelemetryContextPool_Create_WithNullCorrelationId_Works()
    {
        // Arrange & Act
        var context = TelemetryContextPool.Create(typeof(string), typeof(int), "Handler", null);

        // Assert
        Assert.Null(context.CorrelationId);
        Assert.Equal("Handler", context.HandlerName);

        // Cleanup
        TelemetryContextPool.Return(context);
    }

    [Fact]
    public void TelemetryContextPool_Create_WithNullHandlerName_Works()
    {
        // Arrange & Act
        var context = TelemetryContextPool.Create(typeof(string), typeof(int), null, "correlation-id");

        // Assert
        Assert.Null(context.HandlerName);
        Assert.Equal("correlation-id", context.CorrelationId);

        // Cleanup
        TelemetryContextPool.Return(context);
    }

    [Fact]
    public void PooledBufferManager_Should_HandleMultipleSizes()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var buffer1 = bufferManager.RentBuffer(512);
        var buffer2 = bufferManager.RentBuffer(1024);
        var buffer3 = bufferManager.RentBuffer(2048);

        // Assert
        Assert.True(buffer1.Length >= 512);
        Assert.True(buffer2.Length >= 1024);
        Assert.True(buffer3.Length >= 2048);

        // Cleanup
        bufferManager.ReturnBuffer(buffer1);
        bufferManager.ReturnBuffer(buffer2);
        bufferManager.ReturnBuffer(buffer3);
    }

    [Fact]
    public void PooledBufferManager_Should_HandleZeroSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var buffer = bufferManager.RentBuffer(0);

        // Assert
        Assert.NotNull(buffer);

        // Cleanup
        bufferManager.ReturnBuffer(buffer);
    }

    [Fact]
    public void PooledBufferManager_RentSpan_Should_HandleLargeSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var span = bufferManager.RentSpan(10000);

        // Assert
        Assert.Equal(10000, span.Length);
    }

    [Fact]
    public void PooledBufferManager_RentSpan_Should_HandleSmallSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var span = bufferManager.RentSpan(16);

        // Assert
        Assert.Equal(16, span.Length);
    }

    [Fact]
    public void SpanExtensions_CopyToSpan_Should_HandleEmptySource()
    {
        // Arrange
        var source = Array.Empty<byte>().AsSpan();
        var destination = new byte[10];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(0, copied);
    }

    [Fact]
    public void SpanExtensions_CopyToSpan_Should_HandleEmptyDestination()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3 }.AsSpan();
        var destination = Array.Empty<byte>();

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(0, copied);
    }

    [Fact]
    public void SpanExtensions_CopyToSpan_Should_HandleExactSize()
    {
        // Arrange
        var source = new byte[] { 1, 2, 3, 4, 5 }.AsSpan();
        var destination = new byte[5];

        // Act
        var copied = source.CopyToSpan(destination);

        // Assert
        Assert.Equal(5, copied);
        Assert.Equal(source.ToArray(), destination);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Should_HandleZeroLength()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act
        var slice = span.SafeSlice(5, 0);

        // Assert
        Assert.Equal(0, slice.Length);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Should_HandleFullSpan()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act
        var slice = span.SafeSlice(0, 10);

        // Assert
        Assert.Equal(10, slice.Length);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Should_HandleStartAtEnd()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act
        var slice = span.SafeSlice(10, 5);

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void TelemetryContextPooledObjectPolicy_Create_Should_GenerateNewContext()
    {
        // Arrange
        var policy = new TelemetryContextPooledObjectPolicy();

        // Act
        var context1 = policy.Create();
        var context2 = policy.Create();

        // Assert
        Assert.NotSame(context1, context2);
        Assert.NotNull(context1.RequestId);
        Assert.NotNull(context2.RequestId);
    }

    [Fact]
    public void TelemetryContextPooledObjectPolicy_Return_Should_ClearRequestType()
    {
        // Arrange
        var policy = new TelemetryContextPooledObjectPolicy();
        var context = policy.Create();
        context.RequestType = typeof(string);

        // Act
        policy.Return(context);

        // Assert
        Assert.Null(context.RequestType);
    }

    [Fact]
    public void TelemetryContextPooledObjectPolicy_Return_Should_ClearResponseType()
    {
        // Arrange
        var policy = new TelemetryContextPooledObjectPolicy();
        var context = policy.Create();
        context.ResponseType = typeof(int);

        // Act
        policy.Return(context);

        // Assert
        Assert.Null(context.ResponseType);
    }

    [Fact]
    public async Task TelemetryContextPool_Should_BeThreadSafe()
    {
        // Arrange
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                var context = TelemetryContextPool.Get();
                Assert.NotNull(context);
                TelemetryContextPool.Return(context);
            });
        }

        await Task.WhenAll(tasks);

        // Assert - if we reach here without deadlocks, test passes
        Assert.True(true);
    }

    [Fact]
    public void PooledBufferManager_Should_ReturnBufferWithCorrectMinSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act
        var buffer = bufferManager.RentBuffer(100);

        // Assert
        Assert.True(buffer.Length >= 100);

        // Cleanup
        bufferManager.ReturnBuffer(buffer);
    }

    [Fact]
    public void PooledBufferManager_Should_HandleNegativeSize()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();

        // Act & Assert - Should not throw, implementation may handle it gracefully
        try
        {
            var buffer = bufferManager.RentBuffer(-1);
            Assert.NotNull(buffer);
            bufferManager.ReturnBuffer(buffer);
        }
        catch (ArgumentOutOfRangeException)
        {
            // This is also acceptable behavior
            Assert.True(true);
        }
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_HandleZeroHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordNotificationPublish(typeof(string), 0, TimeSpan.FromMilliseconds(50), true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_HandleZeroItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(50), 0, true);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_Should_HandleFailedExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false);
        Assert.True(true);
    }

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
    public void PooledTelemetryProvider_RecordHandlerExecution_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "Handler", TimeSpan.FromMilliseconds(100), false, exception);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordNotificationPublish_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordNotificationPublish(typeof(string), 5, TimeSpan.FromMilliseconds(100), false, exception);
        Assert.True(true);
    }

    [Fact]
    public void PooledTelemetryProvider_RecordStreamingOperation_Should_HandleException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();
        var contextPool = provider.GetRequiredService<ITelemetryContextPool>();
        var telemetryProvider = new PooledTelemetryProvider(contextPool);
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        telemetryProvider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", TimeSpan.FromMilliseconds(100), 50, false, exception);
        Assert.True(true);
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

        // Clear context using reflection
        var contextField = typeof(PooledTelemetryProvider).GetField("CorrelationIdContext", BindingFlags.Static | BindingFlags.NonPublic);
        var asyncLocal = (AsyncLocal<string?>)contextField!.GetValue(null)!;
        asyncLocal.Value = null;

        // Get ActivitySource using reflection
        var activitySourceField = typeof(PooledTelemetryProvider).GetField("ActivitySource", BindingFlags.Static | BindingFlags.NonPublic);
        var activitySource = (ActivitySource)activitySourceField!.GetValue(null)!;

        // Create activity with tag
        using var activity = activitySource.StartActivity("Test");
        activity?.SetTag("relay.correlation_id", correlationId);

        // Act
        var result = telemetryProvider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, result);
    }

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
    public void PooledTelemetryProvider_Should_LogDebugMessages()
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

        // Act - Start activity to trigger logging
        var activity = telemetryProvider.StartActivity("TestOperation", typeof(string), "test-id");

        // Record execution to trigger logging
        telemetryProvider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true);

        // Assert - Test passes if no exceptions (logging is tested indirectly)
        Assert.NotNull(activity);
    }

    [Fact]
    public void TelemetryContext_Properties_Should_BeModifiable()
    {
        // Arrange
        var context = TelemetryContextPool.Get();

        // Act
        context.Properties["key1"] = "value1";
        context.Properties["key2"] = 42;

        // Assert
        Assert.Equal("value1", context.Properties["key1"]);
        Assert.Equal(42, context.Properties["key2"]);

        // Cleanup
        TelemetryContextPool.Return(context);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Should_HandleNegativeLength()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act
        var slice = span.SafeSlice(5, -1);

        // Assert
        Assert.True(slice.IsEmpty);
    }

    [Fact]
    public void PooledBufferManager_Multiple_Returns_Should_NotThrow()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();
        var buffer = bufferManager.RentBuffer(1024);

        // Act & Assert - Should not throw
        bufferManager.ReturnBuffer(buffer);
        bufferManager.ReturnBuffer(buffer); // Return same buffer twice
        Assert.True(true);
    }
}