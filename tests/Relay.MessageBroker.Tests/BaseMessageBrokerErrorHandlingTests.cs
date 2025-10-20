using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerErrorHandlingTests
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
}