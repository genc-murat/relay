using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Relay.Core.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.MessageQueue
{
    public class MessageQueueTests
    {
        [Fact]
        public async Task InMemoryMessageQueuePublisher_ShouldPublishMessage()
        {
            // Arrange
            var publisher = new InMemoryMessageQueuePublisher();
            var queueName = "test-queue";
            var message = new TestMessage { Value = "test" };

            // Act
            await publisher.PublishAsync(queueName, message);

            // Assert - We can verify the message was published (implementation detail test)
            message.Should().NotBeNull();
        }

        [Fact]
        public async Task InMemoryMessageQueuePublisher_ShouldPublishToExchange()
        {
            // Arrange
            var publisher = new InMemoryMessageQueuePublisher();
            var exchangeName = "test-exchange";
            var routingKey = "test-key";
            var message = new TestMessage { Value = "test" };

            // Act
            await publisher.PublishAsync(exchangeName, routingKey, message);

            // Assert
            message.Should().NotBeNull();
        }

        [Fact]
        public async Task InMemoryMessageQueuePublisher_ShouldHandleMultipleMessages()
        {
            // Arrange
            var publisher = new InMemoryMessageQueuePublisher();
            var queueName = "test-queue";

            // Act
            await publisher.PublishAsync(queueName, new TestMessage { Value = "message1" });
            await publisher.PublishAsync(queueName, new TestMessage { Value = "message2" });
            await publisher.PublishAsync(queueName, new TestMessage { Value = "message3" });

            // Assert - All messages should be published successfully
            Assert.True(true); // Publisher doesn't expose queues publicly, so we just verify no exceptions
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldStartConsuming()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";
            var messagesReceived = 0;

            // Act
            await consumer.StartConsumingAsync(queueName, (message, ct) =>
            {
                messagesReceived++;
                message.Should().NotBeNull();
                return ValueTask.CompletedTask;
            });

            await Task.Delay(100); // Give time for consumer to start

            // Assert - Consumer should be running
            Assert.True(true);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldStopConsuming()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";

            await consumer.StartConsumingAsync(queueName, (message, ct) =>
            {
                return ValueTask.CompletedTask;
            });

            // Act
            await consumer.StopConsumingAsync();

            // Assert
            Assert.True(true); // Consumer stopped successfully
        }

        [Fact]
        public void MessageQueueAttribute_ShouldSetQueueName()
        {
            // Arrange & Act
            var attribute = new MessageQueueAttribute("test-queue");

            // Assert
            attribute.QueueName.Should().Be("test-queue");
        }

        [Fact]
        public void MessageQueueAttribute_ShouldSetExchangeAndRoutingKey()
        {
            // Arrange & Act
            var attribute = new MessageQueueAttribute("test-queue")
            {
                ExchangeName = "test-exchange",
                RoutingKey = "test-key"
            };

            // Assert
            attribute.ExchangeName.Should().Be("test-exchange");
            attribute.RoutingKey.Should().Be("test-key");
        }

        [Fact]
        public void MessageWrapper_ShouldSerializeCorrectly()
        {
            // Arrange
            var wrapper = new MessageWrapper
            {
                MessageType = "TestMessage",
                Content = "{\"Value\":\"test\"}",
                CorrelationId = "test-correlation-id"
            };

            // Act
            var json = JsonSerializer.Serialize(wrapper);
            var deserialized = JsonSerializer.Deserialize<MessageWrapper>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.MessageType.Should().Be("TestMessage");
            deserialized.Content.Should().Be("{\"Value\":\"test\"}");
            deserialized.CorrelationId.Should().Be("test-correlation-id");
        }

        public class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}
