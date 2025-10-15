using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageSerializerTypeTests
{
    [Fact]
    public void MessageSerializerType_ShouldHaveAllExpectedValues()
    {
        // Assert - Verify all enum values exist
        Assert.Equal(0, (int)MessageSerializerType.Json);
        Assert.Equal(1, (int)MessageSerializerType.MessagePack);
        Assert.Equal(2, (int)MessageSerializerType.Protobuf);
        Assert.Equal(3, (int)MessageSerializerType.Avro);
    }

    [Fact]
    public void MessageSerializerType_ShouldHaveCorrectNames()
    {
        // Arrange & Act & Assert
        Assert.Equal("Json", MessageSerializerType.Json.ToString());
        Assert.Equal("MessagePack", MessageSerializerType.MessagePack.ToString());
        Assert.Equal("Protobuf", MessageSerializerType.Protobuf.ToString());
        Assert.Equal("Avro", MessageSerializerType.Avro.ToString());
    }

    [Fact]
    public void MessageSerializerType_ShouldHaveFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(MessageSerializerType));

        // Assert
        Assert.Equal(4, values.Length);
    }

    [Theory]
    [InlineData(MessageSerializerType.Json)]
    [InlineData(MessageSerializerType.MessagePack)]
    [InlineData(MessageSerializerType.Protobuf)]
    [InlineData(MessageSerializerType.Avro)]
    public void MessageSerializerType_ShouldConvertToString(MessageSerializerType serializerType)
    {
        // Act
        var stringValue = serializerType.ToString();

        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
        Assert.Contains(stringValue, new[] { "Json", "MessagePack", "Protobuf", "Avro" });
    }

    [Fact]
    public void MessageSerializerType_ShouldParseFromString()
    {
        // Arrange & Act
        var parsedJson = Enum.Parse<MessageSerializerType>("Json");
        var parsedMessagePack = Enum.Parse<MessageSerializerType>("MessagePack");
        var parsedProtobuf = Enum.Parse<MessageSerializerType>("Protobuf");
        var parsedAvro = Enum.Parse<MessageSerializerType>("Avro");

        // Assert
        Assert.Equal(MessageSerializerType.Json, parsedJson);
        Assert.Equal(MessageSerializerType.MessagePack, parsedMessagePack);
        Assert.Equal(MessageSerializerType.Protobuf, parsedProtobuf);
        Assert.Equal(MessageSerializerType.Avro, parsedAvro);
    }

    [Fact]
    public void MessageSerializerType_ParseCaseInsensitive_ShouldWork()
    {
        // Arrange & Act
        var parsedJson = Enum.Parse<MessageSerializerType>("json", ignoreCase: true);
        var parsedMessagePack = Enum.Parse<MessageSerializerType>("messagepack", ignoreCase: true);
        var parsedProtobuf = Enum.Parse<MessageSerializerType>("PROTOBUF", ignoreCase: true);
        var parsedAvro = Enum.Parse<MessageSerializerType>("AvRo", ignoreCase: true);

        // Assert
        Assert.Equal(MessageSerializerType.Json, parsedJson);
        Assert.Equal(MessageSerializerType.MessagePack, parsedMessagePack);
        Assert.Equal(MessageSerializerType.Protobuf, parsedProtobuf);
        Assert.Equal(MessageSerializerType.Avro, parsedAvro);
    }

    [Fact]
    public void MessageSerializerType_TryParse_ShouldWork()
    {
        // Arrange & Act
        var jsonSuccess = Enum.TryParse("Json", out MessageSerializerType jsonResult);
        var messagePackSuccess = Enum.TryParse("MessagePack", out MessageSerializerType messagePackResult);
        var protobufSuccess = Enum.TryParse("Protobuf", out MessageSerializerType protobufResult);
        var avroSuccess = Enum.TryParse("Avro", out MessageSerializerType avroResult);
        var invalidSuccess = Enum.TryParse("Invalid", out MessageSerializerType invalidResult);

        // Assert
        Assert.True(jsonSuccess);
        Assert.True(messagePackSuccess);
        Assert.True(protobufSuccess);
        Assert.True(avroSuccess);
        Assert.False(invalidSuccess);

        Assert.Equal(MessageSerializerType.Json, jsonResult);
        Assert.Equal(MessageSerializerType.MessagePack, messagePackResult);
        Assert.Equal(MessageSerializerType.Protobuf, protobufResult);
        Assert.Equal(MessageSerializerType.Avro, avroResult);
        Assert.Equal(default(MessageSerializerType), invalidResult);
    }

    [Fact]
    public void MessageSerializerType_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames(typeof(MessageSerializerType));

        // Assert
        Assert.Equal(4, names.Length);
        Assert.Contains("Json", names);
        Assert.Contains("MessagePack", names);
        Assert.Contains("Protobuf", names);
        Assert.Contains("Avro", names);
    }

    [Fact]
    public void MessageSerializerType_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues(typeof(MessageSerializerType));

        // Assert
        Assert.Equal(4, values.Length);
        Assert.Contains(MessageSerializerType.Json, (MessageSerializerType[])values);
        Assert.Contains(MessageSerializerType.MessagePack, (MessageSerializerType[])values);
        Assert.Contains(MessageSerializerType.Protobuf, (MessageSerializerType[])values);
        Assert.Contains(MessageSerializerType.Avro, (MessageSerializerType[])values);
    }

    [Fact]
    public void MessageSerializerType_IsDefined_ShouldWork()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MessageSerializerType), MessageSerializerType.Json));
        Assert.True(Enum.IsDefined(typeof(MessageSerializerType), MessageSerializerType.MessagePack));
        Assert.True(Enum.IsDefined(typeof(MessageSerializerType), MessageSerializerType.Protobuf));
        Assert.True(Enum.IsDefined(typeof(MessageSerializerType), MessageSerializerType.Avro));
        Assert.False(Enum.IsDefined(typeof(MessageSerializerType), (MessageSerializerType)999));
    }

    [Fact]
    public void MessageSerializerType_HasFlagsAttribute_ShouldBeFalse()
    {
        // Act
        var hasFlags = typeof(MessageSerializerType).IsDefined(typeof(FlagsAttribute), false);

        // Assert
        Assert.False(hasFlags);
    }

    [Fact]
    public void MessageSerializerType_UnderlyingType_ShouldBeInt32()
    {
        // Act
        var underlyingType = Enum.GetUnderlyingType(typeof(MessageSerializerType));

        // Assert
        Assert.Equal(typeof(int), underlyingType);
    }

    [Theory]
    [InlineData(MessageSerializerType.Json, 0)]
    [InlineData(MessageSerializerType.MessagePack, 1)]
    [InlineData(MessageSerializerType.Protobuf, 2)]
    [InlineData(MessageSerializerType.Avro, 3)]
    public void MessageSerializerType_CastToInt_ShouldReturnCorrectValue(MessageSerializerType type, int expectedValue)
    {
        // Act
        int intValue = (int)type;

        // Assert
        Assert.Equal(expectedValue, intValue);
    }

    [Theory]
    [InlineData(0, MessageSerializerType.Json)]
    [InlineData(1, MessageSerializerType.MessagePack)]
    [InlineData(2, MessageSerializerType.Protobuf)]
    [InlineData(3, MessageSerializerType.Avro)]
    public void MessageSerializerType_CastFromInt_ShouldReturnCorrectValue(int intValue, MessageSerializerType expectedType)
    {
        // Act
        MessageSerializerType type = (MessageSerializerType)intValue;

        // Assert
        Assert.Equal(expectedType, type);
    }
}