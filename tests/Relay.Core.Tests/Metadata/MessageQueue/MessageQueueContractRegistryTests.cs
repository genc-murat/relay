using Relay.Core.Metadata.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Metadata.MessageQueue;

public class MessageQueueContractRegistryTests
{
    public MessageQueueContractRegistryTests()
    {
        // Clear registries before each test
        MessageQueueContractRegistry.Clear();
    }

    [Fact]
    public void RegisterContract_WithValidContract_AddsContractToRegistry()
    {
        // Arrange
        var contract = new MessageQueueContract
        {
            QueueName = "test-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        // Act
        MessageQueueContractRegistry.RegisterContract(contract);

        // Assert
        var allContracts = MessageQueueContractRegistry.AllContracts;
        Assert.Single(allContracts);
        Assert.Contains(contract, allContracts);
    }

    [Fact]
    public void RegisterContract_WithNullContract_ThrowsNullReferenceException()
    {
        // Act & Assert - The implementation doesn't currently have null check
        // This test documents the current behavior
        Assert.Throws<NullReferenceException>(() => 
            MessageQueueContractRegistry.RegisterContract(null));
    }

    [Fact]
    public void AllContracts_Getter_ReturnsReadOnlyList()
    {
        // Arrange
        var contract = new MessageQueueContract
        {
            QueueName = "test-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        MessageQueueContractRegistry.RegisterContract(contract);

        // Act
        var contracts = MessageQueueContractRegistry.AllContracts;

        // Assert
        Assert.Single(contracts);
        Assert.Equal("test-queue", contracts.First().QueueName);
        
        // Verify that the result is read-only by checking its type and behavior
        Assert.IsAssignableFrom<IReadOnlyList<MessageQueueContract>>(contracts);
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<MessageQueueContract>>(contracts);
    }

    [Fact]
    public void RegisterMultipleContracts_SameMessageType_GroupsByMessageType()
    {
        // Arrange
        var contract1 = new MessageQueueContract
        {
            QueueName = "queue1",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        var contract2 = new MessageQueueContract
        {
            QueueName = "queue2", 
            MessageType = typeof(string),
            Provider = MessageQueueProvider.AzureServiceBus
        };

        var contract3 = new MessageQueueContract
        {
            QueueName = "queue3",
            MessageType = typeof(int),
            Provider = MessageQueueProvider.RabbitMQ
        };

        // Act
        MessageQueueContractRegistry.RegisterContract(contract1);
        MessageQueueContractRegistry.RegisterContract(contract2);
        MessageQueueContractRegistry.RegisterContract(contract3);

        // Assert
        var stringContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(string));
        var intContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(int));

        Assert.Equal(2, stringContracts.Count);
        Assert.Contains(contract1, stringContracts);
        Assert.Contains(contract2, stringContracts);

        Assert.Single(intContracts);
        Assert.Contains(contract3, intContracts);

        var allContracts = MessageQueueContractRegistry.AllContracts;
        Assert.Equal(3, allContracts.Count);
    }

    [Fact]
    public void GetContractsForMessageType_WithRegisteredType_ReturnsCorrectContracts()
    {
        // Arrange
        var stringContract = new MessageQueueContract
        {
            QueueName = "string-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        var intContract = new MessageQueueContract
        {
            QueueName = "int-queue",
            MessageType = typeof(int),
            Provider = MessageQueueProvider.AzureServiceBus
        };

        MessageQueueContractRegistry.RegisterContract(stringContract);
        MessageQueueContractRegistry.RegisterContract(intContract);

        // Act
        var stringContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(string));
        var intContracts = MessageQueueContractRegistry.GetContractsForMessageType<int>();
        var nonexistentContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(double));

        // Assert
        Assert.Single(stringContracts);
        Assert.Contains(stringContract, stringContracts);

        Assert.Single(intContracts);
        Assert.Contains(intContract, intContracts);

        Assert.Empty(nonexistentContracts);
    }

    [Fact]
    public void GetContractsForMessageType_WithGenericTypeMethod_ReturnsCorrectContracts()
    {
        // Arrange
        var stringContract = new MessageQueueContract
        {
            QueueName = "string-queue-generic",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        MessageQueueContractRegistry.RegisterContract(stringContract);

        // Act
        var stringContracts = MessageQueueContractRegistry.GetContractsForMessageType<string>();

        // Assert
        Assert.Single(stringContracts);
        Assert.Contains(stringContract, stringContracts);
    }

    [Fact]
    public void GetContractsByProvider_ReturnsCorrectFilteredContracts()
    {
        // Arrange
        var rabbitMqContract = new MessageQueueContract
        {
            QueueName = "rabbitmq-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        var azureContract = new MessageQueueContract
        {
            QueueName = "azure-queue",
            MessageType = typeof(int),
            Provider = MessageQueueProvider.AzureServiceBus
        };

        var genericContract = new MessageQueueContract
        {
            QueueName = "generic-queue",
            MessageType = typeof(bool),
            Provider = MessageQueueProvider.Generic
        };

        MessageQueueContractRegistry.RegisterContract(rabbitMqContract);
        MessageQueueContractRegistry.RegisterContract(azureContract);
        MessageQueueContractRegistry.RegisterContract(genericContract);

        // Act
        var rabbitMqContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.RabbitMQ);
        var azureContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.AzureServiceBus);
        var genericContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.Generic);
        var kafkaContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.Kafka);

        // Assert
        Assert.Single(rabbitMqContracts);
        Assert.Contains(rabbitMqContract, rabbitMqContracts);

        Assert.Single(azureContracts);
        Assert.Contains(azureContract, azureContracts);

        Assert.Single(genericContracts);
        Assert.Contains(genericContract, genericContracts);

        Assert.Empty(kafkaContracts);
    }

    [Fact]
    public void Clear_RemovesAllContracts()
    {
        // Arrange
        var contract = new MessageQueueContract
        {
            QueueName = "test-queue-to-clear",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        MessageQueueContractRegistry.RegisterContract(contract);
        Assert.Single(MessageQueueContractRegistry.AllContracts);

        // Act
        MessageQueueContractRegistry.Clear();

        // Assert
        Assert.Empty(MessageQueueContractRegistry.AllContracts);
        Assert.Empty(MessageQueueContractRegistry.GetContractsForMessageType<string>());
        Assert.Empty(MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.RabbitMQ));
    }

    [Fact]
    public void RegisterContract_MultipleTimesWithSameMessageType_AddsAllContracts()
    {
        // Arrange
        var contract1 = new MessageQueueContract
        {
            QueueName = "first-string-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        var contract2 = new MessageQueueContract
        {
            QueueName = "second-string-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.AzureServiceBus
        };

        // Act
        MessageQueueContractRegistry.RegisterContract(contract1);
        MessageQueueContractRegistry.RegisterContract(contract2);

        // Assert
        var stringContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(string));
        Assert.Equal(2, stringContracts.Count);
        Assert.Contains(contract1, stringContracts);
        Assert.Contains(contract2, stringContracts);
    }

    [Fact]
    public void GetContractsForMessageType_WithNoMatchingContracts_ReturnsEmptyList()
    {
        // Arrange
        var contract = new MessageQueueContract
        {
            QueueName = "existent-queue",
            MessageType = typeof(string),
            Provider = MessageQueueProvider.RabbitMQ
        };

        MessageQueueContractRegistry.RegisterContract(contract);

        // Act
        var nonexistentContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(decimal));

        // Assert
        Assert.Empty(nonexistentContracts);
    }

    [Fact]
    public void AllContracts_InitiallyReturnsEmptyList()
    {
        // Act
        var allContracts = MessageQueueContractRegistry.AllContracts;

        // Assert
        Assert.Empty(allContracts);
    }

    [Fact]
    public void RegisterContract_WithComplexContract_PreservesAllProperties()
    {
        // Arrange
        var complexContract = new MessageQueueContract
        {
            QueueName = "complex-test-queue",
            ExchangeName = "test-exchange",
            RoutingKey = "test.routing.key",
            MessageType = typeof(string), // Use a simple type to avoid the need for custom classes
            ResponseType = typeof(int),
            HandlerType = typeof(object),
            HandlerMethodName = "HandleTest",
            Provider = MessageQueueProvider.RabbitMQ,
            Properties = new Dictionary<string, object> { { "key1", "value1" } }
        };

        // Act
        MessageQueueContractRegistry.RegisterContract(complexContract);

        // Assert
        var allContracts = MessageQueueContractRegistry.AllContracts;
        Assert.Single(allContracts);
        
        var retrievedContract = allContracts.First();
        Assert.Equal("complex-test-queue", retrievedContract.QueueName);
        Assert.Equal("test-exchange", retrievedContract.ExchangeName);
        Assert.Equal("test.routing.key", retrievedContract.RoutingKey);
        Assert.Equal(typeof(string), retrievedContract.MessageType);
        Assert.Equal(typeof(int), retrievedContract.ResponseType);
        Assert.Equal(typeof(object), retrievedContract.HandlerType);
        Assert.Equal("HandleTest", retrievedContract.HandlerMethodName);
        Assert.Equal(MessageQueueProvider.RabbitMQ, retrievedContract.Provider);
        Assert.Contains(new KeyValuePair<string, object>("key1", "value1"), retrievedContract.Properties);
    }
}
