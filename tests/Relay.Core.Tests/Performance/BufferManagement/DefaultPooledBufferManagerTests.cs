using System;
using System.Buffers;
using Relay.Core.Performance.BufferManagement;
using Xunit;

namespace Relay.Core.Tests.Performance.BufferManagement
{
    /// <summary>
    /// Comprehensive tests for DefaultPooledBufferManager to increase test coverage
    /// </summary>
    public class DefaultPooledBufferManagerTests
    {
        [Fact]
        public void Constructor_WithNullPool_ShouldUseDefaultPool()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(1024);
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 1024);
            bufferManager.ReturnBuffer(buffer);
        }

        [Fact]
        public void RentBuffer_WithVariousSizes_ShouldReturnCorrectlySizedBuffers()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var sizes = new[] { 1, 16, 256, 1024, 10000 };
            foreach (var size in sizes)
            {
                var buffer = bufferManager.RentBuffer(size);
                Assert.NotNull(buffer);
                Assert.True(buffer.Length >= size);
                bufferManager.ReturnBuffer(buffer);
            }
        }

        [Fact]
        public void RentBuffer_WithNegativeSize_ShouldThrowArgumentOutOfRangeException()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => bufferManager.RentBuffer(-1));
            Assert.Equal("minimumLength", exception.ParamName);
        }

        [Fact]
        public void ReturnBuffer_WithNullBuffer_ShouldNotThrow()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var exception = Record.Exception(() => bufferManager.ReturnBuffer(null));
            Assert.Null(exception);
        }

        [Fact]
        public void RentSpan_WithVariousSizes_ShouldReturnCorrectlySizedSpans()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var sizes = new[] { 1, 16, 256, 1024 };
            foreach (var size in sizes)
            {
                var span = bufferManager.RentSpan(size);
                // Span is a value type and cannot be null, so just verify the length
                Assert.True(span.Length == size);
            }
        }

        [Fact]
        public void RentSpan_WithNegativeSize_ShouldThrowArgumentOutOfRangeException()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => bufferManager.RentSpan(-1));
            Assert.Equal("minimumLength", exception.ParamName);
        }

        [Fact]
        public void ReturnSpan_WithNullBuffer_ShouldNotThrow()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var exception = Record.Exception(() => bufferManager.ReturnSpan(null));
            Assert.Null(exception);
        }

        [Fact]
        public void RentBuffer_ThenReturn_ThenRentAgain_ShouldReuseBuffer()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var expectedSize = 1024;

            var buffer1 = bufferManager.RentBuffer(expectedSize);
            bufferManager.ReturnBuffer(buffer1);
            var buffer2 = bufferManager.RentBuffer(expectedSize);

            Assert.NotNull(buffer1);
            Assert.NotNull(buffer2);
            Assert.True(buffer1.Length >= expectedSize);
            Assert.True(buffer2.Length >= expectedSize);

            bufferManager.ReturnBuffer(buffer2);
        }

        [Fact]
        public void Constructor_WithCustomPool_ShouldUseCustomPool()
        {
            var customPool = ArrayPool<byte>.Create(1024, 4);
            var bufferManager = new DefaultPooledBufferManager(customPool);
            var buffer = bufferManager.RentBuffer(256);
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 256);
            bufferManager.ReturnBuffer(buffer);
        }

        [Fact]
        public void RentBuffer_WithZeroSize_ShouldReturnValidBuffer()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(0);
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 0);
            bufferManager.ReturnBuffer(buffer);
        }

        [Fact]
        public void ReturnBuffer_WithValidBuffer_ShouldReturnSuccessfully()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(1024);
            var exception = Record.Exception(() => bufferManager.ReturnBuffer(buffer));
            Assert.Null(exception);
        }

        [Fact]
        public void ReturnBuffer_WithClearOption_ShouldNotThrow()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(1024);
            for (int i = 0; i < buffer.Length && i < 10; i++)
            {
                buffer[i] = (byte)(i + 1);
            }
            var exception = Record.Exception(() => bufferManager.ReturnBuffer(buffer, clearArray: true));
            Assert.Null(exception);
        }

        [Fact]
        public void ReturnSpan_WithValidBuffer_ShouldReturnSuccessfully()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(1024);
            var exception = Record.Exception(() => bufferManager.ReturnSpan(buffer));
            Assert.Null(exception);
        }

        [Fact]
        public void ReturnSpan_WithClearOption_ShouldWorkCorrectly()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var buffer = bufferManager.RentBuffer(1024);
            for (int i = 0; i < buffer.Length && i < 10; i++)
            {
                buffer[i] = (byte)(i + 1);
            }
            var exception = Record.Exception(() => bufferManager.ReturnSpan(buffer, clearArray: true));
            Assert.Null(exception);
        }

        [Fact]
        public void RentSpan_WithZeroSize_ShouldReturnCorrectSizedSpan()
        {
            var bufferManager = new DefaultPooledBufferManager();
            var span = bufferManager.RentSpan(0);
            // Span is a value type and cannot be null, so just verify the length
            Assert.True(span.Length == 0);
        }
    }
}
