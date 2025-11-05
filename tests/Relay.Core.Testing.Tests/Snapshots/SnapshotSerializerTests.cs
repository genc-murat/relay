using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class JsonSnapshotSerializerTests
{
    [Fact]
    public void Constructor_WithNullOptions_UsesDefaultOptions()
    {
        // Act
        var serializer = new JsonSnapshotSerializer(null);

        // Assert
        Assert.NotNull(serializer);
        // We can't directly test the internal options, but we can test serialization behavior
    }

    [Fact]
    public void Constructor_WithDefaultOptions_UsesExpectedDefaults()
    {
        // Act
        var serializer = new JsonSnapshotSerializer();

        // Assert
        Assert.NotNull(serializer);
    }

    [Fact]
    public void Serialize_SimpleObject_ReturnsJsonString()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var testObject = new { Name = "Test", Value = 42 };

        // Act
        var json = serializer.Serialize(testObject);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("Test", json);
        Assert.Contains("42", json);
        Assert.Contains("name", json); // camelCase by default
        Assert.Contains("value", json);
    }

    [Fact]
    public void Serialize_WithIndentedFormatting_IncludesNewlines()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var testObject = new { Property1 = "Value1", Property2 = "Value2" };

        // Act
        var json = serializer.Serialize(testObject);

        // Assert
        Assert.Contains("\n", json); // Should be indented
        Assert.Contains("  ", json); // Should have indentation
    }

    [Fact]
    public void Serialize_NullValue_HandlesCorrectly()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        string nullValue = null;

        // Act
        var json = serializer.Serialize(nullValue);

        // Assert
        Assert.Equal("null", json);
    }

    [Fact]
    public void Serialize_ComplexObject_WithNestedProperties()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var complexObject = new
        {
            Id = 123,
            Details = new
            {
                Name = "Complex Test",
                Tags = new[] { "tag1", "tag2", "tag3" },
                Metadata = new { Version = "1.0", Active = true }
            },
            Numbers = new[] { 1, 2, 3, 4, 5 }
        };

        // Act
        var json = serializer.Serialize(complexObject);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("123", json);
        Assert.Contains("Complex Test", json);
        Assert.Contains("tag1", json);
        Assert.Contains("tag2", json);
        Assert.Contains("tag3", json);
        Assert.Contains("1.0", json);
        Assert.Contains("true", json);
        Assert.Contains("1", json);
        Assert.Contains("5", json);
    }

    [Fact]
    public void Deserialize_SimpleObject_ReturnsCorrectObject()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var json = "{\"name\":\"Test\",\"value\":42}";
        var expected = new { name = "Test", value = 42 };

        // Act
        var result = serializer.Deserialize<TestObject>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Deserialize_ComplexObject_ReturnsCorrectObject()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var json = @"{
  ""id"": 123,
  ""details"": {
    ""name"": ""Complex Test"",
    ""tags"": [
      ""tag1"",
      ""tag2""
    ],
    ""metadata"": {
      ""version"": ""1.0"",
      ""active"": true
    }
  },
  ""numbers"": [
    1,
    2,
    3
  ]
}";

        // Act
        var result = serializer.Deserialize<ComplexTestObject>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(123, result.Id);
        Assert.Equal("Complex Test", result.Details.Name);
        Assert.Equal(2, result.Details.Tags.Length);
        Assert.Equal("tag1", result.Details.Tags[0]);
        Assert.Equal("tag2", result.Details.Tags[1]);
        Assert.Equal("1.0", result.Details.Metadata.Version);
        Assert.True(result.Details.Metadata.Active);
        Assert.Equal(3, result.Numbers.Length);
        Assert.Equal(new[] { 1, 2, 3 }, result.Numbers);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var invalidJson = "{\"invalid\": json}";

        // Act & Assert
        Assert.Throws<JsonException>(() => serializer.Deserialize<TestObject>(invalidJson));
    }

    [Fact]
    public void Deserialize_NullJson_ThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => serializer.Deserialize<TestObject>(null!));
    }

    [Fact]
    public void RoundTrip_SerializeThenDeserialize_ReturnsEquivalentObject()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var original = new TestObject
        {
            Name = "RoundTrip Test",
            Value = 999,
            IsActive = true,
            Tags = new[] { "round", "trip", "test" }
        };

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<TestObject>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.Name, deserialized.Name);
        Assert.Equal(original.Value, deserialized.Value);
        Assert.Equal(original.IsActive, deserialized.IsActive);
        Assert.Equal(original.Tags, deserialized.Tags);
    }

    [Fact]
    public void Serialize_WithCustomOptions_UsesCustomOptions()
    {
        // Arrange
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
        var serializer = new JsonSnapshotSerializer(options);
        var testObject = new { FirstName = "John", LastName = "Doe" };

        // Act
        var json = serializer.Serialize(testObject);

        // Assert
        Assert.NotNull(json);
        Assert.DoesNotContain("\n", json); // No indentation
        Assert.Contains("first_name", json); // snake_case
        Assert.Contains("last_name", json);
    }

    [Fact]
    public void Serialize_IgnoresNullValues_WhenConfigured()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var testObject = new
        {
            Name = "Test",
            Value = (string)null,
            Active = true
        };

        // Act
        var json = serializer.Serialize(testObject);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("Test", json);
        Assert.Contains("true", json);
        Assert.DoesNotContain("value", json); // null value should be ignored
    }

    [Fact]
    public void Serialize_PrimitiveTypes_HandlesCorrectly()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();

        // Act & Assert
        Assert.Equal("42", serializer.Serialize(42));
        Assert.Equal("3.14", serializer.Serialize(3.14));
        Assert.Equal("true", serializer.Serialize(true));
        Assert.Equal("false", serializer.Serialize(false));
        Assert.Equal("\"Hello World\"", serializer.Serialize("Hello World"));
    }

    [Fact]
    public void Deserialize_PrimitiveTypes_HandlesCorrectly()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();

        // Act & Assert
        Assert.Equal(42, serializer.Deserialize<int>("42"));
        Assert.Equal(3.14, serializer.Deserialize<double>("3.14"));
        Assert.True(serializer.Deserialize<bool>("true"));
        Assert.False(serializer.Deserialize<bool>("false"));
        Assert.Equal("Hello World", serializer.Deserialize<string>("\"Hello World\""));
    }

    [Fact]
    public void Serialize_EmptyObject_ProducesValidJson()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var emptyObject = new { };

        // Act
        var json = serializer.Serialize(emptyObject);

        // Assert
        Assert.Equal("{}", json);
    }

    [Fact]
    public void Deserialize_EmptyJson_ReturnsDefaultObject()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();

        // Act
        var result = serializer.Deserialize<TestObject>("{}");

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Equal(0, result.Value);
        Assert.False(result.IsActive);
        Assert.Null(result.Tags);
    }

    [Fact]
    public void Serialize_Array_HandlesCorrectly()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var json = serializer.Serialize(array);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("1", json);
        Assert.Contains("2", json);
        Assert.Contains("3", json);
        Assert.Contains("4", json);
        Assert.Contains("5", json);
        Assert.Contains("[", json);
        Assert.Contains("]", json);
    }

    [Fact]
    public void Deserialize_Array_HandlesCorrectly()
    {
        // Arrange
        var serializer = new JsonSnapshotSerializer();
        var json = "[1, 2, 3, 4, 5]";

        // Act
        var result = serializer.Deserialize<int[]>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Length);
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
    }
}

// Test classes for serialization tests
public class TestObject
{
    public string? Name { get; set; }
    public int Value { get; set; }
    public bool IsActive { get; set; }
    public string[]? Tags { get; set; }
}

public class ComplexTestObject
{
    public int Id { get; set; }
    public DetailsObject Details { get; set; } = new();
    public int[] Numbers { get; set; } = Array.Empty<int>();
}

public class DetailsObject
{
    public string Name { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public MetadataObject Metadata { get; set; } = new();
}

public class MetadataObject
{
    public string Version { get; set; } = string.Empty;
    public bool Active { get; set; }
}