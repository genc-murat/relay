# Azure Key Vault Integration Guide

This guide explains how to use Azure Key Vault for secure encryption key management with Relay MessageBroker.

## Overview

The `AzureKeyVaultKeyProvider` provides enterprise-grade encryption key management by integrating with Azure Key Vault. It offers:

- **Secure Key Storage**: Keys are stored in Azure Key Vault, not in configuration files or environment variables
- **Automatic Key Rotation**: Support for multiple key versions with grace period for seamless rotation
- **Multiple Authentication Methods**: Works with Managed Identity, Azure CLI, service principals, and more
- **Graceful Fallback**: Falls back to environment variables if Key Vault is unavailable
- **Intelligent Caching**: Caches keys for 5 minutes to reduce Key Vault API calls

## Prerequisites

1. An Azure subscription
2. An Azure Key Vault instance
3. Appropriate permissions to access secrets in the Key Vault

## Authentication Methods

The provider uses `DefaultAzureCredential` which automatically tries multiple authentication methods in order:

### 1. Managed Identity (Recommended for Production)

Best for Azure-hosted applications (Azure App Service, Azure Functions, Azure Container Instances, AKS, etc.)

```csharp
// No additional configuration needed - automatically uses Managed Identity
services.AddMessageBroker(options =>
{
    options.Security.KeyProvider = KeyProviderType.AzureKeyVault;
    options.Security.KeyVaultUrl = "https://your-keyvault.vault.azure.net/";
});
```

**Setup Steps:**
1. Enable Managed Identity for your Azure resource
2. Grant the Managed Identity "Get Secret" permission on your Key Vault:
   ```bash
   az keyvault set-policy \
     --name your-keyvault \
     --object-id <managed-identity-object-id> \
     --secret-permissions get list
   ```

### 2. Azure CLI (Recommended for Local Development)

```bash
# Login with Azure CLI
az login

# Set the default subscription (if you have multiple)
az account set --subscription <subscription-id>
```

Then run your application - it will automatically use your Azure CLI credentials.

### 3. Service Principal (Environment Variables)

For non-Azure environments or CI/CD pipelines:

```bash
export AZURE_CLIENT_ID="<app-id>"
export AZURE_TENANT_ID="<tenant-id>"
export AZURE_CLIENT_SECRET="<password>"
```

```csharp
services.AddMessageBroker(options =>
{
    options.Security.KeyProvider = KeyProviderType.AzureKeyVault;
    options.Security.KeyVaultUrl = "https://your-keyvault.vault.azure.net/";
});
```

### 4. Visual Studio / VS Code

Automatically uses your signed-in account in Visual Studio or VS Code Azure Account extension.

## Setting Up Encryption Keys in Azure Key Vault

### Secret Naming Convention

Keys must follow this naming pattern: `relay-encryption-key-{version}`

For example, for version "v1", the secret name should be: `relay-encryption-key-v1`

### Creating a Key

#### Option 1: Using Azure CLI

```bash
# Generate a random 256-bit (32-byte) key
KEY_BYTES=$(openssl rand -base64 32)

# Store it in Key Vault
az keyvault secret set \
  --vault-name your-keyvault \
  --name relay-encryption-key-v1 \
  --value "$KEY_BYTES"
```

#### Option 2: Using PowerShell

```powershell
# Generate a random 256-bit (32-byte) key
$keyBytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Fill($keyBytes)
$keyBase64 = [Convert]::ToBase64String($keyBytes)

# Store it in Key Vault
az keyvault secret set `
  --vault-name your-keyvault `
  --name relay-encryption-key-v1 `
  --value $keyBase64
```

#### Option 3: Using Azure Portal

1. Navigate to your Key Vault in Azure Portal
2. Go to "Secrets" section
3. Click "+ Generate/Import"
4. Enter name: `relay-encryption-key-v1`
5. Enter a Base64-encoded 32-byte key as the value
6. Click "Create"

## Configuration

### Basic Configuration

```csharp
services.AddMessageBroker(options =>
{
    options.MessageBrokerType = MessageBrokerType.RabbitMQ;
    
    // Enable encryption with Azure Key Vault
    options.Security.EnableEncryption = true;
    options.Security.KeyProvider = KeyProviderType.AzureKeyVault;
    options.Security.KeyVaultUrl = "https://your-keyvault.vault.azure.net/";
    options.Security.KeyVersion = "v1";
});
```

### Configuration with appsettings.json

```json
{
  "MessageBroker": {
    "MessageBrokerType": "RabbitMQ",
    "Security": {
      "EnableEncryption": true,
      "KeyProvider": "AzureKeyVault",
      "KeyVaultUrl": "https://your-keyvault.vault.azure.net/",
      "KeyVersion": "v1"
    }
  }
}
```

```csharp
services.AddMessageBroker(builder.Configuration.GetSection("MessageBroker"));
```

## Key Rotation

The provider supports seamless key rotation:

### 1. Create a New Key Version

```bash
# Generate and store a new key version
KEY_BYTES=$(openssl rand -base64 32)
az keyvault secret set \
  --vault-name your-keyvault \
  --name relay-encryption-key-v2 \
  --value "$KEY_BYTES"
```

### 2. Update Configuration to Use New Key

```csharp
options.Security.KeyVersion = "v2";
options.Security.KeyRotationGracePeriod = TimeSpan.FromDays(7);
```

### 3. How It Works

- New messages are encrypted with **v2**
- Old messages encrypted with **v1** can still be decrypted during the grace period
- After the grace period expires, only **v2** is used

## Fallback to Environment Variables

If Azure Key Vault is unavailable, the provider automatically falls back to environment variables:

```bash
# For the current key version
export RELAY_ENCRYPTION_KEY="<base64-encoded-32-byte-key>"

# For specific versions
export RELAY_ENCRYPTION_KEY_V1="<base64-encoded-32-byte-key>"
export RELAY_ENCRYPTION_KEY_V2="<base64-encoded-32-byte-key>"
```

## Best Practices

### 1. Use Managed Identity in Production

Avoid using service principals with secrets. Use Managed Identity whenever possible.

### 2. Enable Key Vault Soft Delete and Purge Protection

```bash
az keyvault update \
  --name your-keyvault \
  --enable-soft-delete true \
  --enable-purge-protection true
```

### 3. Use Azure Key Vault Firewall

Restrict access to your Key Vault:

```bash
az keyvault network-rule add \
  --name your-keyvault \
  --ip-address <your-app-ip>
```

### 4. Monitor Key Access

Enable Key Vault logging to Azure Monitor:

```bash
az monitor diagnostic-settings create \
  --resource "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.KeyVault/vaults/{vault-name}" \
  --name "KeyVault-Diagnostics" \
  --logs '[{"category": "AuditEvent", "enabled": true}]' \
  --workspace "/subscriptions/{subscription-id}/resourceGroups/{resource-group}/providers/Microsoft.OperationalInsights/workspaces/{workspace-name}"
```

### 5. Rotate Keys Regularly

Set up a key rotation schedule (e.g., every 90 days):

1. Create new key version
2. Deploy configuration update with new version
3. Wait for grace period
4. Optionally disable old key version in Key Vault

### 6. Use Key Expiration

Set expiration dates on keys in Key Vault for automatic enforcement:

```bash
az keyvault secret set \
  --vault-name your-keyvault \
  --name relay-encryption-key-v1 \
  --value "$KEY_BYTES" \
  --expires "2025-12-31T23:59:59Z"
```

## Troubleshooting

### "Failed to initialize Azure Key Vault client"

**Cause**: Unable to authenticate with Azure Key Vault.

**Solutions**:
- Ensure you're logged in with Azure CLI: `az login`
- Check environment variables are set correctly for service principal
- Verify Managed Identity is enabled and has permissions
- Check Key Vault firewall rules

### "Encryption key not found in Key Vault"

**Cause**: Secret doesn't exist or wrong name.

**Solutions**:
- Verify secret name follows convention: `relay-encryption-key-{version}`
- Check secret exists: `az keyvault secret show --vault-name your-keyvault --name relay-encryption-key-v1`
- Ensure the version in configuration matches the secret name

### "Access Denied"

**Cause**: Insufficient permissions on Key Vault.

**Solutions**:
- Grant "Get" and "List" secret permissions:
  ```bash
  az keyvault set-policy \
    --name your-keyvault \
    --object-id <principal-object-id> \
    --secret-permissions get list
  ```

### High Latency

**Cause**: Too many Key Vault API calls.

**Solutions**:
- Keys are cached for 5 minutes by default - this should minimize calls
- Check logs to see how often keys are being fetched
- Consider if you need to adjust cache refresh interval (requires code change)

## Security Considerations

1. **Never log encryption keys**: The provider is designed to never log key values
2. **Use HTTPS only**: Key Vault URLs should always use HTTPS
3. **Principle of least privilege**: Grant only necessary permissions
4. **Regular audits**: Review Key Vault access logs regularly
5. **Backup keys**: While Key Vault is highly available, consider disaster recovery procedures
6. **Network isolation**: Use Private Endpoints for Key Vault in production

## Sample Application

See the `samples/MessageBroker.Security.AzureKeyVault` directory for a complete example.

## Additional Resources

- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [DefaultAzureCredential Overview](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Azure Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)
- [Managed Identity Documentation](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
