using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerSerializationTests
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

    private class ComplexMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }
}