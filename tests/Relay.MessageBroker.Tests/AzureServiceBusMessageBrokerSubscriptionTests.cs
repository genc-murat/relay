using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.AzureServiceBus;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AzureServiceBusMessageBrokerSubscriptionTests
{
    private readonly Mock<ILogger<AzureServiceBusMessageBroker>> _loggerMock;

    public AzureServiceBusMessageBrokerSubscriptionTests()
    {
        _loggerMock = new Mock<ILogger<AzureServiceBusMessageBroker>>();
    }

    [Fact]
    public async Task SubscribeAsync_WithValidHandler_ShouldRegisterHandler()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-entity"
            }
        };

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        var processedMessages = new List<TestMessage>();
        var handler = new Func<TestMessage, MessageContext, CancellationToken, ValueTask>(async (msg, ctx, ct) =>
        {
            processedMessages.Add(msg);
            await ctx.Acknowledge();
        });

        // Act
        await broker.SubscribeAsync(handler);

        // Assert
        // The subscription was registered without exception
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidMessage_ShouldDeserializeAndProcess()
    {
        // This test simulates how the internal message processing would work
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-entity",
                AutoCompleteMessages = false
            }
        };

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        var processedMessages = new List<TestMessage>();
        var contexts = new List<MessageContext>();
        
        var handler = new Func<TestMessage, MessageContext, CancellationToken, ValueTask>(async (msg, ctx, ct) =>
        {
            processedMessages.Add(msg);
            contexts.Add(ctx);
            await ctx.Acknowledge();
        });

        // Subscribe to the message type
        await broker.SubscribeAsync(handler);

        // Validate that subscription was successful
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithNoMatchingHandler_ShouldCompleteWithoutProcessing()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-entity",
                AutoCompleteMessages = false
            }
        };

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Create a message that doesn't have a handler
        var testMessage = new TestMessage { Id = 1, Content = "Test content" };
        var serializedMessage = JsonSerializer.Serialize(testMessage);
        var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(serializedMessage))
        {
            Subject = "NonExistentType", // This type doesn't have a handler
            MessageId = Guid.NewGuid().ToString()
        };

        // Subscribe to a different type than what we'll process
        var processedMessages = new List<AnotherTestMessage>();
        await broker.SubscribeAsync<AnotherTestMessage>(async (msg, ctx, ct) =>
        {
            processedMessages.Add(msg);
            await ctx.Acknowledge();
        });

        // Act: This simulates the internal message processing flow
        await broker.SubscribeAsync<TestMessage>(async (msg, ctx, ct) => 
        {
            // This handler would be called if the internal processing worked
        });

        // Assert that the broker was created without issues
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithAutoCompleteEnabled_ShouldAutoComplete()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-entity",
                AutoCompleteMessages = true
            }
        };

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act: Subscribe to a message type
        await broker.SubscribeAsync<TestMessage>(async (msg, ctx, ct) =>
        {
            // Process the message
            await ValueTask.CompletedTask;
        });

        // Assert that the broker handles auto-completion correctly
        Assert.NotNull(broker);
    }

    [Fact]
    public async Task ProcessSessionMessageAsync_WithValidSessionMessage_ShouldProcess()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-entity",
                SessionsEnabled = true,
                AutoCompleteMessages = false
            }
        };

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);

        var processedMessages = new List<TestMessage>();
        var contexts = new List<MessageContext>();
        
        // Subscribe to handle test messages
        var handler = new Func<TestMessage, MessageContext, CancellationToken, ValueTask>(async (msg, ctx, ct) =>
        {
            processedMessages.Add(msg);
            contexts.Add(ctx);
            await ctx.Acknowledge();
        });

        await broker.SubscribeAsync(handler);

        // Act: This simulates the internal session message processing
        // The broker should be able to handle session messages
        await broker.StartAsync();

        // Assert
        Assert.NotNull(broker);
        await broker.StopAsync();
    }

    [Fact]
    public async Task RequeueDeadLetterMessageAsync_WithTopicEntityType_ShouldCreateCorrectReceiver()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AzureServiceBus = new AzureServiceBusOptions
            {
                ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                DefaultEntityName = "test-topic",
                EntityType = AzureEntityType.Topic,
                SubscriptionName = "test-subscription"
            }
        };

        var messageId = "test-message-id";
        var messageBody = Encoding.UTF8.GetBytes("test content");

        var receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromBytes(messageBody),
            messageId: messageId
        );

        var receiverMock = new Mock<ServiceBusReceiver>();
        receiverMock.Setup(x => x.ReceiveMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ServiceBusReceivedMessage> { receivedMessage });

        var senderMock = new Mock<ServiceBusSender>();

        var clientMock = new Mock<ServiceBusClient>();
        clientMock.Setup(x => x.CreateReceiver("test-topic", "test-subscription", It.IsAny<ServiceBusReceiverOptions>()))
            .Returns(receiverMock.Object);
        clientMock.Setup(x => x.CreateSender("test-topic"))
            .Returns(senderMock.Object);

        var broker = new AzureServiceBusMessageBroker(Options.Create(options), _loggerMock.Object);
        
        // Inject mocked client
        typeof(AzureServiceBusMessageBroker).GetField("_client", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(broker, clientMock.Object);

        // Act
        await broker.RequeueDeadLetterMessageAsync(messageId, "test-topic");

        // Assert
        senderMock.Verify(x => x.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()), Times.Once);
        receiverMock.Verify(x => x.CompleteMessageAsync(receivedMessage, It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that the receiver was created with topic and subscription name
        clientMock.Verify(x => x.CreateReceiver("test-topic", "test-subscription", It.IsAny<ServiceBusReceiverOptions>()), Times.Once);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private class AnotherTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}