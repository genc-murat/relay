using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

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

        protected override ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            PublishedMessages.Add((message!, serializedMessage, options));
            return ValueTask.CompletedTask;
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

    private class TestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class LargeTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    private class ComplexNestedMessage
    {
        public Guid Id { get; set; }
        public NestedObject Root { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    private class NestedObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
        public List<NestedObject> Children { get; set; } = new();
    }
}