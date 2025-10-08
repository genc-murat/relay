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

    public class AOTHelpersTests
    {
        [Fact]
        public void GetTypeName_ShouldReturnCorrectName()
        {
            // Act
            var result = AOTHelpers.GetTypeName<string>();

            // Assert
            Assert.Equal("String", result);
        }

        [Fact]
        public void IsValueType_ShouldReturnCorrectResult()
        {
            // Act & Assert
            Assert.True(AOTHelpers.IsValueType<int>());
            Assert.False(AOTHelpers.IsValueType<string>());
        }

        [Fact]
        public void CreateDefault_ShouldReturnDefault()
        {
            // Act & Assert
            Assert.Equal(0, AOTHelpers.CreateDefault<int>());
            Assert.Equal(default(string), AOTHelpers.CreateDefault<string>());
        }
    }

    public class CacheFriendlyDictionaryTests
    {
        [Fact]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Act
            var dict = new CacheFriendlyDictionary<string, int>();

            // Assert
            Assert.NotNull(dict);
        }

        [Fact]
        public void AddAndTryGetValue_ShouldWorkCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();

            // Act
            dict.Add("key1", 100);
            dict.Add("key2", 200);

            // Assert
            Assert.True(dict.TryGetValue("key1", out var value1));
            Assert.Equal(100, value1);
            Assert.True(dict.TryGetValue("key2", out var value2));
            Assert.Equal(200, value2);
            Assert.False(dict.TryGetValue("key3", out _));
        }

        [Fact]
        public void AddDuplicateKey_ShouldWorkCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();

            // Act
            dict.Add("key", 100);
            dict.Add("key", 200); // Should add new entry with same key

            // Assert
            Assert.True(dict.TryGetValue("key", out var value));
            // Should find one of the values (implementation specific)
            Assert.True(value == 100 || value == 200);
        }

        [Fact]
        public void LargeNumberOfItems_ShouldWorkCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<int, string>();

            // Act
            for (int i = 0; i < 1000; i++)
            {
                dict.Add(i, $"value{i}");
            }

            // Assert
            for (int i = 0; i < 1000; i++)
            {
                Assert.True(dict.TryGetValue(i, out var value));
                Assert.Equal($"value{i}", value);
            }
        }

        [Fact]
        public void Constructor_CustomCapacity_InitializesCorrectly()
        {
            // Arrange & Act
            var dict = new CacheFriendlyDictionary<string, int>(32);

            // Assert
            Assert.NotNull(dict);
        }

        [Fact]
        public void TryGetValue_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();
            dict.Add("existing", 1);

            // Act
            var found = dict.TryGetValue("nonexistent", out var value);

            // Assert
            Assert.False(found);
            Assert.Equal(default(int), value);
        }

        [Fact]
        public void Add_NullKey_ThrowsException()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => dict.Add(null!, 1));
        }

        [Fact]
        public void TryGetValue_NullKey_ThrowsException()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();

            // Act & Assert
            Assert.ThrowsAny<Exception>(() => dict.TryGetValue(null!, out _));
        }

        [Fact]
        public void Add_IntegerKeys_WorksCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<int, string>();

            // Act
            dict.Add(1, "one");
            dict.Add(2, "two");
            dict.Add(3, "three");

            // Assert
            Assert.True(dict.TryGetValue(1, out var one));
            Assert.True(dict.TryGetValue(2, out var two));
            Assert.True(dict.TryGetValue(3, out var three));
            Assert.Equal("one", one);
            Assert.Equal("two", two);
            Assert.Equal("three", three);
        }

        [Fact]
        public void Add_CustomObjectKeys_WorksCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<TestKey, string>();
            var key1 = new TestKey { Id = 1, Name = "test1" };
            var key2 = new TestKey { Id = 2, Name = "test2" };

            // Act
            dict.Add(key1, "value1");
            dict.Add(key2, "value2");

            // Assert
            Assert.True(dict.TryGetValue(key1, out var value1));
            Assert.True(dict.TryGetValue(key2, out var value2));
            Assert.Equal("value1", value1);
            Assert.Equal("value2", value2);
        }

        [Fact]
        public void Add_ItemsWithSameHashCode_HandlesCollisionsCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<CollisionKey, int>();
            var key1 = new CollisionKey { Value = 1 };
            var key2 = new CollisionKey { Value = 2 };
            var key3 = new CollisionKey { Value = 3 };

            // Act
            dict.Add(key1, 10);
            dict.Add(key2, 20);
            dict.Add(key3, 30);

            // Assert
            Assert.True(dict.TryGetValue(key1, out var value1));
            Assert.True(dict.TryGetValue(key2, out var value2));
            Assert.True(dict.TryGetValue(key3, out var value3));
            Assert.Equal(10, value1);
            Assert.Equal(20, value2);
            Assert.Equal(30, value3);
        }

        [Fact]
        public void Add_ItemsBeyondInitialCapacity_TriggersResize()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<int, int>(4); // Small initial capacity
            var itemCount = 20; // More than initial capacity

            // Act
            for (int i = 0; i < itemCount; i++)
            {
                dict.Add(i, i);
            }

            // Assert
            for (int i = 0; i < itemCount; i++)
            {
                var found = dict.TryGetValue(i, out var value);
                Assert.True(found, $"Key {i} not found after resize");
                Assert.Equal(i, value);
            }
        }

        [Fact]
        public void Add_StringKeysWithDifferentCases_HandlesCorrectly()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, int>();

            // Act
            dict.Add("Key", 1);
            dict.Add("key", 2);
            dict.Add("KEY", 3);

            // Assert
            Assert.True(dict.TryGetValue("Key", out var value1));
            Assert.True(dict.TryGetValue("key", out var value2));
            Assert.True(dict.TryGetValue("KEY", out var value3));
            Assert.Equal(1, value1);
            Assert.Equal(2, value2);
            Assert.Equal(3, value3);
        }

        [Fact]
        public void Add_ReferenceTypeValues_HandlesNullValues()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<string, string>();

            // Act
            dict.Add("null", null!);
            dict.Add("notnull", "value");

            // Assert
            Assert.True(dict.TryGetValue("null", out var nullValue));
            Assert.True(dict.TryGetValue("notnull", out var notNullValue));
            Assert.Null(nullValue);
            Assert.Equal("value", notNullValue);
        }

        [Fact]
        public void Performance_LargeNumberOfOperations_CompletesInReasonableTime()
        {
            // Arrange
            var dict = new CacheFriendlyDictionary<int, int>();
            var itemCount = 10000;
            var random = new Random(42); // Fixed seed for reproducibility
            var keys = Enumerable.Range(0, itemCount).OrderBy(_ => random.Next()).ToArray();

            // Act & Assert - Add operations
            var addStart = DateTime.UtcNow;
            foreach (var key in keys)
            {
                dict.Add(key, key * 2);
            }
            var addDuration = DateTime.UtcNow - addStart;

            // Assert - Add should complete quickly (less than 1 second for 10k items)
            Assert.True(addDuration.TotalSeconds < 1.0, $"Add operations took {addDuration.TotalSeconds} seconds");

            // Act & Assert - Lookup operations
            var lookupStart = DateTime.UtcNow;
            foreach (var key in keys)
            {
                var found = dict.TryGetValue(key, out var value);
                Assert.True(found, $"Key {key} not found");
                Assert.Equal(key * 2, value);
            }
            var lookupDuration = DateTime.UtcNow - lookupStart;

            // Assert - Lookup should complete quickly (less than 1 second for 10k items)
            Assert.True(lookupDuration.TotalSeconds < 1.0, $"Lookup operations took {lookupDuration.TotalSeconds} seconds");
        }

        private class TestKey : IEquatable<TestKey>
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;

            public bool Equals(TestKey? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Id == other.Id && Name == other.Name;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as TestKey);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, Name);
            }
        }

        private class CollisionKey : IEquatable<CollisionKey>
        {
            public int Value { get; set; }

            public bool Equals(CollisionKey? other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Value == other.Value;
            }

            public override bool Equals(object? obj)
            {
                return Equals(obj as CollisionKey);
            }

            public override int GetHashCode()
            {
                // Force hash code collisions by using a constant
                return 42;
            }
        }
    }
}