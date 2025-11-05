using System;
using System.Text;
using System.Text.Json;
using Relay.Core.Caching;
using Xunit;

namespace Relay.Core.Tests.Caching
{
    public class JsonCacheSerializerTests
    {
        public class TestObject
        {
            public int Id { get; set; }
            public string? StringValue { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsActive { get; set; }
        }

        [Fact]
        public void SerializeAndDeserialize_WithSimpleObject_ShouldReturnEquivalentObject()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var original = new TestObject
            {
                Id = 1,
                StringValue = "Test",
                Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true
            };

            // Act
            var serialized = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<TestObject>(serialized);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.Id, deserialized.Id);
            Assert.Equal(original.StringValue, deserialized.StringValue);
            Assert.Equal(original.Timestamp, deserialized.Timestamp);
            Assert.Equal(original.IsActive, deserialized.IsActive);
        }

        [Fact]
        public void Serialize_WithDefaultOptions_ShouldUseCamelCase()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var original = new TestObject { StringValue = "Test" };

            // Act
            var serialized = serializer.Serialize(original);
            var jsonString = Encoding.UTF8.GetString(serialized);

            // Assert
            Assert.Contains("\"stringValue\":\"Test\"", jsonString);
        }

        [Fact]
        public void Serialize_WithCustomOptions_ShouldUseProvidedOptions()
        {
            // Arrange
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            var serializer = new JsonCacheSerializer(options);
            var original = new TestObject { StringValue = "Test" };

            // Act
            var serialized = serializer.Serialize(original);
            var jsonString = Encoding.UTF8.GetString(serialized);

            // Assert
            Assert.Contains("\"string_value\":\"Test\"", jsonString);
        }

        [Fact]
        public void Serialize_WithNullObject_ShouldSerializeToNullString()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();

            // Act
            var serialized = serializer.Serialize<TestObject?>(null);
            var jsonString = Encoding.UTF8.GetString(serialized);

            // Assert
            Assert.Equal("null", jsonString);
        }

        [Fact]
        public void Deserialize_NullString_ShouldReturnNull()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var data = Encoding.UTF8.GetBytes("null");

            // Act
            var deserialized = serializer.Deserialize<TestObject?>(data);

            // Assert
            Assert.Null(deserialized);
        }

        [Fact]
        public void Deserialize_InvalidJson_ShouldThrowJsonException()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var invalidData = Encoding.UTF8.GetBytes("{ not json }");

            // Act
            var exception = Assert.Throws<JsonException>(() => serializer.Deserialize<TestObject>(invalidData));

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public void Deserialize_EmptyByteArray_ShouldThrowJsonException()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var emptyData = Array.Empty<byte>();

            // Act
            var exception = Assert.Throws<JsonException>(() => serializer.Deserialize<TestObject>(emptyData));

            // Assert
            Assert.NotNull(exception);
        }
    }
}

