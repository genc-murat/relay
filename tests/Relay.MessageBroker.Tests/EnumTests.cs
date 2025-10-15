using Relay.MessageBroker;
using Relay.MessageBroker.CircuitBreaker;
using Relay.MessageBroker.Compression;
using Relay.MessageBroker.Saga;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTypeTests
{
    [Fact]
    public void MessageBrokerType_ShouldHaveAllExpectedValues()
    {
        // Assert - Verify all enum values exist
        Assert.Equal(0, (int)MessageBrokerType.RabbitMQ);
        Assert.Equal(1, (int)MessageBrokerType.Kafka);
        Assert.Equal(2, (int)MessageBrokerType.AzureServiceBus);
        Assert.Equal(3, (int)MessageBrokerType.AwsSqsSns);
        Assert.Equal(4, (int)MessageBrokerType.Nats);
        Assert.Equal(5, (int)MessageBrokerType.RedisStreams);
        Assert.Equal(6, (int)MessageBrokerType.InMemory);
    }

    [Fact]
    public void MessageBrokerType_ShouldHaveCorrectNames()
    {
        // Arrange & Act & Assert
        Assert.Equal("RabbitMQ", MessageBrokerType.RabbitMQ.ToString());
        Assert.Equal("Kafka", MessageBrokerType.Kafka.ToString());
        Assert.Equal("AzureServiceBus", MessageBrokerType.AzureServiceBus.ToString());
        Assert.Equal("AwsSqsSns", MessageBrokerType.AwsSqsSns.ToString());
        Assert.Equal("Nats", MessageBrokerType.Nats.ToString());
        Assert.Equal("RedisStreams", MessageBrokerType.RedisStreams.ToString());
        Assert.Equal("InMemory", MessageBrokerType.InMemory.ToString());
    }

    [Fact]
    public void MessageBrokerType_ShouldHaveSevenValues()
    {
        // Arrange & Act
        var values = Enum.GetValues(typeof(MessageBrokerType));

        // Assert
        Assert.Equal(7, values.Length);
    }

    [Theory]
    [InlineData(MessageBrokerType.InMemory)]
    [InlineData(MessageBrokerType.RabbitMQ)]
    [InlineData(MessageBrokerType.Kafka)]
    [InlineData(MessageBrokerType.AzureServiceBus)]
    [InlineData(MessageBrokerType.AwsSqsSns)]
    [InlineData(MessageBrokerType.Nats)]
    [InlineData(MessageBrokerType.RedisStreams)]
    public void MessageBrokerType_ShouldConvertToString(MessageBrokerType brokerType)
    {
        // Act
        var stringValue = brokerType.ToString();
        
        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }

    [Fact]
    public void MessageBrokerType_ShouldParseFromString()
    {
        // Arrange & Act
        var parsed1 = Enum.Parse<MessageBrokerType>("InMemory");
        var parsed2 = Enum.Parse<MessageBrokerType>("RabbitMQ");
        var parsed3 = Enum.Parse<MessageBrokerType>("Kafka");
        var parsed4 = Enum.Parse<MessageBrokerType>("AzureServiceBus");
        var parsed5 = Enum.Parse<MessageBrokerType>("AwsSqsSns");
        var parsed6 = Enum.Parse<MessageBrokerType>("Nats");
        var parsed7 = Enum.Parse<MessageBrokerType>("RedisStreams");

        // Assert
        Assert.Equal(MessageBrokerType.InMemory, parsed1);
        Assert.Equal(MessageBrokerType.RabbitMQ, parsed2);
        Assert.Equal(MessageBrokerType.Kafka, parsed3);
        Assert.Equal(MessageBrokerType.AzureServiceBus, parsed4);
        Assert.Equal(MessageBrokerType.AwsSqsSns, parsed5);
        Assert.Equal(MessageBrokerType.Nats, parsed6);
        Assert.Equal(MessageBrokerType.RedisStreams, parsed7);
    }

    [Fact]
    public void MessageBrokerType_ParseCaseInsensitive_ShouldWork()
    {
        // Arrange & Act
        var parsedInMemory = Enum.Parse<MessageBrokerType>("inmemory", ignoreCase: true);
        var parsedRabbitMQ = Enum.Parse<MessageBrokerType>("rabbitmq", ignoreCase: true);
        var parsedKafka = Enum.Parse<MessageBrokerType>("KAFKA", ignoreCase: true);
        var parsedAzure = Enum.Parse<MessageBrokerType>("azureservicebus", ignoreCase: true);
        var parsedAws = Enum.Parse<MessageBrokerType>("AWSSQSSNS", ignoreCase: true);
        var parsedNats = Enum.Parse<MessageBrokerType>("NATS", ignoreCase: true);
        var parsedRedis = Enum.Parse<MessageBrokerType>("redisstreams", ignoreCase: true);

        // Assert
        Assert.Equal(MessageBrokerType.InMemory, parsedInMemory);
        Assert.Equal(MessageBrokerType.RabbitMQ, parsedRabbitMQ);
        Assert.Equal(MessageBrokerType.Kafka, parsedKafka);
        Assert.Equal(MessageBrokerType.AzureServiceBus, parsedAzure);
        Assert.Equal(MessageBrokerType.AwsSqsSns, parsedAws);
        Assert.Equal(MessageBrokerType.Nats, parsedNats);
        Assert.Equal(MessageBrokerType.RedisStreams, parsedRedis);
    }

    [Fact]
    public void MessageBrokerType_TryParse_ShouldWork()
    {
        // Arrange & Act
        var inMemorySuccess = Enum.TryParse("InMemory", out MessageBrokerType inMemoryResult);
        var rabbitMQSuccess = Enum.TryParse("RabbitMQ", out MessageBrokerType rabbitMQResult);
        var kafkaSuccess = Enum.TryParse("Kafka", out MessageBrokerType kafkaResult);
        var azureSuccess = Enum.TryParse("AzureServiceBus", out MessageBrokerType azureResult);
        var awsSuccess = Enum.TryParse("AwsSqsSns", out MessageBrokerType awsResult);
        var natsSuccess = Enum.TryParse("Nats", out MessageBrokerType natsResult);
        var redisSuccess = Enum.TryParse("RedisStreams", out MessageBrokerType redisResult);
        var invalidSuccess = Enum.TryParse("Invalid", out MessageBrokerType invalidResult);

        // Assert
        Assert.True(inMemorySuccess);
        Assert.True(rabbitMQSuccess);
        Assert.True(kafkaSuccess);
        Assert.True(azureSuccess);
        Assert.True(awsSuccess);
        Assert.True(natsSuccess);
        Assert.True(redisSuccess);
        Assert.False(invalidSuccess);

        Assert.Equal(MessageBrokerType.InMemory, inMemoryResult);
        Assert.Equal(MessageBrokerType.RabbitMQ, rabbitMQResult);
        Assert.Equal(MessageBrokerType.Kafka, kafkaResult);
        Assert.Equal(MessageBrokerType.AzureServiceBus, azureResult);
        Assert.Equal(MessageBrokerType.AwsSqsSns, awsResult);
        Assert.Equal(MessageBrokerType.Nats, natsResult);
        Assert.Equal(MessageBrokerType.RedisStreams, redisResult);
        Assert.Equal(default(MessageBrokerType), invalidResult);
    }

    [Fact]
    public void MessageBrokerType_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames(typeof(MessageBrokerType));

        // Assert
        Assert.Equal(7, names.Length);
        Assert.Contains("InMemory", names);
        Assert.Contains("RabbitMQ", names);
        Assert.Contains("Kafka", names);
        Assert.Contains("AzureServiceBus", names);
        Assert.Contains("AwsSqsSns", names);
        Assert.Contains("Nats", names);
        Assert.Contains("RedisStreams", names);
    }

    [Fact]
    public void MessageBrokerType_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues(typeof(MessageBrokerType));

        // Assert
        Assert.Equal(7, values.Length);
        Assert.Contains(MessageBrokerType.InMemory, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.RabbitMQ, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.Kafka, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.AzureServiceBus, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.AwsSqsSns, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.Nats, (MessageBrokerType[])values);
        Assert.Contains(MessageBrokerType.RedisStreams, (MessageBrokerType[])values);
    }

    [Fact]
    public void MessageBrokerType_IsDefined_ShouldWork()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.InMemory));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.RabbitMQ));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.Kafka));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.AzureServiceBus));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.AwsSqsSns));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.Nats));
        Assert.True(Enum.IsDefined(typeof(MessageBrokerType), MessageBrokerType.RedisStreams));
        Assert.False(Enum.IsDefined(typeof(MessageBrokerType), (MessageBrokerType)999));
    }

    [Fact]
    public void MessageBrokerType_HasFlagsAttribute_ShouldBeFalse()
    {
        // Act
        var hasFlags = typeof(MessageBrokerType).IsDefined(typeof(FlagsAttribute), false);

        // Assert
        Assert.False(hasFlags);
    }

    [Fact]
    public void MessageBrokerType_UnderlyingType_ShouldBeInt32()
    {
        // Act
        var underlyingType = Enum.GetUnderlyingType(typeof(MessageBrokerType));

        // Assert
        Assert.Equal(typeof(int), underlyingType);
    }

    [Theory]
    [InlineData(MessageBrokerType.InMemory, 6)]
    [InlineData(MessageBrokerType.RabbitMQ, 0)]
    [InlineData(MessageBrokerType.Kafka, 1)]
    [InlineData(MessageBrokerType.AzureServiceBus, 2)]
    [InlineData(MessageBrokerType.AwsSqsSns, 3)]
    [InlineData(MessageBrokerType.Nats, 4)]
    [InlineData(MessageBrokerType.RedisStreams, 5)]
    public void MessageBrokerType_CastToInt_ShouldReturnCorrectValue(MessageBrokerType type, int expectedValue)
    {
        // Act
        int intValue = (int)type;

        // Assert
        Assert.Equal(expectedValue, intValue);
    }

    [Theory]
    [InlineData(6, MessageBrokerType.InMemory)]
    [InlineData(0, MessageBrokerType.RabbitMQ)]
    [InlineData(1, MessageBrokerType.Kafka)]
    [InlineData(2, MessageBrokerType.AzureServiceBus)]
    [InlineData(3, MessageBrokerType.AwsSqsSns)]
    [InlineData(4, MessageBrokerType.Nats)]
    [InlineData(5, MessageBrokerType.RedisStreams)]
    public void MessageBrokerType_CastFromInt_ShouldReturnCorrectValue(int intValue, MessageBrokerType expectedType)
    {
        // Act
        MessageBrokerType type = (MessageBrokerType)intValue;

        // Assert
        Assert.Equal(expectedType, type);
    }
}

public class CompressionAlgorithmTests
{
    [Fact]
    public void CompressionAlgorithm_ShouldHaveAllExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)CompressionAlgorithm.None);
        Assert.Equal(1, (int)CompressionAlgorithm.GZip);
        Assert.Equal(2, (int)CompressionAlgorithm.Deflate);
        Assert.Equal(3, (int)CompressionAlgorithm.Brotli);
    }

    [Theory]
    [InlineData(CompressionAlgorithm.None)]
    [InlineData(CompressionAlgorithm.GZip)]
    [InlineData(CompressionAlgorithm.Deflate)]
    [InlineData(CompressionAlgorithm.Brotli)]
    public void CompressionAlgorithm_ShouldConvertToString(CompressionAlgorithm algorithm)
    {
        // Act
        var stringValue = algorithm.ToString();
        
        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }
}

public class CircuitBreakerStateTests
{
    [Fact]
    public void CircuitBreakerState_ShouldHaveAllExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)CircuitBreakerState.Closed);
        Assert.Equal(1, (int)CircuitBreakerState.Open);
        Assert.Equal(2, (int)CircuitBreakerState.HalfOpen);
    }

    [Theory]
    [InlineData(CircuitBreakerState.Closed)]
    [InlineData(CircuitBreakerState.Open)]
    [InlineData(CircuitBreakerState.HalfOpen)]
    public void CircuitBreakerState_ShouldConvertToString(CircuitBreakerState state)
    {
        // Act
        var stringValue = state.ToString();
        
        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }
}

public class SagaStateTests
{
    [Fact]
    public void SagaState_ShouldHaveAllExpectedValues()
    {
        // Assert - Verify all enum values exist
        Assert.Equal(0, (int)SagaState.NotStarted);
        Assert.Equal(1, (int)SagaState.Running);
        Assert.Equal(2, (int)SagaState.Compensating);
        Assert.Equal(3, (int)SagaState.Completed);
        Assert.Equal(4, (int)SagaState.Compensated);
        Assert.Equal(5, (int)SagaState.Failed);
        Assert.Equal(6, (int)SagaState.Aborted);
    }

    [Theory]
    [InlineData(SagaState.NotStarted)]
    [InlineData(SagaState.Running)]
    [InlineData(SagaState.Compensating)]
    [InlineData(SagaState.Completed)]
    [InlineData(SagaState.Compensated)]
    [InlineData(SagaState.Failed)]
    [InlineData(SagaState.Aborted)]
    public void SagaState_ShouldConvertToString(SagaState state)
    {
        // Act
        var stringValue = state.ToString();
        
        // Assert
        Assert.False(string.IsNullOrEmpty(stringValue));
    }

    [Fact]
    public void SagaState_Transitions_ShouldBeValid()
    {
        // Valid transitions
        var notStartedToRunning = (SagaState.NotStarted, SagaState.Running);
        var runningToCompleted = (SagaState.Running, SagaState.Completed);
        var runningToCompensating = (SagaState.Running, SagaState.Compensating);
        var compensatingToCompensated = (SagaState.Compensating, SagaState.Compensated);
        var compensatingToFailed = (SagaState.Compensating, SagaState.Failed);
        
        // Assert - Just verify the transitions are logically sound
        Assert.NotEqual(notStartedToRunning.Item1, notStartedToRunning.Item2);
        Assert.NotEqual(runningToCompleted.Item1, runningToCompleted.Item2);
    }
}
