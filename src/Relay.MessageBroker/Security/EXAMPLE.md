# Message Encryption Examples

This document provides practical examples of using message encryption in various scenarios.

## Example 1: Basic Encryption Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.Security;

var services = new ServiceCollection();

// Configure message broker
services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
});

// Add encryption with environment variable key
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v1";
});

// Decorate broker with encryption
services.DecorateWithEncryption();

var serviceProvider = services.BuildServiceProvider();
var messageBroker = serviceProvider.GetRequiredService<IMessageBroker>();

// Publish encrypted message
await messageBroker.PublishAsync(new PaymentProcessedEvent
{
    PaymentId = "pay-123",
    Amount = 150.00m,
    CardLastFour = "1234"
});
```

## Example 2: Key Rotation Scenario

```csharp
// Initial setup with v1 key
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v1";
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});

// Environment variables:
// RELAY_ENCRYPTION_KEY_V1=<base64-key-v1>

// After some time, rotate to v2
// 1. Generate new key and set environment variable
// RELAY_ENCRYPTION_KEY_V2=<base64-key-v2>

// 2. Update configuration
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v2"; // Now using v2
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});

// 3. Deploy the update
// - New messages encrypted with v2
// - Old messages with v1 can still be decrypted for 24 hours

// 4. After grace period, remove v1 key
// unset RELAY_ENCRYPTION_KEY_V1
```

## Example 3: Azure Key Vault Integration

```csharp
using Azure.Identity;

services.AddMessageEncryptionWithKeyVault(options =>
{
    options.EnableEncryption = true;
    options.KeyVaultUrl = "https://my-keyvault.vault.azure.net/";
    options.KeyVersion = "v1";
    options.KeyRotationGracePeriod = TimeSpan.FromHours(48);
});

services.DecorateWithEncryption();

// Note: Ensure your application has access to Key Vault
// - Use Managed Identity in Azure
// - Or set AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID
```

## Example 4: Conditional Encryption

```csharp
// Encrypt only sensitive messages
public class SensitiveDataEvent
{
    public string UserId { get; set; }
    public string SocialSecurityNumber { get; set; }
    public decimal Salary { get; set; }
}

public class PublicEvent
{
    public string EventId { get; set; }
    public string EventType { get; set; }
}

// Configure encryption
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v1";
});

// Only decorate for sensitive data broker
services.AddSingleton<IMessageBroker>(sp =>
{
    var baseBroker = /* create base broker */;
    var encryptor = sp.GetRequiredService<IMessageEncryptor>();
    var options = sp.GetRequiredService<IOptions<SecurityOptions>>();
    var logger = sp.GetRequiredService<ILogger<EncryptionMessageBrokerDecorator>>();
    
    return new EncryptionMessageBrokerDecorator(baseBroker, encryptor, options, logger);
});
```

## Example 5: Monitoring Encryption

```csharp
using Microsoft.Extensions.Logging;

public class EncryptionMonitoringService
{
    private readonly IMessageBroker _messageBroker;
    private readonly KeyRotationManager _keyRotationManager;
    private readonly ILogger<EncryptionMonitoringService> _logger;

    public EncryptionMonitoringService(
        IMessageBroker messageBroker,
        KeyRotationManager keyRotationManager,
        ILogger<EncryptionMonitoringService> logger)
    {
        _messageBroker = messageBroker;
        _keyRotationManager = keyRotationManager;
        _logger = logger;
    }

    public async Task MonitorKeyUsageAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var validVersions = _keyRotationManager.GetValidKeyVersions();
            
            _logger.LogInformation(
                "Active key versions: {Versions}",
                string.Join(", ", validVersions));

            foreach (var version in validVersions)
            {
                var metadata = _keyRotationManager.GetKeyVersionMetadata(version);
                if (metadata != null)
                {
                    _logger.LogInformation(
                        "Key {Version}: Active={Active}, ActivatedAt={ActivatedAt}",
                        version,
                        metadata.IsActive,
                        metadata.ActivatedAt);
                }
            }

            // Cleanup expired keys
            var removedCount = _keyRotationManager.CleanupExpiredKeyVersions();
            if (removedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired key versions", removedCount);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }
}
```

## Example 6: Testing Encryption

```csharp
using Xunit;
using Moq;

public class EncryptionTests
{
    [Fact]
    public async Task PublishAsync_WithEncryption_EncryptsMessage()
    {
        // Arrange
        var mockInnerBroker = new Mock<IMessageBroker>();
        var mockEncryptor = new Mock<IMessageEncryptor>();
        var options = Options.Create(new SecurityOptions
        {
            EnableEncryption = true,
            KeyVersion = "v1"
        });
        var logger = Mock.Of<ILogger<EncryptionMessageBrokerDecorator>>();

        mockEncryptor
            .Setup(e => e.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new byte[] { 1, 2, 3, 4 });

        mockEncryptor
            .Setup(e => e.GetKeyVersion())
            .Returns("v1");

        var decorator = new EncryptionMessageBrokerDecorator(
            mockInnerBroker.Object,
            mockEncryptor.Object,
            options,
            logger);

        var message = new TestMessage { Id = "123", Data = "sensitive" };

        // Act
        await decorator.PublishAsync(message);

        // Assert
        mockEncryptor.Verify(
            e => e.EncryptAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);

        mockInnerBroker.Verify(
            b => b.PublishAsync(
                It.IsAny<EncryptedMessageWrapper>(),
                It.Is<PublishOptions>(o => 
                    o.Headers.ContainsKey(EncryptionMetadata.KeyVersionHeaderKey)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeAsync_WithEncryption_DecryptsMessage()
    {
        // Arrange
        var mockInnerBroker = new Mock<IMessageBroker>();
        var mockEncryptor = new Mock<IMessageEncryptor>();
        var options = Options.Create(new SecurityOptions
        {
            EnableEncryption = true,
            KeyVersion = "v1"
        });
        var logger = Mock.Of<ILogger<EncryptionMessageBrokerDecorator>>();

        var originalMessage = new TestMessage { Id = "123", Data = "sensitive" };
        var serializedMessage = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(originalMessage);

        mockEncryptor
            .Setup(e => e.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedMessage);

        var decorator = new EncryptionMessageBrokerDecorator(
            mockInnerBroker.Object,
            mockEncryptor.Object,
            options,
            logger);

        TestMessage? receivedMessage = null;

        // Act
        await decorator.SubscribeAsync<TestMessage>(async (msg, ctx, ct) =>
        {
            receivedMessage = msg;
        });

        // Assert
        Assert.NotNull(receivedMessage);
        mockEncryptor.Verify(
            e => e.DecryptAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private class TestMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
```

## Example 7: Performance Testing

```csharp
using System.Diagnostics;

public class EncryptionPerformanceTest
{
    public async Task MeasureEncryptionOverheadAsync()
    {
        var services = new ServiceCollection();
        
        // Setup without encryption
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.InMemory;
        });
        
        var sp1 = services.BuildServiceProvider();
        var brokerWithoutEncryption = sp1.GetRequiredService<IMessageBroker>();
        
        // Setup with encryption
        services.AddMessageEncryption(options =>
        {
            options.EnableEncryption = true;
            options.KeyVersion = "v1";
        });
        services.DecorateWithEncryption();
        
        var sp2 = services.BuildServiceProvider();
        var brokerWithEncryption = sp2.GetRequiredService<IMessageBroker>();
        
        var message = new LargeMessage
        {
            Id = Guid.NewGuid().ToString(),
            Data = new string('x', 10000) // 10KB message
        };
        
        // Measure without encryption
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            await brokerWithoutEncryption.PublishAsync(message);
        }
        sw1.Stop();
        
        // Measure with encryption
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            await brokerWithEncryption.PublishAsync(message);
        }
        sw2.Stop();
        
        Console.WriteLine($"Without encryption: {sw1.ElapsedMilliseconds}ms");
        Console.WriteLine($"With encryption: {sw2.ElapsedMilliseconds}ms");
        Console.WriteLine($"Overhead: {sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds}ms ({(double)(sw2.ElapsedMilliseconds - sw1.ElapsedMilliseconds) / sw1.ElapsedMilliseconds * 100:F2}%)");
    }
    
    private class LargeMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}
```

## Example 8: Error Handling

```csharp
public class EncryptionErrorHandlingExample
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<EncryptionErrorHandlingExample> _logger;

    public async Task PublishWithErrorHandlingAsync<TMessage>(TMessage message)
    {
        try
        {
            await _messageBroker.PublishAsync(message);
        }
        catch (EncryptionException ex)
        {
            _logger.LogError(ex, "Failed to encrypt message");
            
            // Handle encryption failure
            // - Alert security team
            // - Store message in dead letter queue
            // - Retry with different key version
            
            throw;
        }
    }

    public async Task SubscribeWithErrorHandlingAsync<TMessage>(
        Func<TMessage, MessageContext, CancellationToken, ValueTask> handler)
    {
        await _messageBroker.SubscribeAsync<TMessage>(async (message, context, ct) =>
        {
            try
            {
                await handler(message, context, ct);
            }
            catch (EncryptionException ex)
            {
                _logger.LogError(ex, "Failed to decrypt message");
                
                // Handle decryption failure
                // - Check if key version is available
                // - Move to poison message queue
                // - Alert operations team
                
                // Don't re-throw to prevent message redelivery
            }
        });
    }
}
```
