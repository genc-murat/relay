using Relay.MessageBroker;
using Relay.MessageBroker.Compression;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class EnumComprehensiveTests
{
    #region MessageBrokerType Tests

    [Fact]
    public void MessageBrokerType_ShouldHaveAllExpectedValues()
    {
        // Assert - Verify all broker types
        var values = Enum.GetValues<MessageBrokerType>();
        Assert.Contains(MessageBrokerType.RabbitMQ, values);
        Assert.Contains(MessageBrokerType.Kafka, values);
        Assert.Contains(MessageBrokerType.AzureServiceBus, values);
        Assert.Contains(MessageBrokerType.AwsSqsSns, values);
        Assert.Contains(MessageBrokerType.Nats, values);
        Assert.Contains(MessageBrokerType.RedisStreams, values);
        Assert.Contains(MessageBrokerType.InMemory, values);
    }

    [Theory]
    [InlineData(MessageBrokerType.RabbitMQ)]
    [InlineData(MessageBrokerType.Kafka)]
    [InlineData(MessageBrokerType.AzureServiceBus)]
    [InlineData(MessageBrokerType.AwsSqsSns)]
    [InlineData(MessageBrokerType.Nats)]
    [InlineData(MessageBrokerType.RedisStreams)]
    [InlineData(MessageBrokerType.InMemory)]
    public void MessageBrokerType_AllValues_ShouldConvertToString(MessageBrokerType brokerType)
    {
        // Act
        var stringValue = brokerType.ToString();

        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }

    [Fact]
    public void MessageBrokerType_DefaultValue_ShouldBeRabbitMQ()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        Assert.Equal(MessageBrokerType.RabbitMQ, options.BrokerType);
    }

    #endregion

    #region CompressionAlgorithm Tests

    [Fact]
    public void CompressionAlgorithm_ShouldHaveAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<CompressionAlgorithm>();
        Assert.Contains(CompressionAlgorithm.None, values);
        Assert.Contains(CompressionAlgorithm.GZip, values);
        Assert.Contains(CompressionAlgorithm.Deflate, values);
        Assert.Contains(CompressionAlgorithm.Brotli, values);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.None)]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void CompressionAlgorithm_AllValues_ShouldConvertToString(CompressionAlgorithm algorithm)
    {
        // Act
        var stringValue = algorithm.ToString();

        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }

    [Fact]
    public void CompressionAlgorithm_DefaultValue_ShouldBeGZip()
    {
        // Arrange & Act
        var options = new CompressionOptions();

        // Assert
        Assert.Equal(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip, options.Algorithm);
    }

    #endregion

    #region MessageSerializerType Tests

    [Fact]
    public void MessageSerializerType_ShouldHaveAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<MessageSerializerType>();
        Assert.Contains(MessageSerializerType.Json, values);
        Assert.Contains(MessageSerializerType.MessagePack, values);
        Assert.Contains(MessageSerializerType.Protobuf, values);
        Assert.Contains(MessageSerializerType.Avro, values);
    }

    [Theory]
    [InlineData(MessageSerializerType.Json)]
    [InlineData(MessageSerializerType.MessagePack)]
    [InlineData(MessageSerializerType.Protobuf)]
    [InlineData(MessageSerializerType.Avro)]
    public void MessageSerializerType_AllValues_ShouldConvertToString(MessageSerializerType serializerType)
    {
        // Act
        var stringValue = serializerType.ToString();

        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }

    [Fact]
    public void MessageSerializerType_DefaultValue_ShouldBeJson()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        Assert.Equal(MessageSerializerType.Json, options.SerializerType);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AllEnums_ShouldHaveConsistentNaming()
    {
        // Assert - Enum values should follow PascalCase convention
        var brokerTypes = Enum.GetNames<MessageBrokerType>();
        var compressionAlgorithms = Enum.GetNames<CompressionAlgorithm>();
        var serializerTypes = Enum.GetNames<MessageSerializerType>();

        foreach (var name in brokerTypes.Concat(compressionAlgorithms).Concat(serializerTypes))
        {
            Assert.Matches("^[A-Z][a-zA-Z0-9]*$", name);
        }
    }

    [Fact]
    public void AllEnums_ShouldHaveUniqueValues()
    {
        // Assert - No duplicate enum values
        var brokerTypeValues = Enum.GetValues<MessageBrokerType>().Cast<int>().ToList();
        var compressionValues = Enum.GetValues<CompressionAlgorithm>().Cast<int>().ToList();
        var serializerValues = Enum.GetValues<MessageSerializerType>().Cast<int>().ToList();

        Assert.Equal(brokerTypeValues.Count, brokerTypeValues.Distinct().Count());
        Assert.Equal(compressionValues.Count, compressionValues.Distinct().Count());
        Assert.Equal(serializerValues.Count, serializerValues.Distinct().Count());
    }

    [Fact]
    public void MessageBrokerType_ShouldBeParseable()
    {
        // Arrange
        var typeName = "RabbitMQ";

        // Act
        var parsed = Enum.Parse<MessageBrokerType>(typeName);

        // Assert
        Assert.Equal(MessageBrokerType.RabbitMQ, parsed);
    }

    [Fact]
    public void CompressionAlgorithm_ShouldBeParseable()
    {
        // Arrange
        var algorithmName = "GZip";

        // Act
        var parsed = Enum.Parse<CompressionAlgorithm>(algorithmName);

        // Assert
        Assert.Equal(CompressionAlgorithm.GZip, parsed);
    }

    [Fact]
    public void MessageSerializerType_ShouldBeParseable()
    {
        // Arrange
        var typeName = "Json";

        // Act
        var parsed = Enum.Parse<MessageSerializerType>(typeName);

        // Assert
        Assert.Equal(MessageSerializerType.Json, parsed);
    }

    [Fact]
    public void Enums_ShouldSupportTryParse()
    {
        // Act & Assert
        Assert.True(Enum.TryParse<MessageBrokerType>("Kafka", out var brokerType));
        Assert.Equal(MessageBrokerType.Kafka, brokerType);

        Assert.True(Enum.TryParse<CompressionAlgorithm>("Brotli", out var algorithm));
        Assert.Equal(CompressionAlgorithm.Brotli, algorithm);

        Assert.True(Enum.TryParse<MessageSerializerType>("MessagePack", out var serializer));
        Assert.Equal(MessageSerializerType.MessagePack, serializer);
    }

    [Fact]
    public void Enums_WithInvalidValue_TryParseShouldReturnFalse()
    {
        // Act & Assert
        Assert.False(Enum.TryParse<MessageBrokerType>("InvalidBroker", out _));
        Assert.False(Enum.TryParse<CompressionAlgorithm>("InvalidAlgorithm", out _));
        Assert.False(Enum.TryParse<MessageSerializerType>("InvalidSerializer", out _));
    }

    #endregion
}
