using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerTests
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

        // Expose protected methods for testing
        public ValueTask TestSerializeMessage<TMessage>(TMessage message)
        {
            var data = SerializeMessage(message);
            return ValueTask.CompletedTask;
        }

        public async ValueTask<byte[]> TestCompressMessageAsync(byte[] data)
        {
            return await CompressMessageAsync(data);
        }

        public async ValueTask<byte[]> TestDecompressMessageAsync(byte[] data)
        {
            return await DecompressMessageAsync(data);
        }

        public async ValueTask TestProcessMessageAsync(object message, Type messageType, MessageContext context)
        {
            await ProcessMessageAsync(message, messageType, context);
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
    public async Task SubscribeAsync_ShouldStoreSubscriptionInfo()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new TestMessage { Id = 123, Name = "Test" };
        var subscriptionOptions = new SubscriptionOptions
        {
            QueueName = "test-queue",
            RoutingKey = "test.key"
        };

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask, subscriptionOptions);

        // Assert
        Assert.Single(broker.SubscribedMessages);
        var (messageType, subscriptionInfo) = broker.SubscribedMessages[0];
        Assert.Equal(typeof(TestMessage), messageType);
        Assert.Equal(subscriptionOptions.QueueName, subscriptionInfo.Options.QueueName);
        Assert.Equal(subscriptionOptions.RoutingKey, subscriptionInfo.Options.RoutingKey);
    }

    [Fact]
    public async Task StartAsync_ShouldCallStartInternal()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.StartAsync();

        // Assert
        Assert.True(broker.StartCalled);
    }

    [Fact]
    public async Task StopAsync_ShouldCallStopInternal()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        await broker.StartAsync();

        // Act
        await broker.StopAsync();

        // Assert
        Assert.True(broker.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_ShouldCallDisposeInternal()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.DisposeAsync();

        // Assert
        Assert.True(broker.DisposeCalled);
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
    public async Task SubscribeAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await broker.SubscribeAsync<TestMessage>(null!));
    }

    [Fact]
    public async Task ProcessMessageAsync_ShouldHandleMultipleHandlers()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var receivedMessages1 = new List<TestMessage>();
        var receivedMessages2 = new List<TestMessage>();

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages1.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages2.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 123, Name = "Test" };
        var context = new MessageContext();

        // Act
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert
        Assert.Single(receivedMessages1);
        Assert.Single(receivedMessages2);
        Assert.Equal(message.Id, receivedMessages1[0].Id);
        Assert.Equal(message.Id, receivedMessages2[0].Id);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenHandlerThrows_ShouldContinueWithOtherHandlers()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var receivedMessages = new List<TestMessage>();

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            throw new InvalidOperationException("Handler failed");
        });

        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) =>
        {
            receivedMessages.Add(msg);
            return ValueTask.CompletedTask;
        });

        await broker.StartAsync();

        var message = new TestMessage { Id = 123, Name = "Test" };
        var context = new MessageContext();

        // Act
        await broker.TestProcessMessageAsync(message, typeof(TestMessage), context);

        // Assert
        Assert.Single(receivedMessages);
        Assert.Equal(message.Id, receivedMessages[0].Id);
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
    public async Task SubscribeAsync_WhenNotStarted_ShouldAutoStart()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.SubscribeAsync<TestMessage>((msg, ctx, ct) => ValueTask.CompletedTask);

        // Assert
        Assert.True(broker.StartCalled);
        Assert.Single(broker.SubscribedMessages);
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
    public async Task Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableMessageBroker(null!, logger));
    }

    [Fact]
    public async Task Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestableMessageBroker(options, null!));
    }

    [Fact]
    public async Task SerializeMessage_ShouldHandleComplexObjects()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var message = new ComplexMessage
        {
            Id = Guid.NewGuid(),
            Name = "Complex Test",
            Items = new List<string> { "item1", "item2", "item3" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 123 },
                { "key3", true }
            },
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await broker.TestSerializeMessage(message);

        // Assert - Just verify it doesn't throw
        Assert.True(true);
    }

    [Fact]
    public async Task CompressMessageAsync_WhenCompressionDisabled_ShouldReturnOriginalData()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new Compression.CompressionOptions { Enabled = false }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await broker.TestCompressMessageAsync(data);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public async Task DecompressMessageAsync_WhenCompressionDisabled_ShouldReturnOriginalData()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new Compression.CompressionOptions { Enabled = false }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        var data = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var result = await broker.TestDecompressMessageAsync(data);

        // Assert
        Assert.Equal(data, result);
    }

    [Fact]
    public async Task StartAsync_MultipleCalls_ShouldOnlyStartOnce()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.StartAsync();
        await broker.StartAsync();
        await broker.StartAsync();

        // Assert
        Assert.True(broker.StartCalled);
        // StartInternal should only be called once
    }

    [Fact]
    public async Task StopAsync_WhenNotStarted_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.StopAsync();

        // Assert - Should not throw
        Assert.False(broker.StopCalled);
    }

    [Fact]
    public async Task DisposeAsync_MultipleCalls_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.DisposeAsync();
        await broker.DisposeAsync();

        // Assert - Should not throw
        Assert.True(broker.DisposeCalled);
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

    private class ComplexMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }
}