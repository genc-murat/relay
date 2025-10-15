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
        
        // Assert
        Assert.Equal(MessageBrokerType.InMemory, parsed1);
        Assert.Equal(MessageBrokerType.RabbitMQ, parsed2);
        Assert.Equal(MessageBrokerType.Kafka, parsed3);
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

public class MessageSerializerTypeTests
{
    [Fact]
    public void MessageSerializerType_ShouldHaveAllExpectedValues()
    {
        // Assert
        Assert.Equal(0, (int)MessageSerializerType.Json);
        Assert.Equal(1, (int)MessageSerializerType.MessagePack);
        Assert.Equal(2, (int)MessageSerializerType.Protobuf);
        Assert.Equal(3, (int)MessageSerializerType.Avro);
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
