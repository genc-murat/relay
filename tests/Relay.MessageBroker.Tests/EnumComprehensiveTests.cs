using FluentAssertions;
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
        values.Should().Contain(MessageBrokerType.RabbitMQ);
        values.Should().Contain(MessageBrokerType.Kafka);
        values.Should().Contain(MessageBrokerType.AzureServiceBus);
        values.Should().Contain(MessageBrokerType.AwsSqsSns);
        values.Should().Contain(MessageBrokerType.Nats);
        values.Should().Contain(MessageBrokerType.RedisStreams);
        values.Should().Contain(MessageBrokerType.InMemory);
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
        stringValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageBrokerType_DefaultValue_ShouldBeRabbitMQ()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        options.BrokerType.Should().Be(MessageBrokerType.RabbitMQ);
    }

    #endregion

    #region CompressionAlgorithm Tests

    [Fact]
    public void CompressionAlgorithm_ShouldHaveAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<CompressionAlgorithm>();
        values.Should().Contain(CompressionAlgorithm.None);
        values.Should().Contain(CompressionAlgorithm.GZip);
        values.Should().Contain(CompressionAlgorithm.Deflate);
        values.Should().Contain(CompressionAlgorithm.Brotli);
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
        stringValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CompressionAlgorithm_DefaultValue_ShouldBeGZip()
    {
        // Arrange & Act
        var options = new CompressionOptions();

        // Assert
        options.Algorithm.Should().Be(Relay.Core.Caching.Compression.CompressionAlgorithm.GZip);
    }

    #endregion

    #region MessageSerializerType Tests

    [Fact]
    public void MessageSerializerType_ShouldHaveAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<MessageSerializerType>();
        values.Should().Contain(MessageSerializerType.Json);
        values.Should().Contain(MessageSerializerType.MessagePack);
        values.Should().Contain(MessageSerializerType.Protobuf);
        values.Should().Contain(MessageSerializerType.Avro);
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
        stringValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void MessageSerializerType_DefaultValue_ShouldBeJson()
    {
        // Arrange & Act
        var options = new MessageBrokerOptions();

        // Assert
        options.SerializerType.Should().Be(MessageSerializerType.Json);
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
            name.Should().MatchRegex("^[A-Z][a-zA-Z0-9]*$", 
                $"Enum value '{name}' should follow PascalCase convention");
        }
    }

    [Fact]
    public void AllEnums_ShouldHaveUniqueValues()
    {
        // Assert - No duplicate enum values
        var brokerTypeValues = Enum.GetValues<MessageBrokerType>().Cast<int>().ToList();
        var compressionValues = Enum.GetValues<CompressionAlgorithm>().Cast<int>().ToList();
        var serializerValues = Enum.GetValues<MessageSerializerType>().Cast<int>().ToList();

        brokerTypeValues.Should().OnlyHaveUniqueItems();
        compressionValues.Should().OnlyHaveUniqueItems();
        serializerValues.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void MessageBrokerType_ShouldBeParseable()
    {
        // Arrange
        var typeName = "RabbitMQ";

        // Act
        var parsed = Enum.Parse<MessageBrokerType>(typeName);

        // Assert
        parsed.Should().Be(MessageBrokerType.RabbitMQ);
    }

    [Fact]
    public void CompressionAlgorithm_ShouldBeParseable()
    {
        // Arrange
        var algorithmName = "GZip";

        // Act
        var parsed = Enum.Parse<CompressionAlgorithm>(algorithmName);

        // Assert
        parsed.Should().Be(CompressionAlgorithm.GZip);
    }

    [Fact]
    public void MessageSerializerType_ShouldBeParseable()
    {
        // Arrange
        var typeName = "Json";

        // Act
        var parsed = Enum.Parse<MessageSerializerType>(typeName);

        // Assert
        parsed.Should().Be(MessageSerializerType.Json);
    }

    [Fact]
    public void Enums_ShouldSupportTryParse()
    {
        // Act & Assert
        Enum.TryParse<MessageBrokerType>("Kafka", out var brokerType).Should().BeTrue();
        brokerType.Should().Be(MessageBrokerType.Kafka);

        Enum.TryParse<CompressionAlgorithm>("Brotli", out var algorithm).Should().BeTrue();
        algorithm.Should().Be(CompressionAlgorithm.Brotli);

        Enum.TryParse<MessageSerializerType>("MessagePack", out var serializer).Should().BeTrue();
        serializer.Should().Be(MessageSerializerType.MessagePack);
    }

    [Fact]
    public void Enums_WithInvalidValue_TryParseShouldReturnFalse()
    {
        // Act & Assert
        Enum.TryParse<MessageBrokerType>("InvalidBroker", out _).Should().BeFalse();
        Enum.TryParse<CompressionAlgorithm>("InvalidAlgorithm", out _).Should().BeFalse();
        Enum.TryParse<MessageSerializerType>("InvalidSerializer", out _).Should().BeFalse();
    }

    #endregion
}
