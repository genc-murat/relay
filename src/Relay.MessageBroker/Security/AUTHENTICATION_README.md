# Message Authentication and Authorization

This module provides JWT-based authentication and role-based authorization for message broker operations.

## Features

- **JWT Token Validation**: Validates JWT tokens with signature verification, expiration checks, and issuer/audience validation
- **Token Caching**: Caches validated tokens for 5 minutes (configurable) to improve performance
- **Role-Based Authorization**: Configurable role-to-permission mappings for publish and subscribe operations
- **External Identity Providers**: Integration with Azure AD and OAuth2 providers
- **Security Event Logging**: Comprehensive logging of authentication and authorization events
- **Decorator Pattern**: Non-invasive security layer that wraps existing message broker implementations

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│         SecurityMessageBrokerDecorator                      │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  1. Extract token from headers                        │ │
│  │  2. Validate token (IMessageAuthenticator)            │ │
│  │  3. Authorize operation (role-based)                  │ │
│  │  4. Forward to inner broker if authorized             │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│         JwtMessageAuthenticator                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  - Token validation with caching                      │ │
│  │  - Signature verification (symmetric/asymmetric)      │ │
│  │  - Expiration, issuer, audience validation            │ │
│  │  - Role extraction from claims                        │ │
│  │  - Permission checking                                │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│         IIdentityProvider (Optional)                        │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  - AzureAdIdentityProvider                            │ │
│  │  - OAuth2IdentityProvider                             │ │
│  │  - Custom implementations                             │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

## Configuration

### Basic JWT Authentication

```csharp
services.AddMessageAuthentication(authOptions =>
{
    authOptions.EnableAuthentication = true;
    authOptions.JwtIssuer = "https://your-issuer.com";
    authOptions.JwtAudience = "your-audience";
    authOptions.JwtSigningKey = "your-base64-encoded-key";
    authOptions.TokenCacheTtl = TimeSpan.FromMinutes(5);
}, authzOptions =>
{
    authzOptions.RoleClaimType = "role";
    authzOptions.AllowByDefault = false;
    
    // Configure publish permissions
    authzOptions.PublishPermissions["admin"] = new List<string> { "*" };
    authzOptions.PublishPermissions["publisher"] = new List<string> { "OrderCreated", "OrderUpdated" };
    
    // Configure subscribe permissions
    authzOptions.SubscribePermissions["admin"] = new List<string> { "*" };
    authzOptions.SubscribePermissions["subscriber"] = new List<string> { "OrderCreated" };
});

// Decorate the message broker with security
services.DecorateWithSecurity();
```

### Azure AD Integration

```csharp
services.AddMessageAuthenticationWithAzureAd(
    authOptions =>
    {
        authOptions.EnableAuthentication = true;
        authOptions.JwtIssuer = "https://login.microsoftonline.com/{tenant-id}/v2.0";
        authOptions.JwtAudience = "{client-id}";
    },
    authzOptions =>
    {
        authzOptions.PublishPermissions["MessagePublisher"] = new List<string> { "*" };
        authzOptions.SubscribePermissions["MessageConsumer"] = new List<string> { "*" };
    },
    azureAdOptions =>
    {
        azureAdOptions.TenantId = "{tenant-id}";
        azureAdOptions.ClientId = "{client-id}";
    });

services.DecorateWithSecurity();
```

### OAuth2 Integration

```csharp
services.AddMessageAuthenticationWithOAuth2(
    authOptions =>
    {
        authOptions.EnableAuthentication = true;
        authOptions.JwtIssuer = "https://your-oauth-provider.com";
        authOptions.JwtAudience = "your-api";
    },
    authzOptions =>
    {
        authzOptions.PublishPermissions["publisher"] = new List<string> { "*" };
    },
    oauth2Options =>
    {
        oauth2Options.Authority = "https://your-oauth-provider.com";
        oauth2Options.Audience = "your-api";
        oauth2Options.IntrospectionEndpoint = "https://your-oauth-provider.com/oauth/introspect";
        oauth2Options.ClientId = "your-client-id";
        oauth2Options.ClientSecret = "your-client-secret";
    });

services.DecorateWithSecurity();
```

## Usage

### Publishing with Authentication

```csharp
var message = new OrderCreatedEvent { OrderId = "123", Amount = 100.00m };

var publishOptions = new PublishOptions
{
    Headers = new Dictionary<string, object>
    {
        ["Authorization"] = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
};

await messageBroker.PublishAsync(message, publishOptions);
```

### Subscribing with Authentication

```csharp
await messageBroker.SubscribeAsync<OrderCreatedEvent>(async (message, context, ct) =>
{
    // The message has already been authenticated and authorized
    // by the SecurityMessageBrokerDecorator
    
    Console.WriteLine($"Processing order: {message.OrderId}");
    
    await context.Acknowledge!();
});
```

## Token Format

The authentication token should be a valid JWT token with the following structure:

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "iss": "https://your-issuer.com",
    "aud": "your-audience",
    "sub": "user-id",
    "role": ["admin", "publisher"],
    "exp": 1234567890,
    "iat": 1234567890
  }
}
```

## Supported Signature Algorithms

- HMAC: HS256, HS384, HS512
- RSA: RS256, RS384, RS512
- ECDSA: ES256, ES384, ES512

## Role-Based Authorization

### Permission Mapping

Permissions are configured using role-to-permission mappings:

```csharp
authzOptions.PublishPermissions["admin"] = new List<string> { "*" };
authzOptions.PublishPermissions["publisher"] = new List<string> 
{ 
    "OrderCreated", 
    "OrderUpdated",
    "OrderDeleted"
};
```

- Use `"*"` to grant access to all message types
- Specify individual message type names for granular control
- Multiple roles can be assigned to a user via JWT claims

### Authorization Flow

1. Extract token from message headers (`Authorization` or `X-Auth-Token`)
2. Validate token signature, expiration, issuer, and audience
3. Extract roles from JWT claims (configurable claim type)
4. Check if any user role has permissions for the operation
5. Allow or deny the operation based on permissions

## Security Event Logging

All security events are logged with the `SECURITY:` prefix:

```
SECURITY: Authentication successful for subject: user@example.com
SECURITY: Authorized publish. Roles: [admin, publisher]. MessageType: OrderCreated
SECURITY: Unauthorized publish attempt. Reason: Insufficient permissions. Roles: [viewer]. MessageType: OrderCreated
SECURITY: Authentication failed. Reason: Token expired
```

## Performance Considerations

### Token Caching

- Validated tokens are cached for 5 minutes (configurable)
- Cache is automatically cleaned up every minute
- Reduces validation overhead for frequently used tokens

### Signature Verification

- Symmetric keys (HMAC) are faster than asymmetric keys (RSA, ECDSA)
- Public keys are loaded once at startup
- Consider using Azure Key Vault for key management in production

## Error Handling

### AuthenticationException

Thrown when authentication or authorization fails:

```csharp
try
{
    await messageBroker.PublishAsync(message, options);
}
catch (AuthenticationException ex)
{
    // Handle authentication/authorization failure
    Console.WriteLine($"Security error: {ex.Message}");
}
```

### Common Error Scenarios

- **No token provided**: `Authentication token is required for publish operations`
- **Invalid token**: `Invalid authentication token`
- **Expired token**: Token validation fails with expiration error
- **Insufficient permissions**: `Insufficient permissions to publish messages of type {MessageType}`

## Best Practices

1. **Use HTTPS**: Always use HTTPS/TLS for message broker connections
2. **Rotate Keys**: Implement key rotation with grace periods
3. **Short Token Lifetime**: Use short-lived tokens (e.g., 15 minutes)
4. **Least Privilege**: Grant minimum required permissions to each role
5. **Monitor Security Events**: Set up alerts for unauthorized access attempts
6. **Validate Audience**: Always validate the audience claim to prevent token reuse
7. **Use Strong Keys**: Use at least 256-bit keys for HMAC, 2048-bit for RSA

## Integration with Other Decorators

The security decorator can be combined with other decorators:

```csharp
services.AddMessageBroker(/* ... */);

// Apply decorators in order
services.DecorateWithSecurity();        // Authentication/Authorization
services.DecorateWithEncryption();      // Message encryption
services.DecorateWithDeduplication();   // Deduplication
services.DecorateWithBatch();           // Batching
```

**Note**: Security should typically be applied first to ensure all operations are authenticated.

## Testing

### Unit Testing

```csharp
[Fact]
public async Task PublishAsync_WithValidToken_Succeeds()
{
    // Arrange
    var token = GenerateValidJwtToken();
    var options = new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["Authorization"] = $"Bearer {token}"
        }
    };
    
    // Act
    await messageBroker.PublishAsync(message, options);
    
    // Assert
    // Verify message was published
}

[Fact]
public async Task PublishAsync_WithInvalidToken_ThrowsAuthenticationException()
{
    // Arrange
    var options = new PublishOptions
    {
        Headers = new Dictionary<string, object>
        {
            ["Authorization"] = "Bearer invalid-token"
        }
    };
    
    // Act & Assert
    await Assert.ThrowsAsync<AuthenticationException>(
        () => messageBroker.PublishAsync(message, options));
}
```

## Troubleshooting

### Token Validation Fails

1. Check token expiration
2. Verify issuer and audience match configuration
3. Ensure signing key is correct
4. Check signature algorithm is supported

### Authorization Fails

1. Verify roles are present in JWT claims
2. Check role claim type configuration
3. Ensure permissions are configured for the user's roles
4. Review security event logs for details

### Performance Issues

1. Increase token cache TTL
2. Use symmetric keys instead of asymmetric
3. Consider using a distributed cache for token validation
4. Monitor cache hit rate in logs
