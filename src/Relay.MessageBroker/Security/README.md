# Message Encryption

The Message Encryption feature provides end-to-end encryption for message payloads using AES-256-GCM encryption. This ensures that sensitive data is protected both in transit and at rest.

## Features

- **AES-256-GCM Encryption**: Industry-standard encryption algorithm with authenticated encryption
- **Key Rotation Support**: Graceful key rotation with configurable grace period for decrypting old messages
- **Multiple Key Sources**: Load keys from environment variables or Azure Key Vault
- **Automatic Key Caching**: Keys are cached with automatic refresh to minimize overhead
- **Encryption Metadata**: Key version and algorithm information stored in message headers
- **Transparent Integration**: Decorator pattern allows encryption to be added without modifying existing code

## Configuration

### Basic Configuration with Environment Variables

```csharp
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.EncryptionAlgorithm = "AES256-GCM";
    options.KeyVersion = "v1";
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});

// Decorate the message broker with encryption
services.DecorateWithEncryption();
```

Set the encryption key in an environment variable:
```bash
# Windows
set RELAY_ENCRYPTION_KEY=<base64-encoded-32-byte-key>

# Linux/Mac
export RELAY_ENCRYPTION_KEY=<base64-encoded-32-byte-key>
```

### Configuration with Azure Key Vault

```csharp
services.AddMessageEncryptionWithKeyVault(options =>
{
    options.EnableEncryption = true;
    options.KeyVaultUrl = "https://your-keyvault.vault.azure.net/";
    options.KeyVersion = "v1";
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});

services.DecorateWithEncryption();
```

## Key Generation

Generate a secure 256-bit (32-byte) encryption key:

```bash
# Using PowerShell
$key = [System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
$base64Key = [Convert]::ToBase64String($key)
Write-Host $base64Key

# Using OpenSSL
openssl rand -base64 32
```

## Key Rotation

### Step 1: Generate New Key

Generate a new encryption key for version v2:

```bash
openssl rand -base64 32
```

### Step 2: Set New Key in Environment

```bash
# Keep old key available during grace period
export RELAY_ENCRYPTION_KEY_V1=<old-key>
export RELAY_ENCRYPTION_KEY_V2=<new-key>
```

### Step 3: Update Configuration

```csharp
services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v2"; // Update to new version
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});
```

### Step 4: Deploy and Monitor

- New messages will be encrypted with v2
- Old messages encrypted with v1 can still be decrypted during the grace period
- After the grace period expires, remove the old key

## Usage

Once configured, encryption is transparent to your application code:

```csharp
// Publishing - message is automatically encrypted
await messageBroker.PublishAsync(new OrderCreatedEvent
{
    OrderId = "12345",
    CustomerId = "customer-1",
    Amount = 99.99m
});

// Subscribing - message is automatically decrypted
await messageBroker.SubscribeAsync<OrderCreatedEvent>(async (message, context, ct) =>
{
    // Message is already decrypted
    Console.WriteLine($"Order {message.OrderId} created");
});
```

## Security Best Practices

1. **Never Commit Keys**: Never commit encryption keys to source control
2. **Use Key Vault**: Use Azure Key Vault or similar services for production environments
3. **Rotate Keys Regularly**: Rotate encryption keys every 90 days
4. **Monitor Key Usage**: Track which key versions are being used
5. **Secure Key Storage**: Store keys in secure locations with restricted access
6. **Use Strong Keys**: Always use cryptographically secure random keys
7. **Grace Period**: Set an appropriate grace period for key rotation (24-48 hours recommended)

## Encryption Metadata

Encryption metadata is automatically added to message headers:

- `X-Encryption-KeyVersion`: The key version used for encryption
- `X-Encryption-Algorithm`: The encryption algorithm (e.g., "AES256-GCM")
- `X-Encryption-EncryptedAt`: Timestamp when the message was encrypted

This metadata is used during decryption to:
- Select the correct key version
- Verify the encryption algorithm
- Track message age for key rotation

## Performance Considerations

- **Overhead**: Encryption adds approximately 5-10ms latency per message
- **Size Increase**: Encrypted messages are slightly larger due to nonce and tag (28 bytes overhead)
- **Key Caching**: Keys are cached for 5 minutes to minimize key loading overhead
- **Batch Processing**: Consider using batch processing for high-throughput scenarios

## Troubleshooting

### EncryptionException: Key not found

**Cause**: The encryption key is not set in environment variables or Key Vault.

**Solution**: Set the `RELAY_ENCRYPTION_KEY` environment variable or configure Key Vault.

### EncryptionException: Invalid key size

**Cause**: The encryption key is not 32 bytes (256 bits).

**Solution**: Generate a new key using the key generation commands above.

### EncryptionException: Failed to decrypt message

**Cause**: The message was encrypted with a key that is no longer available.

**Solution**: Ensure old keys are available during the grace period after key rotation.

## Example: Complete Setup

```csharp
// Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Add message broker
builder.Services.AddMessageBroker(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.ConnectionString = "amqp://localhost";
});

// Add encryption
builder.Services.AddMessageEncryption(options =>
{
    options.EnableEncryption = true;
    options.KeyVersion = "v1";
    options.KeyRotationGracePeriod = TimeSpan.FromHours(24);
});

// Decorate with encryption
builder.Services.DecorateWithEncryption();

var app = builder.Build();

// Use the message broker
var messageBroker = app.Services.GetRequiredService<IMessageBroker>();

// Messages are automatically encrypted/decrypted
await messageBroker.PublishAsync(new SensitiveDataEvent
{
    UserId = "user-123",
    CreditCardNumber = "4111-1111-1111-1111"
});
```

## Related Features

- [Outbox Pattern](../Outbox/README.md) - Reliable message publishing
- [Inbox Pattern](../Inbox/README.md) - Idempotent message processing
- [Distributed Tracing](../DistributedTracing/README.md) - End-to-end tracing
- [Health Checks](../HealthChecks/README.md) - Monitor broker health
