using System;
using Xunit;
using Relay.Core.Performance;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Simple test to verify object pooling functionality works
/// </summary>
public class SimplePoolingTest
{
    [Fact]
    public void TelemetryContextPool_BasicFunctionality_Works()
    {
        // Arrange & Act
        var context1 = TelemetryContextPool.Get();
        var originalId = context1.RequestId;
        
        TelemetryContextPool.Return(context1);
        var context2 = TelemetryContextPool.Get();
        
        // Assert
        Assert.Same(context1, context2); // Should be the same instance (pooled)
        Assert.NotEqual(originalId, context2.RequestId); // Should have new ID
        
        // Cleanup
        TelemetryContextPool.Return(context2);
    }

    [Fact]
    public void BufferManager_BasicFunctionality_Works()
    {
        // Arrange
        var bufferManager = new DefaultPooledBufferManager();
        
        // Act
        var buffer1 = bufferManager.RentBuffer(1024);
        bufferManager.ReturnBuffer(buffer1);
        var buffer2 = bufferManager.RentBuffer(1024);
        
        // Assert
        Assert.Same(buffer1, buffer2); // Should be the same instance (pooled)
        
        // Cleanup
        bufferManager.ReturnBuffer(buffer2);
    }

    [Fact]
    public void SpanExtensions_SafeSlice_Works()
    {
        // Arrange
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var span = data.AsSpan();
        
        // Act
        var slice = span.SafeSlice(1, 3);
        
        // Assert
        Assert.Equal(3, slice.Length);
        Assert.Equal(2, slice[0]);
        Assert.Equal(3, slice[1]);
        Assert.Equal(4, slice[2]);
    }
}