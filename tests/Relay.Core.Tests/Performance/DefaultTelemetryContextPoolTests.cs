using Microsoft.Extensions.ObjectPool;
using Relay.Core.Performance.Telemetry;
using Relay.Core.Telemetry;
using System;
using Xunit;

namespace Relay.Core.Tests.Performance;

public class DefaultTelemetryContextPoolTests
{
    [Fact]
    public void DefaultTelemetryContextPool_Constructor_Should_Throw_When_ObjectPoolProvider_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultTelemetryContextPool(null!));
    }

    [Fact]
    public void DefaultTelemetryContextPool_Get_Should_Return_Valid_TelemetryContext()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);

        // Act
        var context = pool.Get();

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.RequestId);
        Assert.NotEqual(default, context.StartTime);
        Assert.True(context.StartTime > DateTimeOffset.MinValue);

        // Cleanup
        pool.Return(context);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Get_Should_Generate_New_RequestId_Each_Time()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);

        // Act
        var context1 = pool.Get();
        var context2 = pool.Get();

        // Assert
        Assert.NotEqual(context1.RequestId, context2.RequestId);

        // Cleanup
        pool.Return(context1);
        pool.Return(context2);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Get_Should_Set_Fresh_StartTime()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);
        var before = DateTimeOffset.UtcNow;

        // Act
        var context = pool.Get();

        // Assert
        Assert.True(context.StartTime >= before);
        Assert.True(context.StartTime <= DateTimeOffset.UtcNow);

        // Cleanup
        pool.Return(context);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Return_Should_Handle_Null_Context()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);

        // Act & Assert - Should not throw
        pool.Return(null!);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Return_Should_Handle_Valid_Context()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);
        var context = pool.Get();

        // Act & Assert - Should not throw
        pool.Return(context);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Should_Reuse_Contexts()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);

        // Act
        var context1 = pool.Get();
        var originalId = context1.RequestId;
        pool.Return(context1);
        var context2 = pool.Get();

        // Assert
        Assert.Same(context1, context2);
        Assert.NotEqual(originalId, context2.RequestId); // Should have new ID

        // Cleanup
        pool.Return(context2);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Should_Reset_Context_State_On_Get()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);

        // Get and modify context
        var context = pool.Get();
        context.CorrelationId = "test-correlation";
        context.HandlerName = "TestHandler";
        context.RequestType = typeof(string);
        context.ResponseType = typeof(int);
        context.Properties["key"] = "value";

        // Return and get again
        pool.Return(context);
        var newContext = pool.Get();

        // Assert - should be same object but reset
        Assert.Same(context, newContext);
        Assert.Null(newContext.CorrelationId);
        Assert.Null(newContext.HandlerName);
        Assert.Null(newContext.RequestType);
        Assert.Null(newContext.ResponseType);
        Assert.Empty(newContext.Properties);
        Assert.NotNull(newContext.RequestId); // Should have new ID

        // Cleanup
        pool.Return(newContext);
    }

    [Fact]
    public void DefaultTelemetryContextPool_Should_Be_Thread_Safe()
    {
        // Arrange
        var provider = new DefaultObjectPoolProvider();
        var pool = new DefaultTelemetryContextPool(provider);
        var contexts = new System.Collections.Concurrent.ConcurrentBag<TelemetryContext>();

        // Act - Run multiple threads getting and returning contexts
        var tasks = new System.Threading.Tasks.Task[10];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var context = pool.Get();
                    contexts.Add(context);
                    pool.Return(context);
                }
            });
        }

        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert - Should have created contexts without issues
        Assert.Equal(1000, contexts.Count);

        // Verify all contexts are valid
        foreach (var context in contexts)
        {
            Assert.NotNull(context.RequestId);
        }
    }
}