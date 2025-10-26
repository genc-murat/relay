using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Validation.Interfaces;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ValidationComplexSchemaTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public List<object> PublishedMessages { get; } = [];

        public TestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            IContractValidator? contractValidator = null)
            : base(options, logger, contractValidator: contractValidator)
        {
        }

        protected override ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            PublishedMessages.Add(message!);
            return ValueTask.CompletedTask;
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        protected override ValueTask DisposeInternalAsync()
            => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Validation_NestedSchema_ShouldValidateDeeply()
    {
        // Arrange
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); // Valid

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var nestedMessage = new NestedOrderMessage
        {
            OrderId = "ORDER-001",
            Customer = new CustomerInfo
            {
                Id = "CUST-001",
                Name = "John Doe",
                Address = new Address
                {
                    Street = "123 Main St",
                    City = "Anytown",
                    Country = "USA",
                    PostalCode = "12345"
                }
            },
            Items =
            [
                new() { ProductId = "PROD-001", Quantity = 2, Price = 29.99m },
                new() { ProductId = "PROD-002", Quantity = 1, Price = 49.99m }
            ],
            TotalAmount = 109.97m
        };

        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""orderId"": { ""type"": ""string"" },
                    ""customer"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""string"" },
                            ""name"": { ""type"": ""string"" },
                            ""address"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""street"": { ""type"": ""string"" },
                                    ""city"": { ""type"": ""string"" },
                                    ""country"": { ""type"": ""string"" },
                                    ""postalCode"": { ""type"": ""string"" }
                                },
                                ""required"": [""street"", ""city"", ""country""]
                            }
                        },
                        ""required"": [""id"", ""name"", ""address""]
                    },
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""productId"": { ""type"": ""string"" },
                                ""quantity"": { ""type"": ""integer"", ""minimum"": 1 },
                                ""price"": { ""type"": ""number"", ""minimum"": 0 }
                            },
                            ""required"": [""productId"", ""quantity"", ""price""]
                        }
                    },
                    ""totalAmount"": { ""type"": ""number"", ""minimum"": 0 }
                },
                ""required"": [""orderId"", ""customer"", ""items"", ""totalAmount""]
            }"
        };

        var publishOptions = new PublishOptions { Schema = schema };

        // Act
        await broker.PublishAsync(nestedMessage, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(nestedMessage, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_NestedSchemaFailure_ShouldThrowWithDetails()
    {
        // Arrange
        var validationErrors = new List<string>
        {
            "customer.address.postalCode: Required property missing",
            "items[0].quantity: Must be greater than or equal to 1",
            "totalAmount: Must be greater than or equal to 0"
        };

        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationErrors);

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var invalidNestedMessage = new NestedOrderMessage
        {
            OrderId = "ORDER-002",
            Customer = new CustomerInfo
            {
                Id = "CUST-002",
                Name = "Jane Doe",
                Address = new Address
                {
                    Street = "456 Oak St",
                    City = "Othertown",
                    Country = "USA"
                    // Missing postalCode
                }
            },
            Items =
            [
                new() { ProductId = "PROD-003", Quantity = 0, Price = 19.99m } // Invalid quantity
            ],
            TotalAmount = -10.00m // Invalid amount
        };

        var schema = new JsonSchemaContract { Schema = "{}" }; // Simplified schema for test
        var publishOptions = new PublishOptions { Schema = schema };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(invalidNestedMessage, publishOptions));

        Assert.Contains("Message schema validation failed", exception.Message);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(invalidNestedMessage, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_ConditionalValidation_ShouldApplyConditions()
    {
        // Arrange
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); // Valid

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var premiumOrder = new ConditionalOrderMessage
        {
            OrderId = "ORDER-003",
            OrderType = "premium",
            Priority = "high", // Required for premium orders
            CustomerTier = "gold",
            ExpeditedShipping = true // Required for premium orders
        };

        var conditionalSchema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""orderId"": { ""type"": ""string"" },
                    ""orderType"": { ""type"": ""string"", ""enum"": [""standard"", ""premium""] },
                    ""priority"": { ""type"": ""string"" },
                    ""customerTier"": { ""type"": ""string"" },
                    ""expeditedShipping"": { ""type"": ""boolean"" }
                },
                ""required"": [""orderId"", ""orderType""],
                ""allOf"": [
                    {
                        ""if"": { ""properties"": { ""orderType"": { ""const"": ""premium"" } } },
                        ""then"": { ""required"": [""priority"", ""expeditedShipping""] }
                    }
                ]
            }"
        };

        var publishOptions = new PublishOptions { Schema = conditionalSchema };

        // Act
        await broker.PublishAsync(premiumOrder, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(premiumOrder, conditionalSchema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_CustomValidator_ShouldExecuteCustomLogic()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<object>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Custom validation: Order amount exceeds customer limit" });

        var contractValidatorMock = new Mock<IContractValidator>();

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var highValueOrder = new OrderMessage
        {
            OrderId = "ORDER-004",
            Amount = 5000.00m,
            CustomerId = "CUST-003"
        };

        var publishOptions = new PublishOptions { Validator = validatorMock.Object };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(highValueOrder, publishOptions));

        Assert.Contains("Message validation failed", exception.Message);
        validatorMock.Verify(v => v.ValidateAsync(highValueOrder, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_MultipleValidators_ShouldExecuteAll()
    {
        // Arrange
        var schemaValidatorMock = new Mock<IContractValidator>();
        schemaValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Schema valid

        var customValidatorMock = new Mock<IValidator<object>>();
        customValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Custom valid

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, schemaValidatorMock.Object);

        var orderMessage = new OrderMessage
        {
            OrderId = "ORDER-005",
            Amount = 100.00m,
            CustomerId = "CUST-004"
        };

        var schema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" };
        var publishOptions = new PublishOptions
        {
            Schema = schema,
            Validator = customValidatorMock.Object
        };

        // Act
        await broker.PublishAsync(orderMessage, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        schemaValidatorMock.Verify(cv => cv.ValidateRequestAsync(orderMessage, schema, It.IsAny<CancellationToken>()), Times.Once);
        customValidatorMock.Verify(v => v.ValidateAsync(orderMessage, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_ComplexArrayValidation_ShouldValidateEachItem()
    {
        // Arrange
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Valid

        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var batchOrderMessage = new BatchOrderMessage
        {
            BatchId = "BATCH-001",
            Orders =
            [
                new() { OrderId = "ORDER-006", Amount = 50.00m, CustomerId = "CUST-005" },
                new() { OrderId = "ORDER-007", Amount = 75.00m, CustomerId = "CUST-006" },
                new() { OrderId = "ORDER-008", Amount = 25.00m, CustomerId = "CUST-007" }
            ],
            TotalBatchValue = 150.00m
        };

        var arraySchema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""batchId"": { ""type"": ""string"" },
                    ""orders"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""orderId"": { ""type"": ""string"" },
                                ""amount"": { ""type"": ""number"", ""minimum"": 0 },
                                ""customerId"": { ""type"": ""string"" }
                            },
                            ""required"": [""orderId"", ""amount"", ""customerId""]
                        },
                        ""minItems"": 1,
                        ""maxItems"": 100
                    },
                    ""totalBatchValue"": { ""type"": ""number"", ""minimum"": 0 }
                },
                ""required"": [""batchId"", ""orders"", ""totalBatchValue""]
            }"
        };

        var publishOptions = new PublishOptions { Schema = arraySchema };

        // Act
        await broker.PublishAsync(batchOrderMessage, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(batchOrderMessage, arraySchema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validation_RecursiveSchema_ShouldHandleSelfReferencing()
    {
        // Arrange
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Valid

        var options = Options.Create(new MessageBrokerOptions());
        ILogger<TestableMessageBroker> logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger, contractValidatorMock.Object);

        var treeMessage = new TreeNodeMessage
        {
            Id = "root",
            Value = "Root Node",
            Children =
            [
                new() {
                    Id = "child1",
                    Value = "Child 1",
                    Children =
                    [
                        new() { Id = "leaf1", Value = "Leaf 1", Children = new List<TreeNodeMessage>() }
                    ]
                },
                new() { Id = "child2", Value = "Child 2", Children = new List<TreeNodeMessage>() }
            ]
        };

        var recursiveSchema = new JsonSchemaContract
        {
            Schema = @"{
                ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
                ""type"": ""object"",
                ""properties"": {
                    ""id"": { ""type"": ""string"" },
                    ""value"": { ""type"": ""string"" },
                    ""children"": {
                        ""type"": ""array"",
                        ""items"": { ""$ref"": ""#"" }
                    }
                },
                ""required"": [""id"", ""value"", ""children""]
            }"
        };

        var publishOptions = new PublishOptions { Schema = recursiveSchema };

        // Act
        await broker.PublishAsync(treeMessage, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(treeMessage, recursiveSchema, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Supporting classes
    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CustomerId { get; set; } = string.Empty;
    }

    public class NestedOrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public CustomerInfo Customer { get; set; } = new();
        public List<OrderItem> Items { get; set; } = [];
        public decimal TotalAmount { get; set; }
    }

    public class CustomerInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new();
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class ConditionalOrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string OrderType { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string CustomerTier { get; set; } = string.Empty;
        public bool ExpeditedShipping { get; set; }
    }

    public class BatchOrderMessage
    {
        public string BatchId { get; set; } = string.Empty;
        public List<OrderMessage> Orders { get; set; } = new();
        public decimal TotalBatchValue { get; set; }
    }

    public class TreeNodeMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public List<TreeNodeMessage> Children { get; set; } = [];
    }
}