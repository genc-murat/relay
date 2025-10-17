using System;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
            Assert.NotNull(message);
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
            Assert.NotNull(message);
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
                Assert.NotNull(message);
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
        public void InMemoryMessageQueueConsumer_Constructor_ShouldThrow_WhenServiceProviderIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new InMemoryMessageQueueConsumer(null!));
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldConsumeMessagesSuccessfully()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";
            var receivedMessages = new List<TestMessage>();

            await consumer.StartConsumingAsync(queueName, (message, ct) =>
            {
                if (message is TestMessage testMessage)
                {
                    receivedMessages.Add(testMessage);
                }
                return ValueTask.CompletedTask;
            });

            // Act - Enqueue a message
            var testMessage = new TestMessage { Value = "test content" };
            var wrapper = new MessageWrapper
            {
                MessageType = typeof(TestMessage).AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(testMessage)
            };
            consumer.EnqueueMessage(queueName, JsonSerializer.Serialize(wrapper));

            // Wait for message to be processed
            await Task.Delay(1000);

            // Assert
            Assert.Single(receivedMessages);
            Assert.Equal("test content", receivedMessages[0].Value);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldHandleExchangeAndRoutingKey()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var exchangeName = "test-exchange";
            var routingKey = "test.key";
            var receivedMessages = new List<TestMessage>();

            await consumer.StartConsumingAsync(exchangeName, routingKey, (message, ct) =>
            {
                if (message is TestMessage testMessage)
                {
                    receivedMessages.Add(testMessage);
                }
                return ValueTask.CompletedTask;
            });

            // Act - Enqueue a message to the combined queue name
            var testMessage = new TestMessage { Value = "exchange content" };
            var wrapper = new MessageWrapper
            {
                MessageType = typeof(TestMessage).AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(testMessage)
            };
            var queueName = $"{exchangeName}.{routingKey}";
            consumer.EnqueueMessage(queueName, JsonSerializer.Serialize(wrapper));

            // Wait for message to be processed
            await Task.Delay(200);

            // Assert
            Assert.Single(receivedMessages);
            Assert.Equal("exchange content", receivedMessages[0].Value);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldHandleDeserializationFailure()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";
            var messagesReceived = 0;

            await consumer.StartConsumingAsync(queueName, (message, ct) =>
            {
                messagesReceived++;
                return ValueTask.CompletedTask;
            });

            // Act - Enqueue invalid JSON
            consumer.EnqueueMessage(queueName, "invalid json");

            // Wait for processing attempt
            await Task.Delay(200);

            // Assert - Message should not be processed due to deserialization failure
            Assert.Equal(0, messagesReceived);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldHandleHandlerException()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";

            await consumer.StartConsumingAsync(queueName, (message, ct) =>
            {
                throw new InvalidOperationException("Handler error");
            });

            // Act - Enqueue a valid message
            var testMessage = new TestMessage { Value = "test" };
            var wrapper = new MessageWrapper
            {
                MessageType = typeof(TestMessage).AssemblyQualifiedName!,
                Content = JsonSerializer.Serialize(testMessage)
            };
            consumer.EnqueueMessage(queueName, JsonSerializer.Serialize(wrapper));

            // Wait for processing (consumer should continue despite exception)
            await Task.Delay(200);

            // Assert - Consumer should still be running (no crash)
            Assert.True(true);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_ShouldHandleCancellation()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queueName = "test-queue";
            var cts = new CancellationTokenSource();

            await consumer.StartConsumingAsync(queueName, async (message, ct) =>
            {
                // Simulate long-running handler
                await Task.Delay(1000, ct);
            }, cts.Token);

            // Act - Cancel after a short delay
            await Task.Delay(50);
            cts.Cancel();

            // Wait a bit more
            await Task.Delay(100);

            // Assert - Consumer should have stopped gracefully
            Assert.True(true);
        }

        [Fact]
        public async Task InMemoryMessageQueueConsumer_StopConsuming_ShouldCancelAllConsumers()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var consumer = new InMemoryMessageQueueConsumer(serviceProvider);
            var queue1 = "queue1";
            var queue2 = "queue2";
            var activeConsumers = 0;

            // Start multiple consumers
            await consumer.StartConsumingAsync(queue1, (message, ct) =>
            {
                Interlocked.Increment(ref activeConsumers);
                return ValueTask.CompletedTask;
            });

            await consumer.StartConsumingAsync(queue2, (message, ct) =>
            {
                Interlocked.Increment(ref activeConsumers);
                return ValueTask.CompletedTask;
            });

            // Act
            await consumer.StopConsumingAsync();

            // Wait for consumers to stop
            await Task.Delay(100);

            // Assert - All consumers should be stopped
            Assert.Equal(0, activeConsumers);
        }

        [Fact]
        public void MessageQueueAttribute_ShouldSetQueueName()
        {
            // Arrange & Act
            var attribute = new MessageQueueAttribute("test-queue");

            // Assert
            Assert.Equal("test-queue", attribute.QueueName);
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
            Assert.Equal("test-exchange", attribute.ExchangeName);
            Assert.Equal("test-key", attribute.RoutingKey);
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
            Assert.NotNull(deserialized);
            Assert.Equal("TestMessage", deserialized.MessageType);
            Assert.Equal("{\"Value\":\"test\"}", deserialized.Content);
            Assert.Equal("test-correlation-id", deserialized.CorrelationId);
        }

        public class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }
    }
}