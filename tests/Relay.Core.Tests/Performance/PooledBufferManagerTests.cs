using Relay.Core.Performance.BufferManagement;
using Relay.Core.Performance.Extensions;
using System;
using Xunit;

namespace Relay.Core.Tests.Performance;

public class PooledBufferManagerTests
{
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
    public void SpanExtensions_SafeSlice_Should_HandleNegativeLength()
    {
        // Arrange
        var span = new byte[10].AsSpan();

        // Act
        var slice = span.SafeSlice(5, -1);

        // Assert
        Assert.True(slice.IsEmpty);
    }
}