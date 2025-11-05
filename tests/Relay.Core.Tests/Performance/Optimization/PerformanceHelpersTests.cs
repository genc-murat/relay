using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Relay.Core.Performance.Optimization;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.Performance.Optimization
{
    public class PerformanceHelpersTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceHelpersTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void IsSIMDAvailable_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsSIMDAvailable;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"SIMD Available: {result}");
        }

        [Fact]
        public void IsAVX2Available_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsAVX2Available;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"AVX2 Available: {result}");
        }

        [Fact]
        public void IsSSEAvailable_ShouldReturnBoolean()
        {
            // Act
            var result = PerformanceHelpers.IsSSEAvailable;

            // Assert
            Assert.IsType<bool>(result);
            _output.WriteLine($"SSE Available: {result}");
        }

        [Fact]
        public void VectorSize_ShouldBePositive()
        {
            // Act
            var result = PerformanceHelpers.VectorSize;

            // Assert
            Assert.True(result > 0);
            _output.WriteLine($"Vector Size: {result}");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public void GetOptimalBatchSize_ShouldReturnValidSize(int totalItems)
        {
            // Act
            var result = PerformanceHelpers.GetOptimalBatchSize(totalItems);

            // Assert
            Assert.True(result >= totalItems);
            if (PerformanceHelpers.IsSIMDAvailable)
            {
                Assert.True(result % PerformanceHelpers.VectorSize == 0 || result == totalItems);
            }
            _output.WriteLine($"Total Items: {totalItems}, Optimal Batch Size: {result}");
        }

        [Fact]
        public void PrefetchMemory_NullObject_ShouldNotThrow()
        {
            // Act & Assert
            PerformanceHelpers.PrefetchMemory<object>(null);
        }

        [Fact]
        public void PrefetchMemory_ValidObject_ShouldNotThrow()
        {
            // Arrange
            var obj = new TestObject { Id = 1, Name = "Test" };

            // Act & Assert
            PerformanceHelpers.PrefetchMemory(obj);
        }

        [Fact]
        public void PrefetchMemory_EmptySpan_ShouldNotThrow()
        {
            // Arrange
            var span = new Span<int>();

            // Act & Assert
            PerformanceHelpers.PrefetchMemory(span);
        }

        [Fact]
        public void PrefetchMemory_ValidSpan_ShouldNotThrow()
        {
            // Arrange
            var array = new int[] { 1, 2, 3, 4, 5 };
            var span = array.AsSpan();

            // Act & Assert
            PerformanceHelpers.PrefetchMemory(span);
        }

        [Fact]
        public void PrefetchMemory_LargeSpan_ShouldNotThrow()
        {
            // Arrange
            var array = new int[10000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;
            var span = array.AsSpan();

            // Act & Assert
            PerformanceHelpers.PrefetchMemory(span);
        }

        [Fact]
        public void PrefetchMemoryClassArray_NullArray_ShouldNotThrow()
        {
            // Act & Assert
            PerformanceHelpers.PrefetchMemoryClassArray<TestObject>(null);
        }

        [Fact]
        public void PrefetchMemoryClassArray_EmptyArray_ShouldNotThrow()
        {
            // Arrange
            var array = new TestObject[0];

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryClassArray(array);
        }

        [Fact]
        public void PrefetchMemoryClassArray_ValidArray_ShouldNotThrow()
        {
            // Arrange
            var array = new TestObject[]
            {
                new TestObject { Id = 1, Name = "Test1" },
                new TestObject { Id = 2, Name = "Test2" },
                new TestObject { Id = 3, Name = "Test3" }
            };

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryClassArray(array);
        }

        [Fact]
        public void PrefetchMemoryClassArray_ArrayWithNulls_ShouldNotThrow()
        {
            // Arrange
            var array = new TestObject[]
            {
                new TestObject { Id = 1, Name = "Test1" },
                null,
                new TestObject { Id = 3, Name = "Test3" }
            };

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryClassArray(array);
        }

        [Fact]
        public void PrefetchMemoryValueArray_NullArray_ShouldNotThrow()
        {
            // Act & Assert
            PerformanceHelpers.PrefetchMemoryValueArray<int>(null);
        }

        [Fact]
        public void PrefetchMemoryValueArray_EmptyArray_ShouldNotThrow()
        {
            // Arrange
            var array = new int[0];

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryValueArray(array);
        }

        [Fact]
        public void PrefetchMemoryValueArray_ValidArray_ShouldNotThrow()
        {
            // Arrange
            var array = new int[] { 1, 2, 3, 4, 5 };

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryValueArray(array);
        }

        [Fact]
        public void PrefetchMemoryValueArray_LargeArray_ShouldNotThrow()
        {
            // Arrange
            var array = new int[10000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryValueArray(array);
        }

        [Fact]
        public void PrefetchMemoryMultiple_NullArray_ShouldNotThrow()
        {
            // Act & Assert
            PerformanceHelpers.PrefetchMemoryMultiple<object>(null);
        }

        [Fact]
        public void PrefetchMemoryMultiple_EmptyArray_ShouldNotThrow()
        {
            // Arrange
            var array = new TestObject[0];

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryMultiple(array);
        }

        [Fact]
        public void PrefetchMemoryMultiple_ValidArray_ShouldNotThrow()
        {
            // Arrange
            var obj1 = new TestObject { Id = 1, Name = "Test1" };
            var obj2 = new TestObject { Id = 2, Name = "Test2" };
            var obj3 = new TestObject { Id = 3, Name = "Test3" };

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryMultiple(obj1, obj2, obj3);
        }

        [Fact]
        public void PrefetchMemoryMultiple_WithNulls_ShouldNotThrow()
        {
            // Arrange
            var obj1 = new TestObject { Id = 1, Name = "Test1" };
            var obj2 = new TestObject { Id = 2, Name = "Test2" };

            // Act & Assert
            PerformanceHelpers.PrefetchMemoryMultiple(obj1, null, obj2);
        }

        [Fact]
        public void PrefetchMemoryMultiple_MoreThanFourObjects_ShouldOnlyPrefetchFirstFour()
        {
            // Arrange
            var objects = new TestObject[10];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = new TestObject { Id = i, Name = $"Test{i}" };

            // Act & Assert - Should not throw even with more than 4 objects
            PerformanceHelpers.PrefetchMemoryMultiple(objects);
        }

        [Fact]
        public void InitializeArray_ValidSpan_ShouldFillCorrectly()
        {
            // Arrange
            var array = new int[100];
            var span = array.AsSpan();
            const int value = 42;

            // Act
            PerformanceHelpers.InitializeArray(span, value);

            // Assert
            foreach (var item in span)
            {
                Assert.Equal(value, item);
            }
        }

        [Fact]
        public void InitializeArray_EmptySpan_ShouldNotThrow()
        {
            // Arrange
            var span = new Span<int>();

            // Act & Assert
            PerformanceHelpers.InitializeArray(span, 42);
        }

        [Fact]
        public void InitializeArray_SmallSpan_ShouldFillCorrectly()
        {
            // Arrange
            var array = new int[3];
            var span = array.AsSpan();
            const int value = 123;

            // Act
            PerformanceHelpers.InitializeArray(span, value);

            // Assert
            Assert.Equal(value, span[0]);
            Assert.Equal(value, span[1]);
            Assert.Equal(value, span[2]);
        }

        [Fact]
        public void SequenceEqual_IdenticalSpans_ShouldReturnTrue()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3, 4, 5 };
            var data2 = new byte[] { 1, 2, 3, 4, 5 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqual_DifferentSpans_ShouldReturnFalse()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3, 4, 5 };
            var data2 = new byte[] { 1, 2, 3, 4, 6 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqual_DifferentLengths_ShouldReturnFalse()
        {
            // Arrange
            var data1 = new byte[] { 1, 2, 3 };
            var data2 = new byte[] { 1, 2, 3, 4 };
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqual_EmptySpans_ShouldReturnTrue()
        {
            // Arrange
            var span1 = new Span<byte>();
            var span2 = new Span<byte>();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqual_LargeSpans_ShouldReturnCorrectResult()
        {
            // Arrange
            var data1 = new byte[10000];
            var data2 = new byte[10000];
            
            for (int i = 0; i < data1.Length; i++)
            {
                data1[i] = (byte)(i % 256);
                data2[i] = (byte)(i % 256);
            }
            
            // Make one difference
            data2[5000] = 255;
            
            var span1 = data1.AsSpan();
            var span2 = data2.AsSpan();

            // Act
            var result = PerformanceHelpers.SequenceEqual(span1, span2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task PrefetchMemory_PerformanceTest_ShouldCompleteInReasonableTime()
        {
            // Arrange
            var objects = new TestObject[1000];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = new TestObject { Id = i, Name = $"Test{i}" };

            var stopwatch = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 100; i++)
            {
                foreach (var obj in objects)
                {
                    PerformanceHelpers.PrefetchMemory(obj);
                }
            }

            stopwatch.Stop();

            // Assert
            _output.WriteLine($"PrefetchMemory performance: 100,000 calls in {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"Average: {(double)stopwatch.ElapsedMilliseconds / 100000:F6}ms per call");
            
            // Should complete quickly (this is a very loose requirement)
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, "PrefetchMemory took too long");
        }

        [Fact]
        public void PrefetchMemory_MemoryAccessPattern_ShouldNotCorruptData()
        {
            // Arrange
            var originalData = new int[1000];
            for (int i = 0; i < originalData.Length; i++)
                originalData[i] = i;

            var testData = new int[1000];
            Array.Copy(originalData, testData, originalData.Length);

            // Act
            PerformanceHelpers.PrefetchMemory(testData.AsSpan());

            // Assert - Data should remain unchanged
            for (int i = 0; i < originalData.Length; i++)
            {
                Assert.Equal(originalData[i], testData[i]);
            }
        }

        // Test helper class
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
}
