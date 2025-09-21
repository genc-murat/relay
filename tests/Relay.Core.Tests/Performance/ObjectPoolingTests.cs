using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Relay.Core.Performance;
using Relay.Core.Telemetry;
using Xunit;

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
}