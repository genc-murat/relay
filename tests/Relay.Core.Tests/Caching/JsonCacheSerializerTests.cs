using System;
using System.Text.Json;
using FluentAssertions;
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
            deserialized.Should().BeEquivalentTo(original);
        }

        [Fact]
        public void Serialize_WithDefaultOptions_ShouldUseCamelCase()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var original = new TestObject { StringValue = "Test" };

            // Act
            var serialized = serializer.Serialize(original);
            var jsonString = System.Text.Encoding.UTF8.GetString(serialized);

            // Assert
            jsonString.Should().Contain("\"stringValue\":\"Test\"");
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
            var jsonString = System.Text.Encoding.UTF8.GetString(serialized);

            // Assert
            jsonString.Should().Contain("\"string_value\":\"Test\"");
        }

        [Fact]
        public void Serialize_WithNullObject_ShouldSerializeToNullString()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();

            // Act
            var serialized = serializer.Serialize<TestObject?>(null);
            var jsonString = System.Text.Encoding.UTF8.GetString(serialized);

            // Assert
            jsonString.Should().Be("null");
        }

        [Fact]
        public void Deserialize_NullString_ShouldReturnNull()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var data = System.Text.Encoding.UTF8.GetBytes("null");

            // Act
            var deserialized = serializer.Deserialize<TestObject?>(data);

            // Assert
            deserialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_InvalidJson_ShouldThrowJsonException()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var invalidData = System.Text.Encoding.UTF8.GetBytes("{ not json }");

            // Act
            Action act = () => serializer.Deserialize<TestObject>(invalidData);

            // Assert
            act.Should().Throw<JsonException>();
        }

        [Fact]
        public void Deserialize_EmptyByteArray_ShouldThrowJsonException()
        {
            // Arrange
            var serializer = new JsonCacheSerializer();
            var emptyData = Array.Empty<byte>();

            // Act
            Action act = () => serializer.Deserialize<TestObject>(emptyData);

            // Assert
            act.Should().Throw<JsonException>();
        }
    }
}
