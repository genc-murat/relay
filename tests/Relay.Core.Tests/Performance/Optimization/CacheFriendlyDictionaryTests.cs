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