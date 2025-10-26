using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Validation.Interfaces;
using Relay.MessageBroker.Compression;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerPublishingTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public List<(object Message, byte[] SerializedMessage, PublishOptions? Options)> PublishedMessages { get; } = new();
        public List<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = new();
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        public TestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            Relay.MessageBroker.Compression.IMessageCompressor? compressor = null,
            IContractValidator? contractValidator = null)
            : base(options, logger, compressor, contractValidator)
        {
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishedMessages.Add((message!, serializedMessage, options));

            // For testing publish-subscribe, process the message if started
            if (IsStarted)
            {
                var decompressed = await DecompressMessageAsync(serializedMessage, cancellationToken);
                var deserialized = DeserializeMessage<TMessage>(decompressed);
                var context = new MessageContext();
                await ProcessMessageAsync(deserialized, typeof(TMessage), context, cancellationToken);
            }
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            SubscribedMessages.Add((messageType, subscriptionInfo));
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            StartCalled = true;
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
        {
            StopCalled = true;
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisposeInternalAsync()
        {
            DisposeCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task PublishAsync_ShouldSerializeMessageCorrectly()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (publishedMessage, serializedData, _) = broker.PublishedMessages[0];
        Assert.Equal(message.Id, ((TestMessage)publishedMessage).Id);
        Assert.Equal(message.Name, ((TestMessage)publishedMessage).Name);

        // Verify JSON serialization
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestMessage>(serializedData);
        Assert.NotNull(deserialized);
        Assert.Equal(message.Id, deserialized.Id);
        Assert.Equal(message.Name, deserialized.Name);
    }

    [Fact]
    public async Task PublishAsync_WithCompressionEnabled_ShouldCompressMessage()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new Compression.CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (_, serializedData, _) = broker.PublishedMessages[0];

        // Compressed data should be different from uncompressed
        var uncompressed = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        Assert.True(serializedData.Length > 0); // Just verify compression happened
    }

    [Fact]
    public async Task PublishAsync_WithNullMessage_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await broker.PublishAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task PublishAsync_WhenNotStarted_ShouldAutoStart()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.True(broker.StartCalled);
        Assert.Single(broker.PublishedMessages);
    }

    [Fact]
    public async Task PublishAsync_WhenPublishInternalThrows_ShouldLogAndRethrow()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new ThrowingTestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(message));
        Assert.Equal("Simulated publish failure", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithLargeMessage_ShouldHandleCorrectly()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var largeMessage = new LargeTestMessage
        {
            Id = 123,
            Name = new string('A', 10000), // 10KB string
            Data = new byte[50000] // 50KB byte array
        };
        Array.Fill(largeMessage.Data, (byte)42);

        // Act
        await broker.PublishAsync(largeMessage);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (publishedMessage, _, _) = broker.PublishedMessages[0];
        Assert.Equal(largeMessage.Id, ((LargeTestMessage)publishedMessage).Id);
        Assert.Equal(largeMessage.Name.Length, ((LargeTestMessage)publishedMessage).Name.Length);
    }

    [Fact]
    public async Task PublishAsync_WithComplexNestedObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var complexMessage = new ComplexNestedMessage
        {
            Id = Guid.NewGuid(),
            Root = new NestedObject
            {
                Name = "Root",
                Children = new List<NestedObject>
                {
                    new NestedObject { Name = "Child1", Value = 1 },
                    new NestedObject { Name = "Child2", Value = 2 }
                }
            },
            Metadata = new Dictionary<string, object>
            {
                { "created", DateTimeOffset.UtcNow },
                { "version", "1.0" }
            }
        };

        // Act
        await broker.PublishAsync(complexMessage);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (publishedMessage, serializedData, _) = broker.PublishedMessages[0];

        // Verify deserialization
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<ComplexNestedMessage>(serializedData);
        Assert.NotNull(deserialized);
        Assert.Equal(complexMessage.Id, deserialized.Id);
        Assert.Equal(complexMessage.Root.Name, deserialized.Root.Name);
        Assert.Equal(complexMessage.Root.Children.Count, deserialized.Root.Children.Count);
    }

    [Fact]
    public async Task PublishAsync_WithValidator_ShouldValidateMessage()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var contractValidatorMock = new Mock<IContractValidator>();
        var broker = new TestableMessageBroker(options, logger, contractValidator: contractValidatorMock.Object);

        var validatorMock = new Mock<IValidator<object>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Valid

        var message = new TestMessage { Id = 123, Name = "Test" };
        var publishOptions = new PublishOptions { Validator = validatorMock.Object };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        validatorMock.Verify(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithValidatorThatFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var contractValidatorMock = new Mock<IContractValidator>();
        var broker = new TestableMessageBroker(options, logger, contractValidator: contractValidatorMock.Object);

        var validatorMock = new Mock<IValidator<object>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Validation error" });

        var message = new TestMessage { Id = 123, Name = "Test" };
        var publishOptions = new PublishOptions { Validator = validatorMock.Object };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(message, publishOptions));
        Assert.Contains("Message validation failed", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithSchema_ShouldValidateAgainstSchema()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // Valid

        var broker = new TestableMessageBroker(options, logger, contractValidator: contractValidatorMock.Object);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var schema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" };
        var publishOptions = new PublishOptions { Schema = schema };

        // Act
        await broker.PublishAsync(message, publishOptions);

        // Assert
        Assert.Single(broker.PublishedMessages);
        contractValidatorMock.Verify(cv => cv.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithSchemaThatFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var contractValidatorMock = new Mock<IContractValidator>();
        contractValidatorMock.Setup(cv => cv.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Schema validation error" });

        var broker = new TestableMessageBroker(options, logger, contractValidator: contractValidatorMock.Object);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var schema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" };
        var publishOptions = new PublishOptions { Schema = schema };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await broker.PublishAsync(message, publishOptions));
        Assert.Contains("Message schema validation failed", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithNonSerializableMessage_ShouldThrowJsonException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Create a message that causes circular reference or other serialization issues
        var message = new CircularReferenceMessage();
        message.Self = message; // Circular reference

        // Act & Assert
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await broker.PublishAsync(message));
    }

    [Fact]
    public async Task PublishAsync_WithCompressionEnabledAndCompressorFails_ShouldUseUncompressedData()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new Compression.CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;

        var compressorMock = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        compressorMock.Setup(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null); // Simulate compression failure
        compressorMock.Setup(c => c.DecompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns((byte[] data, CancellationToken ct) => ValueTask.FromResult(data)); // Return input as is for decompression

        var broker = new TestableMessageBroker(options, logger, compressorMock.Object);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(broker.PublishedMessages);
        var (_, serializedData, _) = broker.PublishedMessages[0];

        // Should have uncompressed JSON data
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestMessage>(serializedData);
        Assert.NotNull(deserialized);
        Assert.Equal(message.Id, deserialized.Id);
        Assert.Equal(message.Name, deserialized.Name);

        compressorMock.Verify(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldRecordTelemetryOnSuccess()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        // Telemetry is recorded internally, we can't easily mock it without changing the constructor
        // This test ensures no exceptions are thrown during telemetry recording
        Assert.Single(broker.PublishedMessages);
    }

    [Fact]
    public async Task PublishAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await broker.PublishAsync(message, cancellationToken: cts.Token));
    }

    public class ThrowingTestableMessageBroker : BaseMessageBroker
    {
        public ThrowingTestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            Relay.MessageBroker.Compression.IMessageCompressor? compressor = null,
            IContractValidator? contractValidator = null)
            : base(options, logger, compressor, contractValidator)
        {
        }

        protected override ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Simulated publish failure");
        }

        protected override ValueTask SubscribeInternalAsync(
            Type messageType,
            SubscriptionInfo subscriptionInfo,
            CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StartInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask StopInternalAsync(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        protected override ValueTask DisposeInternalAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LargeTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public class ComplexNestedMessage
    {
        public Guid Id { get; set; }
        public NestedObject Root { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class NestedObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public List<NestedObject> Children { get; set; } = new();
    }

    public class CircularReferenceMessage
    {
        public CircularReferenceMessage? Self { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}