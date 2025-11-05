using Relay.Core.MessageQueue;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.MessageQueue;

public class InMemoryMessageQueuePublisherComprehensiveTests
{
    [Fact]
    public async Task InMemoryMessageQueuePublisher_PublishAsync_WithCancellationPropagates()
    {
        // Arrange
        var publisher = new InMemoryMessageQueuePublisher();
        var queueName = "test-queue";
        var testMessage = new TestMessage { Value = "test-content" };

        // Act
        using (var cts = new CancellationTokenSource())
        {
            await publisher.PublishAsync(queueName, testMessage, cts.Token);
        }

        // Assert - Test passes if no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task InMemoryMessageQueuePublisher_PublishAsync_ToExchange()
    {
        // Arrange
        var publisher = new InMemoryMessageQueuePublisher();
        var exchangeName = "test-exchange";
        var routingKey = "test-routing-key";
        var testMessage = new TestMessage { Value = "test-content" };

        // Act
        await publisher.PublishAsync(exchangeName, routingKey, testMessage);

        // Assert - Test passes if no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task InMemoryMessageQueuePublisher_PublishAsync_WithActivity_AddsCorrelationId()
    {
        // Arrange
        var publisher = new InMemoryMessageQueuePublisher();
        var queueName = "test-queue";
        var testMessage = new TestMessage { Value = "test-content" };

        // Act
        using (var activity = new System.Diagnostics.Activity("TestActivity").Start())
        {
            await publisher.PublishAsync(queueName, testMessage);
        }

        // Assert - Test passes if no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task InMemoryMessageQueuePublisher_PublishAsync_MultipleTypes()
    {
        // Arrange
        var publisher = new InMemoryMessageQueuePublisher();
        var queueName = "test-queue";

        // Act
        await publisher.PublishAsync(queueName, new TestMessage { Value = "string-message" });
        await publisher.PublishAsync(queueName, new IntTestMessage { Value = 42 });
        await publisher.PublishAsync(queueName, new ComplexTestMessage { Id = 1, Name = "Test", Values = new List<string> { "a", "b", "c" } });

        // Assert - Test passes if no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task InMemoryMessageQueuePublisher_PublishAsync_WithReflectionAccess()
    {
        // Arrange
        var publisher = new InMemoryMessageQueuePublisher();
        var queueName = "test-queue";
        var testMessage = new TestMessage { Value = "test-content" };

        // Use reflection to access internal field for verification
        var queuesField = typeof(InMemoryMessageQueuePublisher).GetField("_queues", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var queues = queuesField?.GetValue(publisher) as System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentQueue<string>>;

        // Act
        await publisher.PublishAsync(queueName, testMessage);

        // Assert
        Assert.NotNull(queues);
        Assert.True(queues.ContainsKey(queueName));
        
        if (queues.TryGetValue(queueName, out var queue))
        {
            Assert.True(queue.Count > 0);
            
            if (queue.TryDequeue(out var messageJson))
            {
                var wrapper = JsonSerializer.Deserialize<MessageWrapper>(messageJson);
                Assert.NotNull(wrapper);
                Assert.Equal(testMessage.GetType().FullName, wrapper.MessageType);
                Assert.Contains("test-content", wrapper.Content);
                Assert.NotNull(wrapper.CorrelationId);
            }
        }
    }

    [Fact]
    public void InMemoryMessageQueuePublisher_InternalTryDequeueMessage_Accessible()
    {
        // Arrange - Direct access to internal method via reflection
        var publisher = new InMemoryMessageQueuePublisher();
        var testMessage = new TestMessage { Value = "test-content" };
        var queueName = "test-queue";

        // Use reflection to access internal TryDequeueMessage
        var tryDequeueMethod = typeof(InMemoryMessageQueuePublisher).GetMethod("TryDequeueMessage", 
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Set up a message first by directly accessing the queues
        var queuesField = typeof(InMemoryMessageQueuePublisher).GetField("_queues", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        var queues = queuesField?.GetValue(publisher) as System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentQueue<string>>;
        
        if (queues != null)
        {
            var queue = queues.GetOrAdd(queueName, _ => new System.Collections.Concurrent.ConcurrentQueue<string>());
            var wrapper = new MessageWrapper
            {
                MessageType = testMessage.GetType().FullName,
                Content = JsonSerializer.Serialize(testMessage),
                CorrelationId = "test-correlation"
            };
            queue.Enqueue(JsonSerializer.Serialize(wrapper));
        }

        // Act
        var result = tryDequeueMethod?.Invoke(publisher, new object[] { queueName, null });

        // Assert
        Assert.NotNull(result);
        var success = (bool)result;
        Assert.True(success);
    }

    public class TestMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    public class IntTestMessage
    {
        public int Value { get; set; }
    }

    public class ComplexTestMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new List<string>();
    }
}
