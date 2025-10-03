using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.Core;
using Relay.MessageBroker;
using Relay.MessageBroker.Compression;
using System.Text;

// Create host with Relay and Message Broker
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add Relay
        services.AddRelay(options =>
        {
            options.ScanAssemblies(typeof(Program).Assembly);
        });

        // Add Message Broker with compression
        services.AddRelayMessageBroker(options =>
        {
            options.UseInMemory();
            options.EnableCompression = true;
            options.CompressionThreshold = 1024; // Compress messages larger than 1KB
        });
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var messageBroker = host.Services.GetRequiredService<IMessageBroker>();

logger.LogInformation("=== Message Compression Sample ===");
logger.LogInformation("Demonstrating message compression with different payload sizes");
logger.LogInformation("");

// Test 1: Small message (won't be compressed)
logger.LogInformation("Test 1: Small message (< threshold)");
await TestMessageCompression(messageBroker, logger, "SmallMessage", 500);
logger.LogInformation("");

// Test 2: Medium message (will be compressed)
logger.LogInformation("Test 2: Medium message (> threshold)");
await TestMessageCompression(messageBroker, logger, "MediumMessage", 5000);
logger.LogInformation("");

// Test 3: Large message (will be compressed)
logger.LogInformation("Test 3: Large message (much > threshold)");
await TestMessageCompression(messageBroker, logger, "LargeMessage", 50000);
logger.LogInformation("");

// Test 4: Demonstrate compression ratio
logger.LogInformation("Test 4: Compression statistics");
await DemonstrateCompressionStats(messageBroker, logger);

logger.LogInformation("");
logger.LogInformation("Press any key to exit...");
Console.ReadKey();

static async Task TestMessageCompression(IMessageBroker broker, ILogger logger, string topic, int payloadSize)
{
    var payload = GeneratePayload(payloadSize);
    var message = new SampleMessage
    {
        Id = Guid.NewGuid().ToString(),
        Topic = topic,
        Payload = payload,
        Timestamp = DateTime.UtcNow
    };

    // Subscribe
    var receivedMessage = new TaskCompletionSource<SampleMessage>();
    await broker.SubscribeAsync<SampleMessage>(topic, async (msg) =>
    {
        receivedMessage.SetResult(msg);
        await Task.CompletedTask;
    });

    // Publish
    var originalSize = Encoding.UTF8.GetByteCount(System.Text.Json.JsonSerializer.Serialize(message));
    await broker.PublishAsync(topic, message);

    // Wait for message
    var received = await receivedMessage.Task;

    logger.LogInformation("Original size: {OriginalSize} bytes", originalSize);
    logger.LogInformation("Message sent and received successfully");
    logger.LogInformation("Message ID: {Id}", received.Id);
    logger.LogInformation("Payload size: {Size} characters", received.Payload.Length);

    // Verify content
    if (received.Payload == message.Payload)
    {
        logger.LogInformation("✅ Content verified - no data loss");
    }
    else
    {
        logger.LogError("❌ Content mismatch!");
    }
}

static async Task DemonstrateCompressionStats(IMessageBroker broker, ILogger logger)
{
    var sizes = new[] { 1000, 5000, 10000, 50000, 100000 };
    
    logger.LogInformation("Payload Size | Original | Compressed | Ratio");
    logger.LogInformation("-------------|----------|------------|------");

    foreach (var size in sizes)
    {
        var payload = GeneratePayload(size);
        var message = new SampleMessage
        {
            Id = Guid.NewGuid().ToString(),
            Topic = "CompressionTest",
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };

        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var originalSize = Encoding.UTF8.GetByteCount(json);
        
        // Compress manually to show ratio
        var compressor = new GzipCompressor();
        var compressed = await compressor.CompressAsync(Encoding.UTF8.GetBytes(json));
        var compressedSize = compressed.Length;
        var ratio = (1.0 - ((double)compressedSize / originalSize)) * 100;

        logger.LogInformation("{Size,12} | {Original,8} | {Compressed,10} | {Ratio,5:F1}%",
            $"{size}",
            $"{originalSize}",
            $"{compressedSize}",
            ratio);
    }

    logger.LogInformation("");
    logger.LogInformation("Key Insights:");
    logger.LogInformation("- Compression is most effective for larger, repetitive data");
    logger.LogInformation("- Small messages may not benefit from compression");
    logger.LogInformation("- Use CompressionThreshold to optimize performance");
    
    await Task.CompletedTask;
}

static string GeneratePayload(int size)
{
    // Generate repetitive data (compresses well)
    var sb = new StringBuilder();
    var baseText = "This is a sample payload with repetitive content. ";
    
    while (sb.Length < size)
    {
        sb.Append(baseText);
    }
    
    return sb.ToString().Substring(0, size);
}

// Sample message class
public class SampleMessage
{
    public string Id { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

