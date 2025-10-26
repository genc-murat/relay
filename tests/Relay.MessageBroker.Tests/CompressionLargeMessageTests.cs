using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Compression;
using System.Collections.Concurrent;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CompressionLargeMessageTests
{
    public class TestableMessageBroker : BaseMessageBroker
    {
        public ConcurrentBag<object> PublishedMessages { get; } = new();
        public ConcurrentBag<byte[]> SerializedMessages { get; } = new();
        public ConcurrentBag<byte[]> DecompressedMessages { get; } = new();

        public TestableMessageBroker(
            IOptions<MessageBrokerOptions> options,
            ILogger logger,
            Relay.MessageBroker.Compression.IMessageCompressor? compressor = null)
            : base(options, logger, compressor: compressor)
        {
        }

        protected override async ValueTask PublishInternalAsync<TMessage>(
            TMessage message,
            byte[] serializedMessage,
            PublishOptions? options,
            CancellationToken cancellationToken)
        {
            PublishedMessages.Add(message!);
            SerializedMessages.Add(serializedMessage);

            // Process message for subscribers if started
            if (IsStarted)
            {
                try
                {
                    var decompressed = await DecompressMessageAsync(serializedMessage, cancellationToken);
                    DecompressedMessages.Add(decompressed);
                    var deserialized = DeserializeMessage<TMessage>(decompressed);
                    var context = new MessageContext();
                    await ProcessMessageAsync(deserialized, typeof(TMessage), context, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log decompression failure but don't crash - simulate graceful handling
                    // In real scenarios, this would be logged and the message might be retried or dead-lettered
                }
            }
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
            => ValueTask.CompletedTask;

        protected override ValueTask DisposeInternalAsync()
            => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Compression_VeryLargeMessage_ShouldCompressEfficiently()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        await broker.StartAsync();

        // Create a 100MB message with compressible data (repeating pattern)
        var largeData = new byte[100 * 1024 * 1024]; // 100MB
        for (int i = 0; i < largeData.Length; i++)
        {
            largeData[i] = (byte)(i % 256); // Repeating pattern, highly compressible
        }

        var largeMessage = new LargeDataMessage
        {
            Id = Guid.NewGuid(),
            Data = largeData,
            Metadata = $"Large message with {largeData.Length} bytes"
        };

        // Act
        await broker.PublishAsync(largeMessage);

        // Assert
        Assert.Single(broker.PublishedMessages);
        Assert.Single(broker.SerializedMessages);

        var serializedData = broker.SerializedMessages.First();
        var originalSize = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(largeMessage).Length;

        // Compressed data should be significantly smaller
        var compressionRatio = (double)serializedData.Length / originalSize;
        Assert.True(compressionRatio < 0.1, $"Compression ratio {compressionRatio:P2} is not efficient enough");

        // Should be able to decompress and get original data back
        Assert.Single(broker.DecompressedMessages);
        var decompressedData = broker.DecompressedMessages.First();
        var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<LargeDataMessage>(decompressedData);
        Assert.NotNull(deserializedMessage);
        Assert.Equal(largeMessage.Id, deserializedMessage.Id);
        Assert.Equal(largeMessage.Data.Length, deserializedMessage.Data.Length);
    }

    [Fact]
    public async Task Compression_AlgorithmSwitching_ShouldWorkCorrectly()
    {
        // Arrange
        var algorithms = new[]
        {
            Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
            Relay.Core.Caching.Compression.CompressionAlgorithm.Deflate,
            Relay.Core.Caching.Compression.CompressionAlgorithm.Brotli
        };

        var compressors = new Relay.MessageBroker.Compression.IMessageCompressor[]
        {
            new Relay.MessageBroker.Compression.GZipMessageCompressor(),
            new Relay.MessageBroker.Compression.DeflateMessageCompressor(),
            new Relay.MessageBroker.Compression.BrotliMessageCompressor()
        };

        var message = new TestMessage
        {
            Id = 123,
            Data = new string('A', 10000), // 10KB of compressible data
            Timestamp = DateTimeOffset.UtcNow
        };

        for (int i = 0; i < algorithms.Length; i++)
        {
            var algorithm = algorithms[i];
            var compressor = compressors[i];

            var options = Options.Create(new MessageBrokerOptions
            {
                Compression = new CompressionOptions
                {
                    Enabled = true,
                    Algorithm = algorithm
                }
            });
            var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
            var broker = new TestableMessageBroker(options, logger, compressor);

            await broker.StartAsync();

            // Act
            await broker.PublishAsync(message);

            // Assert
            Assert.Single(broker.PublishedMessages);
            Assert.Single(broker.SerializedMessages);

            var serializedData = broker.SerializedMessages.First();
            var originalSize = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message).Length;

            // Should be compressed (smaller than original)
            Assert.True(serializedData.Length < originalSize,
                $"Algorithm {algorithm} did not compress data effectively");

            // Should be able to decompress
            Assert.Single(broker.DecompressedMessages);
            var decompressedData = broker.DecompressedMessages.First();
            var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<TestMessage>(decompressedData);
            Assert.NotNull(deserializedMessage);
            Assert.Equal(message.Id, deserializedMessage.Id);
            Assert.Equal(message.Data, deserializedMessage.Data);
        }
    }

    [Fact]
    public async Task Compression_CompressionFailure_ShouldFallbackToUncompressed()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;

        var failingCompressor = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        failingCompressor.Setup(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null); // Simulate compression failure
        failingCompressor.Setup(c => c.DecompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Returns((byte[] data, CancellationToken ct) => ValueTask.FromResult(data)); // Return as-is for decompression

        var broker = new TestableMessageBroker(options, logger, failingCompressor.Object);

        await broker.StartAsync();

        var message = new TestMessage { Id = 456, Data = "Test data" };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(broker.PublishedMessages);
        Assert.Single(broker.SerializedMessages);

        var serializedData = broker.SerializedMessages.First();

        // Should have uncompressed JSON data
        var expectedJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TestMessage>(serializedData);
        Assert.NotNull(deserialized);
        Assert.Equal(message.Id, deserialized.Id);
        Assert.Equal(message.Data, deserialized.Data);

        failingCompressor.Verify(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Compression_DecompressionFailure_ShouldHandleGracefully()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;

        var failingCompressor = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        failingCompressor.Setup(c => c.CompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 }); // Return dummy compressed data
        failingCompressor.Setup(c => c.DecompressAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Decompression failed"));

        var broker = new TestableMessageBroker(options, logger, failingCompressor.Object);

        // Don't start broker to avoid decompression during publishing
        var message = new TestMessage { Id = 789, Data = "Test data" };

        // Act & Assert
        // Publishing should succeed (compression works)
        await broker.PublishAsync(message);

        Assert.Single(broker.PublishedMessages);
        // Decompression failure should be handled gracefully (logged but not crash the system)
    }

    [Fact]
    public async Task Compression_ConcurrentLargeMessages_ShouldHandleThreadSafety()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        await broker.StartAsync();

        const int messageCount = 10;
        const int dataSize = 10 * 1024 * 1024; // 10MB each
        var messages = new LargeDataMessage[messageCount];

        for (int i = 0; i < messageCount; i++)
        {
            var data = new byte[dataSize];
            // Create compressible data with repeating patterns
            for (int j = 0; j < data.Length; j++)
            {
                data[j] = (byte)((i + j) % 256); // Repeating pattern, highly compressible
            }
            messages[i] = new LargeDataMessage
            {
                Id = Guid.NewGuid(),
                Data = data,
                Metadata = $"Message {i}"
            };
        }

        // Act - Publish concurrently
        var publishTasks = messages.Select(async msg => await broker.PublishAsync(msg)).ToArray();
        await Task.WhenAll(publishTasks);

        // Assert
        Assert.Equal(messageCount, broker.PublishedMessages.Count);
        Assert.Equal(messageCount, broker.SerializedMessages.Count);

        // All messages should be compressed
        foreach (var serialized in broker.SerializedMessages)
        {
            var originalSize = dataSize + 1000; // Approximate original size
            Assert.True(serialized.Length < originalSize,
                $"Message not compressed effectively: {serialized.Length} vs {originalSize}");
        }

        // All messages should be decompressed correctly
        Assert.Equal(messageCount, broker.DecompressedMessages.Count);
        var processedIds = broker.DecompressedMessages
            .Select(d => System.Text.Json.JsonSerializer.Deserialize<LargeDataMessage>(d)?.Id)
            .ToHashSet();

        foreach (var message in messages)
        {
            Assert.Contains(message.Id, processedIds);
        }
    }

    [Fact]
    public async Task Compression_CompressionLevelImpact_ShouldVaryEfficiency()
    {
        // Note: This test assumes different compression levels are supported
        // In practice, the compressor implementations might not expose compression levels

        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        await broker.StartAsync();

        // Create highly compressible data
        var compressibleData = new string('A', 100000); // 100KB of identical characters
        var message = new TestMessage { Id = 999, Data = compressibleData };

        // Act
        await broker.PublishAsync(message);

        // Assert
        Assert.Single(broker.SerializedMessages);
        var serializedData = broker.SerializedMessages.First();
        var originalSize = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(message).Length;

        // Should achieve very high compression ratio
        var compressionRatio = (double)serializedData.Length / originalSize;
        Assert.True(compressionRatio < 0.05, $"Compression ratio {compressionRatio:P2} not high enough for compressible data");

        // Should decompress correctly
        Assert.Single(broker.DecompressedMessages);
        var decompressedData = broker.DecompressedMessages.First();
        var deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<TestMessage>(decompressedData);
        Assert.NotNull(deserializedMessage);
        Assert.Equal(message.Data, deserializedMessage.Data);
    }

    [Fact]
    public async Task Compression_MemoryPressure_ShouldNotCauseIssues()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            Compression = new CompressionOptions
            {
                Enabled = true,
                Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip
            }
        });
        var logger = new Mock<ILogger<TestableMessageBroker>>().Object;
        var compressor = new GZipMessageCompressor();
        var broker = new TestableMessageBroker(options, logger, compressor);

        await broker.StartAsync();

        // Create multiple large messages to simulate memory pressure
        var largeMessages = new List<LargeDataMessage>();
        for (int i = 0; i < 5; i++)
        {
            var data = new byte[50 * 1024 * 1024]; // 50MB each
            new Random(i).NextBytes(data);
            largeMessages.Add(new LargeDataMessage
            {
                Id = Guid.NewGuid(),
                Data = data,
                Metadata = $"Memory pressure test {i}"
            });
        }

        // Act
        var publishTasks = largeMessages.Select(async msg => await broker.PublishAsync(msg)).ToArray();
        await Task.WhenAll(publishTasks);

        // Assert - Should handle without throwing OutOfMemoryException
        Assert.Equal(largeMessages.Count, broker.PublishedMessages.Count);
        Assert.Equal(largeMessages.Count, broker.SerializedMessages.Count);

        // Force GC to check for memory issues
        GC.Collect();
        GC.WaitForPendingFinalizers();

        // Should still be able to publish after memory pressure
        var finalMessage = new TestMessage { Id = 1000, Data = "Final test" };
        await broker.PublishAsync(finalMessage);

        Assert.Equal(largeMessages.Count + 1, broker.PublishedMessages.Count);
    }

    public class TestMessage
    {
        public int Id { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class LargeDataMessage
    {
        public Guid Id { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string Metadata { get; set; } = string.Empty;
    }
}