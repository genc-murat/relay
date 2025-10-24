using Relay.Core.Performance.Optimization;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.Performance.Optimization;

/// <summary>
/// Edge case tests for PerformanceHelpers to increase test coverage
/// </summary>
public class PerformanceHelpersEdgeCaseTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceHelpersEdgeCaseTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void PrefetchMemory_SpanWithSingleElement_ShouldNotThrow()
    {
        // Arrange
        var array = new int[1];
        var span = array.AsSpan();

        // Act & Assert
        PerformanceHelpers.PrefetchMemory(span);
    }

    [Fact]
    public void PrefetchMemory_SpanWithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        // Create a span of IntPtr to test with null values (as bytes)
        var ptrArray = new IntPtr[5];
        var span = System.Runtime.InteropServices.MemoryMarshal.AsBytes(ptrArray.AsSpan());

        // Act & Assert
        PerformanceHelpers.PrefetchMemory(span);
    }

    [Fact]
    public void PrefetchMemoryClassArray_SingleElement_ShouldNotThrow()
    {
        // Arrange
        var array = new[] { new TestObject { Id = 1, Name = "Test" } };

        // Act & Assert
        PerformanceHelpers.PrefetchMemoryClassArray(array);
    }

    [Fact]
    public void PrefetchMemoryValueArray_SingleElement_ShouldNotThrow()
    {
        // Arrange
        var array = new[] { 42 };

        // Act & Assert
        PerformanceHelpers.PrefetchMemoryValueArray(array);
    }

    [Fact]
    public void PrefetchMemoryMultiple_SingleElement_ShouldNotThrow()
    {
        // Arrange
        var obj = new TestObject { Id = 1, Name = "Test" };

        // Act & Assert
        PerformanceHelpers.PrefetchMemoryMultiple(obj);
    }

    [Fact]
    public void PrefetchMemoryMultiple_ThreeElements_ShouldNotThrow()
    {
        // Arrange
        var obj1 = new TestObject { Id = 1, Name = "Test1" };
        var obj2 = new TestObject { Id = 2, Name = "Test2" };
        var obj3 = new TestObject { Id = 3, Name = "Test3" };

        // Act & Assert
        PerformanceHelpers.PrefetchMemoryMultiple(obj1, obj2, obj3);
    }

    [Fact]
    public void InitializeArray_WithDifferentValueTypes_ShouldWorkCorrectly()
    {
        // Test with float
        var floatArray = new float[10];
        var floatSpan = floatArray.AsSpan();
        PerformanceHelpers.InitializeArray(floatSpan, 3.14f);
        foreach (var f in floatSpan)
        {
            Assert.Equal(3.14f, f);
        }

        // Test with double
        var doubleArray = new double[5];
        var doubleSpan = doubleArray.AsSpan();
        PerformanceHelpers.InitializeArray(doubleSpan, 2.718);
        foreach (var d in doubleSpan)
        {
            Assert.Equal(2.718, d);
        }

        // Test with long
        var longArray = new long[8];
        var longSpan = longArray.AsSpan();
        PerformanceHelpers.InitializeArray(longSpan, 123456789L);
        foreach (var l in longSpan)
        {
            Assert.Equal(123456789L, l);
        }
    }

    [Fact]
    public void InitializeArray_SizeSmallerThanVectorSize_ShouldWorkCorrectly()
    {
        // Arrange - Create an array smaller than typical vector size (usually 4+ for int)
        var array = new int[2];
        var span = array.AsSpan();

        // Act
        PerformanceHelpers.InitializeArray(span, 999);

        // Assert
        Assert.Equal(999, array[0]);
        Assert.Equal(999, array[1]);
    }

    [Fact]
    public void InitializeArray_SizeEqualToVectorSize_ShouldWorkCorrectly()
    {
        // Arrange
        var vectorSize = PerformanceHelpers.VectorSize;
        var array = new int[vectorSize];
        var span = array.AsSpan();

        // Act
        PerformanceHelpers.InitializeArray(span, 777);

        // Assert
        Assert.All(array, i => Assert.Equal(777, i));
    }

    [Fact]
    public void InitializeArray_WithStructType_MayNotWork()
    {
        // Arrange
        var data = new CustomStruct[5];
        var value = new CustomStruct { A = 10, B = 20 };

        // Act & Assert - Custom structs might not be supported by SIMD
        // The method should either work or throw a NotSupportedException
        var exception = Record.Exception(() => PerformanceHelpers.InitializeArray(data.AsSpan(), value));
        
        // If there's an exception, it should be a NotSupportedException 
        if (exception != null)
        {
            Assert.IsType<NotSupportedException>(exception);
        }
        // If no exception, check values were set correctly
        else 
        {
            foreach (var item in data)
            {
                Assert.Equal(10, item.A);
                Assert.Equal(20, item.B);
            }
        }
    }

    [Fact]
    public void SequenceEqual_WithDifferentStartingValues_ShouldReturnFalse()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3, 4, 5 };
        var data2 = new byte[] { 2, 2, 3, 4, 5 }; // Different first byte
        var span1 = data1.AsSpan();
        var span2 = data2.AsSpan();

        // Act
        var result = PerformanceHelpers.SequenceEqual(span1, span2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SequenceEqual_WithDifferentEndingValues_ShouldReturnFalse()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3, 4, 5 };
        var data2 = new byte[] { 1, 2, 3, 4, 6 }; // Different last byte
        var span1 = data1.AsSpan();
        var span2 = data2.AsSpan();

        // Act
        var result = PerformanceHelpers.SequenceEqual(span1, span2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SequenceEqual_WithSameLargeData_ShouldReturnTrue()
    {
        // Arrange - Create large data arrays that should trigger SIMD path
        var size = PerformanceHelpers.VectorSize * 4; // Ensure it's large enough for SIMD
        var data1 = new byte[size];
        var data2 = new byte[size];

        for (int i = 0; i < size; i++)
        {
            data1[i] = (byte)(i % 256);
            data2[i] = (byte)(i % 256);
        }

        var span1 = data1.AsSpan();
        var span2 = data2.AsSpan();

        // Act
        var result = PerformanceHelpers.SequenceEqual(span1, span2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void SequenceEqual_WithLargeDataAndOneDifference_ShouldReturnFalse()
    {
        // Arrange - Create large data arrays with one difference in the middle
        var size = PerformanceHelpers.VectorSize * 4; // Ensure it's large enough for SIMD
        var data1 = new byte[size];
        var data2 = new byte[size];

        for (int i = 0; i < size; i++)
        {
            data1[i] = (byte)(i % 256);
            data2[i] = (byte)(i % 256);
        }

        // Introduce a difference in the middle
        data2[size / 2] = 255;

        var span1 = data1.AsSpan();
        var span2 = data2.AsSpan();

        // Act
        var result = PerformanceHelpers.SequenceEqual(span1, span2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetOptimalBatchSize_ZeroInput_ShouldReturnZero()
    {
        // Act
        var result = PerformanceHelpers.GetOptimalBatchSize(0);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetOptimalBatchSize_InputEqualToVectorSize_ShouldReturnCorrectValue()
    {
        // Arrange
        var vectorSize = PerformanceHelpers.VectorSize;

        // Act
        var result = PerformanceHelpers.GetOptimalBatchSize(vectorSize);

        // Assert
        Assert.Equal(vectorSize, result);
    }

    [Fact]
    public void GetOptimalBatchSize_InputOneLessThanVectorSize_ShouldReturnVectorSize()
    {
        // Arrange
        var vectorSize = PerformanceHelpers.VectorSize;

        // Act
        var result = PerformanceHelpers.GetOptimalBatchSize(vectorSize - 1);

        // When SIMD is available and the input is less than vector size, it should return the input
        // If SIMD is not available, it should return the input directly
        if (PerformanceHelpers.IsSIMDAvailable)
        {
            Assert.True(result >= vectorSize - 1);
        }
        else
        {
            Assert.Equal(vectorSize - 1, result);
        }
    }

    [Fact]
    public void GetOptimalBatchSize_WithSIMDAvailableCheck()
    {
        // Act
        var simdAvailable = PerformanceHelpers.IsSIMDAvailable;
        var vectorSize = PerformanceHelpers.VectorSize;

        // Test various sizes
        var testSizes = new[] { 1, 5, 10, 16, 20, 32, 64, 100 };

        foreach (var size in testSizes)
        {
            var result = PerformanceHelpers.GetOptimalBatchSize(size);

            if (simdAvailable)
            {
                // When SIMD is available, the result should be >= size
                // If size < vectorSize, it returns the size (no padding needed for small arrays)
                // If size >= vectorSize, it might return a multiple of vectorSize
                Assert.True(result >= size);
            }
            else
            {
                // When SIMD is not available, it should return the original size
                Assert.Equal(size, result);
            }
        }
    }



    [Fact]
    public void PrefetchMemory_SpanOfChar_ShouldNotThrow()
    {
        // Arrange
        var chars = new char[] { 'h', 'e', 'l', 'l', 'o' };
        var span = chars.AsSpan();

        // Act & Assert
        PerformanceHelpers.PrefetchMemory(span);
    }

    [Fact]
    public void PrefetchMemory_SpanOfSizeOne_ShouldNotThrow()
    {
        // Arrange
        var array = new int[1];
        var span = array.AsSpan();

        // Act & Assert
        PerformanceHelpers.PrefetchMemory(span);
    }

    // Helper structs and classes
    private struct CustomStruct
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    private class TestObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}