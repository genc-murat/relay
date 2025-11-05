using System;
using System.Linq;
using Xunit;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Metadata.Endpoints;

namespace Relay.Core.Tests
{
    public class MessageQueueContractTests
    {
        public MessageQueueContractTests()
        {
            // Clear registries before each test
            EndpointMetadataRegistry.Clear();
            MessageQueueContractRegistry.Clear();
        }

        [Fact]
        public void MessageQueueContract_CanBeCreated_WithAllProperties()
        {
            // Arrange & Act
            var contract = new MessageQueueContract
            {
                QueueName = "test-queue",
                ExchangeName = "test-exchange",
                RoutingKey = "test.routing.key",
                MessageType = typeof(string),
                ResponseType = typeof(int),
                HandlerType = typeof(MessageQueueContractTests),
                HandlerMethodName = "TestHandler",
                MessageSchema = new JsonSchemaContract
                {
                    Schema = "{ \"type\": \"string\" }",
                    ContentType = "application/json"
                },
                ResponseSchema = new JsonSchemaContract
                {
                    Schema = "{ \"type\": \"integer\" }",
                    ContentType = "application/json"
                },
                Provider = MessageQueueProvider.RabbitMQ
            };

            // Assert
            Assert.Equal("test-queue", contract.QueueName);
            Assert.Equal("test-exchange", contract.ExchangeName);
            Assert.Equal("test.routing.key", contract.RoutingKey);
            Assert.Equal(typeof(string), contract.MessageType);
            Assert.Equal(typeof(int), contract.ResponseType);
            Assert.Equal(typeof(MessageQueueContractTests), contract.HandlerType);
            Assert.Equal("TestHandler", contract.HandlerMethodName);
            Assert.NotNull(contract.MessageSchema);
            Assert.NotNull(contract.ResponseSchema);
            Assert.Equal(MessageQueueProvider.RabbitMQ, contract.Provider);
            Assert.NotNull(contract.Properties);
        }

        [Fact]
        public void MessageQueueContractRegistry_RegisterContract_AddsContractToRegistry()
        {
            // Arrange
            var contract = new MessageQueueContract
            {
                QueueName = "test-queue",
                MessageType = typeof(string),
                HandlerType = typeof(MessageQueueContractTests)
            };

            // Act
            MessageQueueContractRegistry.RegisterContract(contract);

            // Assert
            var allContracts = MessageQueueContractRegistry.AllContracts;
            Assert.Single(allContracts);
            Assert.Equal(contract, allContracts.First());
        }

        [Fact]
        public void MessageQueueContractRegistry_GetContractsForMessageType_ReturnsCorrectContracts()
        {
            // Arrange
            var contract1 = new MessageQueueContract
            {
                QueueName = "string-queue-1",
                MessageType = typeof(string),
                HandlerType = typeof(MessageQueueContractTests)
            };

            var contract2 = new MessageQueueContract
            {
                QueueName = "string-queue-2",
                MessageType = typeof(string),
                HandlerType = typeof(MessageQueueContractTests)
            };

            var contract3 = new MessageQueueContract
            {
                QueueName = "int-queue",
                MessageType = typeof(int),
                HandlerType = typeof(MessageQueueContractTests)
            };

            MessageQueueContractRegistry.RegisterContract(contract1);
            MessageQueueContractRegistry.RegisterContract(contract2);
            MessageQueueContractRegistry.RegisterContract(contract3);

            // Act
            var stringContracts = MessageQueueContractRegistry.GetContractsForMessageType(typeof(string));
            var intContracts = MessageQueueContractRegistry.GetContractsForMessageType<int>();

            // Assert
            Assert.Equal(2, stringContracts.Count);
            Assert.Contains(contract1, stringContracts);
            Assert.Contains(contract2, stringContracts);

            Assert.Single(intContracts);
            Assert.Contains(contract3, intContracts);
        }

        [Fact]
        public void MessageQueueContractRegistry_GetContractsByProvider_ReturnsCorrectContracts()
        {
            // Arrange
            var contract1 = new MessageQueueContract
            {
                QueueName = "rabbitmq-queue",
                MessageType = typeof(string),
                HandlerType = typeof(MessageQueueContractTests),
                Provider = MessageQueueProvider.RabbitMQ
            };

            var contract2 = new MessageQueueContract
            {
                QueueName = "servicebus-queue",
                MessageType = typeof(int),
                HandlerType = typeof(MessageQueueContractTests),
                Provider = MessageQueueProvider.AzureServiceBus
            };

            var contract3 = new MessageQueueContract
            {
                QueueName = "another-rabbitmq-queue",
                MessageType = typeof(bool),
                HandlerType = typeof(MessageQueueContractTests),
                Provider = MessageQueueProvider.RabbitMQ
            };

            MessageQueueContractRegistry.RegisterContract(contract1);
            MessageQueueContractRegistry.RegisterContract(contract2);
            MessageQueueContractRegistry.RegisterContract(contract3);

            // Act
            var rabbitMqContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.RabbitMQ);
            var serviceBusContracts = MessageQueueContractRegistry.GetContractsByProvider(MessageQueueProvider.AzureServiceBus);

            // Assert
            Assert.Equal(2, rabbitMqContracts.Count);
            Assert.Contains(contract1, rabbitMqContracts);
            Assert.Contains(contract3, rabbitMqContracts);

            Assert.Single(serviceBusContracts);
            Assert.Contains(contract2, serviceBusContracts);
        }

        [Fact]
        public void MessageQueueContractGenerator_GenerateContracts_CreatesContractsFromEndpoints()
        {
            // Arrange
            var metadata1 = new EndpointMetadata
            {
                Route = "/api/create-user",
                HttpMethod = "POST",
                RequestType = typeof(CreateUserCommand),
                ResponseType = typeof(UserCreatedEvent),
                HandlerType = typeof(UserCommandHandler),
                HandlerMethodName = "HandleCreateUser",
                RequestSchema = new JsonSchemaContract { Schema = "{ \"type\": \"object\" }" },
                ResponseSchema = new JsonSchemaContract { Schema = "{ \"type\": \"object\" }" }
            };

            var metadata2 = new EndpointMetadata
            {
                Route = "/api/delete-user",
                HttpMethod = "DELETE",
                RequestType = typeof(DeleteUserCommand),
                HandlerType = typeof(UserCommandHandler),
                HandlerMethodName = "HandleDeleteUser",
                RequestSchema = new JsonSchemaContract { Schema = "{ \"type\": \"object\" }" }
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata1);
            EndpointMetadataRegistry.RegisterEndpoint(metadata2);

            var options = new MessageQueueGenerationOptions
            {
                DefaultProvider = MessageQueueProvider.RabbitMQ,
                QueuePrefix = "myapp",
                DefaultExchange = "myapp.exchange"
            };

            // Act
            var contracts = MessageQueueContractGenerator.GenerateContracts(options);

            // Assert
            Assert.Equal(2, contracts.Count);

            var createUserContract = contracts.First(c => c.MessageType == typeof(CreateUserCommand));
            Assert.Equal("myapp.create-user", createUserContract.QueueName);
            Assert.Equal("myapp.exchange", createUserContract.ExchangeName);
            Assert.Equal("create-user-command", createUserContract.RoutingKey);
            Assert.Equal(typeof(CreateUserCommand), createUserContract.MessageType);
            Assert.Equal(typeof(UserCreatedEvent), createUserContract.ResponseType);
            Assert.Equal(MessageQueueProvider.RabbitMQ, createUserContract.Provider);

            var deleteUserContract = contracts.First(c => c.MessageType == typeof(DeleteUserCommand));
            Assert.Equal("myapp.delete-user", deleteUserContract.QueueName);
            Assert.Equal("myapp.exchange", deleteUserContract.ExchangeName);
            Assert.Equal("delete-user-command", deleteUserContract.RoutingKey);
            Assert.Equal(typeof(DeleteUserCommand), deleteUserContract.MessageType);
            Assert.Null(deleteUserContract.ResponseType);
            Assert.Equal(MessageQueueProvider.RabbitMQ, deleteUserContract.Provider);
        }

        [Fact]
        public void MessageQueueContractGenerator_WithVersionedEndpoints_IncludesVersionInNames()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/v2/users",
                HttpMethod = "POST",
                Version = "v2",
                RequestType = typeof(CreateUserCommand),
                HandlerType = typeof(UserCommandHandler),
                HandlerMethodName = "HandleCreateUser"
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            var options = new MessageQueueGenerationOptions
            {
                DefaultProvider = MessageQueueProvider.RabbitMQ,
                QueuePrefix = "myapp"
            };

            // Act
            var contracts = MessageQueueContractGenerator.GenerateContracts(options);

            // Assert
            Assert.Single(contracts);
            var contract = contracts.First();
            Assert.Equal("myapp.create-user.v2", contract.QueueName);
            Assert.Equal("v2.create-user-command", contract.RoutingKey);
        }

        [Fact]
        public void MessageQueueContractGenerator_WithDifferentProviders_GeneratesCorrectContracts()
        {
            // Arrange
            var metadata = new EndpointMetadata
            {
                Route = "/api/test",
                HttpMethod = "POST",
                RequestType = typeof(TestCommand),
                HandlerType = typeof(TestHandler),
                HandlerMethodName = "HandleTest"
            };

            EndpointMetadataRegistry.RegisterEndpoint(metadata);

            // Test Generic provider
            var genericOptions = new MessageQueueGenerationOptions
            {
                DefaultProvider = MessageQueueProvider.Generic
            };

            var azureOptions = new MessageQueueGenerationOptions
            {
                DefaultProvider = MessageQueueProvider.AzureServiceBus,
                QueuePrefix = "servicebus"
            };

            // Act
            var genericContracts = MessageQueueContractGenerator.GenerateContracts(genericOptions);
            var azureContracts = MessageQueueContractGenerator.GenerateContracts(azureOptions);

            // Assert
            var genericContract = genericContracts.First();
            Assert.Equal(MessageQueueProvider.Generic, genericContract.Provider);
            Assert.Null(genericContract.ExchangeName);
            Assert.Null(genericContract.RoutingKey);

            var azureContract = azureContracts.First();
            Assert.Equal(MessageQueueProvider.AzureServiceBus, azureContract.Provider);
            Assert.Equal("servicebus.test", azureContract.QueueName);
            Assert.Null(azureContract.ExchangeName);
            Assert.Null(azureContract.RoutingKey);
        }
    }

    // Test types for message queue contract tests
    public class CreateUserCommand : IRequest<UserCreatedEvent>
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class DeleteUserCommand : IRequest
    {
        public int UserId { get; set; }
    }

    public class TestCommand : IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class UserCreatedEvent
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserCommandHandler
    {
        [Handle]
        public UserCreatedEvent HandleCreateUser(CreateUserCommand command) => new();

        [Handle]
        public void HandleDeleteUser(DeleteUserCommand command) { }
    }

    public class TestHandler
    {
        [Handle]
        public void HandleTest(TestCommand command) { }
    }
}
