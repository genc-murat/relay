using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core;
using Relay.MessageBroker.Compression;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BaseMessageBrokerLifecycleTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public List<(object Message, byte[] SerializedMessage, PublishOptions? Options)> PublishedMessages { get; } = new();
        public List<(Type MessageType, SubscriptionInfo SubscriptionInfo)> SubscribedMessages { get; } = new();
        public bool StartCalled { get; private set; }
        public bool StopCalled { get; private set; }
        public bool DisposeCalled { get; private set; }

        // Expose protected properties for testing
        public new bool IsStarted => base.IsStarted;
        public new bool IsDisposed => base.IsDisposed;

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

    [Fact]
    public async Task IsStarted_ShouldReturnFalseInitially()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act & Assert
        Assert.False(broker.IsStarted);
    }

    [Fact]
    public async Task IsStarted_ShouldReturnTrueAfterStart()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.StartAsync();

        // Assert
        Assert.True(broker.IsStarted);
    }

    [Fact]
    public async Task IsStarted_ShouldReturnFalseAfterStop()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        await broker.StartAsync();

        // Act
        await broker.StopAsync();

        // Assert
        Assert.False(broker.IsStarted);
    }

    [Fact]
    public async Task IsDisposed_ShouldReturnFalseInitially()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act & Assert
        Assert.False(broker.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_ShouldReturnTrueAfterDispose()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions());
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var broker = new TestableMessageBroker(options, logger);

        // Act
        await broker.DisposeAsync();

        // Assert
        Assert.True(broker.IsDisposed);
    }
}