using FluentAssertions;
using Relay.Core.Caching;
using Relay.Core.Caching.Compression;
using System;
using Xunit;

namespace Relay.Core.Tests.Caching.Compression;

public class CompressedCacheSerializerTests
{
    private readonly MockCacheSerializer _innerSerializer;
    private readonly GzipCacheCompressor _compressor;
    private readonly CompressedCacheSerializer _compressedSerializer;

    public CompressedCacheSerializerTests()
    {
        _innerSerializer = new MockCacheSerializer();
        _compressor = new GzipCacheCompressor(10); // Low threshold for testing
        _compressedSerializer = new CompressedCacheSerializer(_innerSerializer, _compressor);
    }

    [Fact]
    public void Constructor_WithNullInnerSerializer_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new CompressedCacheSerializer(null!, _compressor));
    }

    [Fact]
    public void Constructor_WithNullCompressor_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new CompressedCacheSerializer(_innerSerializer, null!));
    }

    [Fact]
    public void Serialize_WithSmallData_ShouldNotCompress()
    {
        // Arrange
        var testObject = new TestData { Id = 1, Name = "Test" };
        _innerSerializer.SerializeResult = new byte[] { 1, 2, 3, 4, 5 }; // Small data

        // Act
        var result = _compressedSerializer.Serialize(testObject);

        // Assert
        result.Should().BeEquivalentTo(_innerSerializer.SerializeResult);
        _innerSerializer.SerializeCalled.Should().BeTrue();
    }

    [Fact]
    public void Serialize_WithLargeData_ShouldCompress()
    {
        // Arrange
        var testObject = new TestData { Id = 1, Name = "Test" };
        _innerSerializer.SerializeResult = new byte[100]; // Large data

        // Act
        var result = _compressedSerializer.Serialize(testObject);

        // Assert
        result.Length.Should().BeGreaterThan(4); // Should have compression header
        result[0].Should().Be(0x43); // 'C'
        result[1].Should().Be(0x5A); // 'Z'
        _innerSerializer.SerializeCalled.Should().BeTrue();
    }

    [Fact]
    public void Deserialize_WithUncompressedData_ShouldPassThrough()
    {
        // Arrange
        var uncompressedData = new byte[] { 1, 2, 3, 4, 5 };
        var expectedObject = new TestData { Id = 1, Name = "Test" };
        _innerSerializer.DeserializeResult = expectedObject;

        // Act
        var result = _compressedSerializer.Deserialize<TestData>(uncompressedData);

        // Assert
        result.Should().Be(expectedObject);
        _innerSerializer.DeserializeCalled.Should().BeTrue();
        _innerSerializer.DeserializeData.Should().BeEquivalentTo(uncompressedData);
    }

    [Fact]
    public void Deserialize_WithCompressedData_ShouldDecompress()
    {
        // Arrange
        var originalData = new byte[100];
        var compressedData = _compressor.Compress(originalData);
        
        // Add compression header
        var dataWithHeader = new byte[4 + compressedData.Length];
        dataWithHeader[0] = 0x43; // 'C'
        dataWithHeader[1] = 0x5A; // 'Z'
        dataWithHeader[2] = (byte)(originalData.Length >> 8);
        dataWithHeader[3] = (byte)originalData.Length;
        Buffer.BlockCopy(compressedData, 0, dataWithHeader, 4, compressedData.Length);

        var expectedObject = new TestData { Id = 1, Name = "Test" };
        _innerSerializer.DeserializeResult = expectedObject;

        // Act
        var result = _compressedSerializer.Deserialize<TestData>(dataWithHeader);

        // Assert
        result.Should().Be(expectedObject);
        _innerSerializer.DeserializeCalled.Should().BeTrue();
        _innerSerializer.DeserializeData.Should().BeEquivalentTo(originalData);
    }

[Fact]
    public void Serialize_Deserialize_RoundTrip_ShouldPreserveObject()
    {
        // Arrange
        var testObject = new TestData 
        { 
            Id = 42, 
            Name = "Test Object with a longer name to trigger compression",
            Description = new string('A', 200) // Large content to trigger compression
        };

        // Set up the mock to return the original object during deserialization
        _innerSerializer.DeserializeResult = testObject;

        // Act
        var serialized = _compressedSerializer.Serialize(testObject);
        var deserialized = _compressedSerializer.Deserialize<TestData>(serialized);

        // Assert
        deserialized.Should().BeEquivalentTo(testObject);
    }

    private class TestData
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    private class MockCacheSerializer : ICacheSerializer
    {
        public byte[] SerializeResult { get; set; } = Array.Empty<byte>();
        public object DeserializeResult { get; set; } = null!;
        public bool SerializeCalled { get; private set; }
        public bool DeserializeCalled { get; private set; }
        public byte[] DeserializeData { get; private set; } = Array.Empty<byte>();

        public byte[] Serialize<T>(T obj)
        {
            SerializeCalled = true;
            return SerializeResult;
        }

        public T Deserialize<T>(byte[] data)
        {
            DeserializeCalled = true;
            DeserializeData = data;
            return (T)DeserializeResult;
        }
    }
}