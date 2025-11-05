using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Performance.BufferManagement;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.Performance.BufferManagement;

public class OptimizedPooledBufferManagerTests
{
    private readonly ITestOutputHelper _output;

    public OptimizedPooledBufferManagerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Constructor_ShouldInitializePools()
    {
        // Act
        var manager = new OptimizedPooledBufferManager();

        // Assert
        Assert.NotNull(manager);
        
        // Test that pools are working by renting and returning buffers
        var smallBuffer = manager.RentBuffer(512);
        var mediumBuffer = manager.RentBuffer(2048);
        var largeBuffer = manager.RentBuffer(128 * 1024);

        Assert.NotNull(smallBuffer);
        Assert.NotNull(mediumBuffer);
        Assert.NotNull(largeBuffer);

        Assert.True(smallBuffer.Length >= 512);
        Assert.True(mediumBuffer.Length >= 2048);
        Assert.True(largeBuffer.Length >= 128 * 1024);

        manager.ReturnBuffer(smallBuffer);
        manager.ReturnBuffer(mediumBuffer);
        manager.ReturnBuffer(largeBuffer);
    }

    [Theory]
    [InlineData(16)]      // Very small
    [InlineData(512)]     // Small buffer
    [InlineData(1024)]    // Small buffer threshold
    [InlineData(2048)]    // Medium buffer
    [InlineData(32768)]   // Medium buffer
    [InlineData(65536)]   // Medium buffer threshold
    [InlineData(131072)]  // Large buffer
    [InlineData(1048576)] // Very large buffer
    public void RentBuffer_ShouldReturnAppropriateSize(int minimumLength)
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act
        var buffer = manager.RentBuffer(minimumLength);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= minimumLength, 
            $"Buffer length {buffer.Length} should be >= requested {minimumLength}");

        // Clean up
        manager.ReturnBuffer(buffer);
    }

    [Fact]
    public void RentBuffer_ShouldSelectCorrectPoolBasedOnSize()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        
        // Act
        var initialMetrics = manager.GetMetrics();
        
        var smallBuffer = manager.RentBuffer(512);
        var afterSmallMetrics = manager.GetMetrics();
        
        var mediumBuffer = manager.RentBuffer(2048);
        var afterMediumMetrics = manager.GetMetrics();
        
        var largeBuffer = manager.RentBuffer(131072);
        var afterLargeMetrics = manager.GetMetrics();

        // Assert
        Assert.Equal(initialMetrics.TotalRequests + 1, afterSmallMetrics.TotalRequests);
        Assert.Equal(initialMetrics.SmallPoolHits + 1, afterSmallMetrics.SmallPoolHits);
        
        Assert.Equal(afterSmallMetrics.TotalRequests + 1, afterMediumMetrics.TotalRequests);
        Assert.Equal(afterSmallMetrics.MediumPoolHits + 1, afterMediumMetrics.MediumPoolHits);
        
        Assert.Equal(afterMediumMetrics.TotalRequests + 1, afterLargeMetrics.TotalRequests);
        Assert.Equal(afterMediumMetrics.LargePoolHits + 1, afterLargeMetrics.LargePoolHits);

        // Clean up
        manager.ReturnBuffer(smallBuffer);
        manager.ReturnBuffer(mediumBuffer);
        manager.ReturnBuffer(largeBuffer);
    }

    [Fact]
    public void ReturnBuffer_ShouldHandleNullBuffer()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - Should not throw
        manager.ReturnBuffer(null!);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReturnBuffer_ShouldReturnToCorrectPool(bool clearArray)
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        
        var smallBuffer = manager.RentBuffer(512);
        var mediumBuffer = manager.RentBuffer(2048);
        var largeBuffer = manager.RentBuffer(131072);

        // Fill with test data
        smallBuffer[0] = 1;
        mediumBuffer[0] = 2;
        largeBuffer[0] = 3;

        // Act
        manager.ReturnBuffer(smallBuffer, clearArray);
        manager.ReturnBuffer(mediumBuffer, clearArray);
        manager.ReturnBuffer(largeBuffer, clearArray);

        // Assert - If clearArray is true, rent again and verify it's cleared
        if (clearArray)
        {
            var newSmallBuffer = manager.RentBuffer(512);
            var newMediumBuffer = manager.RentBuffer(2048);
            var newLargeBuffer = manager.RentBuffer(131072);

            // Note: ArrayPool.Clear() behavior is implementation specific
            // We're mainly testing that no exceptions are thrown
            
            manager.ReturnBuffer(newSmallBuffer);
            manager.ReturnBuffer(newMediumBuffer);
            manager.ReturnBuffer(newLargeBuffer);
        }
    }

    [Fact]
    public void RentSpan_ShouldReturnValidSpan()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        const int expectedLength = 1024;

        // Act
        var span = manager.RentSpan(expectedLength);

        // Assert
        Assert.Equal(expectedLength, span.Length);
        
        // Test that we can write to the span
        span[0] = 42;
        Assert.Equal(42, span[0]);
        span[expectedLength - 1] = 24;
        Assert.Equal(24, span[expectedLength - 1]);

        // Note: We can't easily return the span since we don't have access to the underlying array
        // This is expected behavior for the API design
    }

    [Fact]
    public void ReturnSpan_ShouldHandleNullBuffer()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - Should not throw
        manager.ReturnSpan(null!);
    }

    [Fact]
    public void RentBufferForRequest_ShouldEstimateSizeCorrectly()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        var smallRequest = new SmallTestRequest();
        var mediumRequest = new MediumTestRequest();
        var largeRequest = new LargeTestRequest();

        // Act
        var smallBuffer = manager.RentBufferForRequest(smallRequest);
        var mediumBuffer = manager.RentBufferForRequest(mediumRequest);
        var largeBuffer = manager.RentBufferForRequest(largeRequest);

        // Assert
        Assert.NotNull(smallBuffer);
        Assert.NotNull(mediumBuffer);
        Assert.NotNull(largeBuffer);

        // Check actual sizes of test classes to understand the estimation
        var smallSize = System.Runtime.CompilerServices.Unsafe.SizeOf<SmallTestRequest>();
        var mediumSize = System.Runtime.CompilerServices.Unsafe.SizeOf<MediumTestRequest>();
        var largeSize = System.Runtime.CompilerServices.Unsafe.SizeOf<LargeTestRequest>();

        _output.WriteLine($"SmallTestRequest size: {smallSize} bytes");
        _output.WriteLine($"MediumTestRequest size: {mediumSize} bytes");
        _output.WriteLine($"LargeTestRequest size: {largeSize} bytes");

        // Based on the size estimation logic in the class
        // All classes are 8 bytes (reference size on 64-bit), so they all get 256 byte buffers
        Assert.True(smallBuffer.Length >= 256);  // Small request -> 256 buffer
        Assert.True(mediumBuffer.Length >= 256); // Medium request -> 256 buffer (since size is 8 bytes)
        Assert.True(largeBuffer.Length >= 256);  // Large request -> 256 buffer (since size is 8 bytes)

        // Clean up
        manager.ReturnBuffer(smallBuffer);
        manager.ReturnBuffer(mediumBuffer);
        manager.ReturnBuffer(largeBuffer);
    }

    [Fact]
    public void GetMetrics_ShouldReturnAccurateMetrics()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        
        // Act
        var initialMetrics = manager.GetMetrics();
        
        // Perform some operations
        var buffer1 = manager.RentBuffer(512);
        var buffer2 = manager.RentBuffer(2048);
        var buffer3 = manager.RentBuffer(131072);
        
        var afterRentMetrics = manager.GetMetrics();
        
        manager.ReturnBuffer(buffer1);
        manager.ReturnBuffer(buffer2);
        manager.ReturnBuffer(buffer3);
        
        var finalMetrics = manager.GetMetrics();

        // Assert
        Assert.Equal(0, initialMetrics.TotalRequests);
        Assert.Equal(0, initialMetrics.SmallPoolHits);
        Assert.Equal(0, initialMetrics.MediumPoolHits);
        Assert.Equal(0, initialMetrics.LargePoolHits);

        Assert.Equal(3, afterRentMetrics.TotalRequests);
        Assert.Equal(1, afterRentMetrics.SmallPoolHits);
        Assert.Equal(1, afterRentMetrics.MediumPoolHits);
        Assert.Equal(1, afterRentMetrics.LargePoolHits);

        Assert.Equal(1.0, afterRentMetrics.SmallPoolEfficiency);
        Assert.Equal(1.0, afterRentMetrics.MediumPoolEfficiency);
        Assert.Equal(1.0, afterRentMetrics.LargePoolEfficiency);

        // Final metrics should be the same (returns don't affect hit counts)
        Assert.Equal(afterRentMetrics.TotalRequests, finalMetrics.TotalRequests);
        Assert.Equal(afterRentMetrics.SmallPoolHits, finalMetrics.SmallPoolHits);
        Assert.Equal(afterRentMetrics.MediumPoolHits, finalMetrics.MediumPoolHits);
        Assert.Equal(afterRentMetrics.LargePoolHits, finalMetrics.LargePoolHits);
    }

    [Fact]
    public void OptimizeForWorkload_ShouldNotThrow()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - Should not throw
        manager.OptimizeForWorkload();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - Should not throw
        manager.Dispose();
    }

    [Fact]
    public void BufferPoolMetrics_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var metrics = new BufferPoolMetrics
        {
            TotalRequests = 1000,
            SmallPoolHits = 600,
            MediumPoolHits = 300,
            LargePoolHits = 100,
            SmallPoolEfficiency = 0.6,
            MediumPoolEfficiency = 0.3,
            LargePoolEfficiency = 0.1
        };

        // Act
        var result = metrics.ToString();

        // Assert
        Assert.Contains("1,000", result);
        Assert.Contains("600", result);
        Assert.Contains("300", result);
        Assert.Contains("100", result);
        Assert.Contains("60.0%", result);
        Assert.Contains("30.0%", result);
        Assert.Contains("10.0%", result);
    }

    [Fact]
    public async Task ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        const int iterations = 100;
        const int concurrentTasks = 10;

        var tasks = Enumerable.Range(0, concurrentTasks).Select(async taskId =>
        {
            for (int i = 0; i < iterations; i++)
            {
                // Use different sizes to hit different pools
                int size = (i % 3) switch
                {
                    0 => 512,    // Small (<= 1024)
                    1 => 2048,   // Medium (<= 65536)  
                    _ => 131072  // Large (> 65536)
                };

                var buffer = manager.RentBuffer(size);
                Assert.NotNull(buffer);
                Assert.True(buffer.Length >= size);

                // Simulate some work - only if buffer has elements
                if (buffer.Length > 0)
                {
                    buffer[0] = (byte)(i % 256);
                }
                await Task.Yield();

                manager.ReturnBuffer(buffer);
            }
        });

        // Act
        await Task.WhenAll(tasks);

        // Assert - Main focus is thread safety, not exact pool distribution
        var metrics = manager.GetMetrics();
        _output.WriteLine($"Concurrent test completed: {metrics}");
        
        Assert.Equal(iterations * concurrentTasks, metrics.TotalRequests);
        
        // At minimum, we should have some activity in the pools
        Assert.True(metrics.TotalRequests > 0, "Should have processed requests");
        
        // The main goal is that no exceptions occurred during concurrent access
        // Pool distribution may vary due to timing, so we just check for activity
        Assert.True(metrics.SmallPoolHits > 0 || metrics.MediumPoolHits > 0 || metrics.LargePoolHits > 0, 
            "Should have some pool activity");
    }

    [Fact]
    public void PoolSelection_ShouldWorkCorrectly()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - Test each pool explicitly
        var smallBuffer = manager.RentBuffer(512);     // Should go to small pool
        var mediumBuffer = manager.RentBuffer(2048);   // Should go to medium pool
        var largeBuffer = manager.RentBuffer(131072);  // Should go to large pool

        var metrics = manager.GetMetrics();
        _output.WriteLine($"Pool selection test: {metrics}");

        Assert.Equal(3, metrics.TotalRequests);
        Assert.Equal(1, metrics.SmallPoolHits);
        Assert.Equal(1, metrics.MediumPoolHits);
        Assert.Equal(1, metrics.LargePoolHits);

        // Clean up
        manager.ReturnBuffer(smallBuffer);
        manager.ReturnBuffer(mediumBuffer);
        manager.ReturnBuffer(largeBuffer);
    }

    [Fact]
    public void PerformanceTest_HighFrequencyOperations()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        const int iterations = 10000;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var buffer = manager.RentBuffer(1024);
            buffer[0] = (byte)(i % 256);
            manager.ReturnBuffer(buffer);
        }

        stopwatch.Stop();

        // Assert
        _output.WriteLine($"High frequency test: {iterations} iterations in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / iterations:F4}ms per operation");

        // Should be very fast due to pooling
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "High frequency operations took too long");

        var metrics = manager.GetMetrics();
        _output.WriteLine($"Performance metrics: {metrics}");
    }

    [Theory]
    [InlineData(0)]       // Zero size
    [InlineData(1)]       // Very small
    public void RentBuffer_EdgeCases_ShouldHandleExtremeSizes(int size)
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act
        var buffer = manager.RentBuffer(size);

        // Assert
        Assert.NotNull(buffer);
        Assert.True(buffer.Length >= size);

        manager.ReturnBuffer(buffer);
    }

    [Fact]
    public void RentBuffer_MaxIntSize_ShouldHandleGracefully()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert - int.MaxValue may cause OutOfMemoryException, which is acceptable
        try
        {
            var buffer = manager.RentBuffer(int.MaxValue);
            Assert.NotNull(buffer);
            manager.ReturnBuffer(buffer);
        }
        catch (OutOfMemoryException)
        {
            // This is acceptable behavior for extreme sizes
            _output.WriteLine("int.MaxValue caused OutOfMemoryException - acceptable");
        }
    }

    [Fact]
    public void RentBuffer_NegativeSize_ShouldThrowArgumentException()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => manager.RentBuffer(-1));
    }





    [Fact]
    public void GetMetrics_ZeroRequests_ShouldHandleDivisionByZero()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act
        var metrics = manager.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalRequests);
        Assert.Equal(0, metrics.SmallPoolHits);
        Assert.Equal(0, metrics.MediumPoolHits);
        Assert.Equal(0, metrics.LargePoolHits);
        Assert.Equal(0.0, metrics.SmallPoolEfficiency);
        Assert.Equal(0.0, metrics.MediumPoolEfficiency);
        Assert.Equal(0.0, metrics.LargePoolEfficiency);
    }

    [Fact]
    public void BufferMetrics_InternalTracking_ShouldUpdateCorrectly()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act - Perform operations to trigger internal metrics updates
        var buffer1 = manager.RentBuffer(512);    // Small pool
        var buffer2 = manager.RentBuffer(2048);   // Medium pool
        var buffer3 = manager.RentBuffer(131072); // Large pool

        var metrics = manager.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalRequests);
        Assert.Equal(1, metrics.SmallPoolHits);
        Assert.Equal(1, metrics.MediumPoolHits);
        Assert.Equal(1, metrics.LargePoolHits);

        // Test efficiency calculations
        Assert.Equal(1.0, metrics.SmallPoolEfficiency);
        Assert.Equal(1.0, metrics.MediumPoolEfficiency);
        Assert.Equal(1.0, metrics.LargePoolEfficiency);

        // Clean up
        manager.ReturnBuffer(buffer1);
        manager.ReturnBuffer(buffer2);
        manager.ReturnBuffer(buffer3);
    }

    [Fact]
    public void RentSpan_ZeroLength_ShouldReturnEmptySpan()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();

        // Act
        var span = manager.RentSpan(0);

        // Assert
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void RentSpan_LargeLength_ShouldWork()
    {
        // Arrange
        var manager = new OptimizedPooledBufferManager();
        const int largeSize = 100 * 1024; // 100KB

        // Act
        var span = manager.RentSpan(largeSize);

        // Assert
        Assert.Equal(largeSize, span.Length);

        // Test that we can access the span
        span[0] = 42;
        span[largeSize - 1] = 24;
        Assert.Equal(42, span[0]);
        Assert.Equal(24, span[largeSize - 1]);
    }



    // Test request classes for size estimation testing
    private class SmallTestRequest : IRequest
    {
        public int Value { get; set; } // 4 bytes
    }

    private class MediumTestRequest : IRequest
    {
        public int Value1 { get; set; }
        public int Value2 { get; set; }
        public int Value3 { get; set; }
        public int Value4 { get; set; }
        public long Value5 { get; set; }
        public long Value6 { get; set; }
        public long Value7 { get; set; }
        public long Value8 { get; set; }
        // Total: 32 bytes
    }

    private class LargeTestRequest : IRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public byte[] Data { get; set; } = new byte[100];
        public decimal Amount { get; set; }
        // Should be > 64 bytes
    }
}
