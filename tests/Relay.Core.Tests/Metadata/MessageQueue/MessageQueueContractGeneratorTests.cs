using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Relay.Core.Metadata.Endpoints;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.Metadata.MessageQueue;

public class MessageQueueContractGeneratorTests
{
    public MessageQueueContractGeneratorTests()
    {
        // Clear registries before each test
        EndpointMetadataRegistry.Clear();
        MessageQueueContractRegistry.Clear();
    }

    [Fact]
    public void GenerateContracts_WithNullOptions_UsesDefaultOptions()
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

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(null);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("test", contract.QueueName); // "TestCommand" -> "Test" (remove suffix) -> "test" (to kebab case)
        Assert.Equal(MessageQueueProvider.Generic, contract.Provider);
        Assert.Null(contract.ExchangeName);
        Assert.Null(contract.RoutingKey);
    }

    [Fact]
    public void GenerateContracts_WithNullOptionsParameter_UsesDefaultOptions()
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

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, null);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("test", contract.QueueName); // "TestCommand" -> "Test" (remove suffix) -> "test" (to kebab case)
        Assert.Equal(MessageQueueProvider.Generic, contract.Provider);
    }

    [Fact]
    public void GenerateContracts_WithIEnumerableAndNullOptions_UsesDefaultOptions()
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

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, null);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("test", contract.QueueName); // "TestCommand" -> "Test" (remove suffix) -> "test" (to kebab case)
    }

    [Fact]
    public void GenerateContracts_WithEndpointWithRequestSuffix_RemovesSuffix()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            RequestType = typeof(GetUserRequest), // Ends with "Request"
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleGetUser"
        };

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("get-user", contract.QueueName); // Should remove "Request" suffix
    }

    [Fact]
    public void GenerateContracts_WithEndpointWithCommandSuffix_RemovesSuffix()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            RequestType = typeof(CreateUserCommand), // Ends with "Command"
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleCreateUser"
        };

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("create-user", contract.QueueName); // Should remove "Command" suffix
    }

    [Fact]
    public void GenerateContracts_WithEndpointWithQuerySuffix_RemovesSuffix()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "GET",
            RequestType = typeof(GetUserQuery), // Ends with "Query"
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleGetUserQuery"
        };

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("get-user", contract.QueueName); // Should remove "Query" suffix
    }

    [Fact]
    public void GenerateContracts_WithQueuePrefix_AddsPrefixToQueueName()
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

        var endpoints = new List<EndpointMetadata> { metadata };
        var options = new MessageQueueGenerationOptions
        {
            QueuePrefix = "myapp"
        };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, options);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("myapp.test", contract.QueueName); // "TestCommand" -> "Test" (remove suffix) -> "test" -> "myapp.test"
    }

    [Fact]
    public void GenerateContracts_WithVersion_AddsVersionToQueueName()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/v1/test",
            HttpMethod = "POST",
            Version = "v1",
            RequestType = typeof(TestCommand),
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleTest"
        };

        var endpoints = new List<EndpointMetadata> { metadata };
        var options = new MessageQueueGenerationOptions();

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, options);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("test.v1", contract.QueueName); // "TestCommand" -> "Test" -> "test" -> "test.v1"
    }

    [Fact]
    public void GenerateContracts_WithRabbitMQProvider_SetsExchangeAndRoutingKey()
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

        var endpoints = new List<EndpointMetadata> { metadata };
        var options = new MessageQueueGenerationOptions
        {
            DefaultProvider = MessageQueueProvider.RabbitMQ,
            DefaultExchange = "relay.exchange"
        };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, options);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("test", contract.QueueName); // "TestCommand" -> "Test" -> "test"
        Assert.Equal("relay.exchange", contract.ExchangeName);
        Assert.Equal("test-command", contract.RoutingKey); // Uses full request type name "TestCommand" -> "test-command" (no suffix removal for routing key)
    }

    [Fact]
    public void GenerateContracts_WithRabbitMQProviderAndVersion_IncludesVersionInRoutingKey()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/v2/test",
            HttpMethod = "POST",
            Version = "v2",
            RequestType = typeof(TestCommand),
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleTest"
        };

        var endpoints = new List<EndpointMetadata> { metadata };
        var options = new MessageQueueGenerationOptions
        {
            DefaultProvider = MessageQueueProvider.RabbitMQ,
            DefaultExchange = "relay.exchange"
        };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, options);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("v2.test-command", contract.RoutingKey);
    }

    [Fact]
    public void GenerateContracts_WithNonRabbitMQProvider_DoesNotSetExchangeOrRoutingKey()
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

        var endpoints = new List<EndpointMetadata> { metadata };
        var options = new MessageQueueGenerationOptions
        {
            DefaultProvider = MessageQueueProvider.AzureServiceBus
        };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints, options);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Null(contract.ExchangeName);
        Assert.Null(contract.RoutingKey);
    }

    [Fact]
    public void GenerateContracts_WithMultipleEndpoints_GeneratesMultipleContracts()
    {
        // Arrange
        var metadata1 = new EndpointMetadata
        {
            Route = "/api/test1",
            HttpMethod = "POST",
            RequestType = typeof(TestCommand1),
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleTest1"
        };

        var metadata2 = new EndpointMetadata
        {
            Route = "/api/test2",
            HttpMethod = "POST",
            RequestType = typeof(TestCommand2),
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleTest2"
        };

        var metadata3 = new EndpointMetadata
        {
            Route = "/api/test3",
            HttpMethod = "POST",
            RequestType = typeof(TestCommand3),
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleTest3"
        };

        var endpoints = new List<EndpointMetadata> { metadata1, metadata2, metadata3 };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Equal(3, contracts.Count);
        Assert.Contains(contracts, c => c.MessageType == typeof(TestCommand1));
        Assert.Contains(contracts, c => c.MessageType == typeof(TestCommand2));
        Assert.Contains(contracts, c => c.MessageType == typeof(TestCommand3));
    }

    [Fact]
    public void GenerateContracts_WithEmptyEndpoints_ReturnsEmptyList()
    {
        // Arrange
        var endpoints = new List<EndpointMetadata>();

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Empty(contracts);
    }

    [Fact]
    public void GenerateContracts_WithNullEndpoints_ThrowsArgumentNullException()
    {
        // Act & Assert - This should throw ArgumentNullException when endpoints is null
        var exception = Assert.Throws<ArgumentNullException>(() => 
            MessageQueueContractGenerator.GenerateContracts((IEnumerable<EndpointMetadata>)null));
        
        // Check that the parameter name is correct
        Assert.Equal("endpoints", exception.ParamName);
    }

    [Fact]
    public void GenerateContracts_WithCamelCaseType_ConvertsToKebabCase()
    {
        // Arrange - Test the ToKebabCase functionality
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            RequestType = typeof(GetUserProfileByIdQuery), // CamelCase with multiple caps
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleGetUserProfile"
        };

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("get-user-profile-by-id", contract.QueueName); // Should convert CamelCase to kebab-case
    }

    [Fact]
    public void GenerateContracts_WithPascalCaseType_ConvertsToKebabCase()
    {
        // Arrange
        var metadata = new EndpointMetadata
        {
            Route = "/api/test",
            HttpMethod = "POST",
            RequestType = typeof(CreateNewUserCommand), // PascalCase
            HandlerType = typeof(TestHandler),
            HandlerMethodName = "HandleCreateNewUser"
        };

        var endpoints = new List<EndpointMetadata> { metadata };

        // Act
        var contracts = MessageQueueContractGenerator.GenerateContracts(endpoints);

        // Assert
        Assert.Single(contracts);
        var contract = contracts.First();
        Assert.Equal("create-new-user", contract.QueueName); // Should convert PascalCase to kebab-case
    }
}

// Supporting test types
public class TestCommand : IRequest { }
public class TestCommand1 : IRequest { }
public class TestCommand2 : IRequest { }
public class TestCommand3 : IRequest { }
public class GetUserRequest : IRequest { }
public class CreateUserCommand : IRequest { }
public class GetUserQuery : IRequest { }
public class GetUserProfileByIdQuery : IRequest { }
public class CreateNewUserCommand : IRequest { }

public class TestHandler
{
    [Handle]
    public void HandleTest(TestCommand command) { }
    
    [Handle]
    public void HandleTest1(TestCommand1 command) { }
    
    [Handle]
    public void HandleTest2(TestCommand2 command) { }
    
    [Handle]
    public void HandleTest3(TestCommand3 command) { }
    
    [Handle]
    public void HandleGetUser(GetUserRequest command) { }
    
    [Handle]
    public void HandleCreateUser(CreateUserCommand command) { }
    
    [Handle]
    public void HandleGetUserQuery(GetUserQuery command) { }
    
    [Handle]
    public void HandleGetUserProfile(GetUserProfileByIdQuery command) { }
    
    [Handle]
    public void HandleCreateNewUser(CreateNewUserCommand command) { }
}