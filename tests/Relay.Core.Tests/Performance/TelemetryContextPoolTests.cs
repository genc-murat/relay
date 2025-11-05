using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;
using Relay.Core.Performance.Telemetry;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Performance.Extensions;

namespace Relay.Core.Tests.Performance;
using Relay.Core.Testing;

public class TelemetryContextPoolTests
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
}
