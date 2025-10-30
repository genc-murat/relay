# Authentication & Authorization Quick Start

Get started with message authentication and authorization in 5 minutes.

## Step 1: Install Dependencies

The authentication features are built into `Relay.MessageBroker`. Ensure you have the following NuGet packages:

```xml
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.0" />
<PackageReference Include="Microsoft.IdentityModel.Tokens" Version="7.0.0" />
```

For Azure AD integration:
```xml
<PackageReference Include="Microsoft.Identity.Client" Version="4.56.0" />
```

## Step 2: Configure Authentication

Add authentication services in your `Program.cs` or `Startup.cs`:

```csharp
using Relay.MessageBroker.Security;

// Basic JWT authentication
services.AddMessageAuthentication(authOptions =>
{
    authOptions.EnableAuthentication = true;
    authOptions.JwtIssuer = "https://your-app.com";
    authOptions.JwtAudience = "message-api";
    authOptions.JwtSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
}, authzOptions =>
{
    // Configure permissions
    authzOptions.PublishPermissions["admin"] = new List<string> { "*" };
    authzOptions.PublishPermissions["service"] = new List<string> { "OrderCreated", "OrderUpdated" };
    
    authzOptions.SubscribePermissions["admin"] = new List<string> { "*" };
    authzOptions.SubscribePermissions["service"] = new List<string> { "OrderCreated" };
});
```

## Step 3: Apply Security Decorator

Decorate your message broker with security:

```csharp
// Add your message broker
services.AddRabbitMQMessageBroker(options => { /* ... */ });

// Apply security decorator
services.DecorateWithSecurity();
```

## Step 4: Generate JWT Token

Create a helper to generate JWT tokens:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

public class TokenGenerator
{
    public static string GenerateToken(string issuer, string audience, string signingKey, string[] roles)
    {
        var securityKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "service-account"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## Step 5: Publish Messages with Authentication

Include the token in message headers:

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly string _authToken;

    public OrderService(IMessageBroker messageBroker, IConfiguration config)
    {
        _messageBroker = messageBroker;
        
        // Generate or retrieve token
        _authToken = TokenGenerator.GenerateToken(
            config["Jwt:Issuer"],
            config["Jwt:Audience"],
            config["Jwt:SigningKey"],
            new[] { "service" });
    }

    public async Task CreateOrderAsync(Order order)
    {
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            Amount = order.TotalAmount
        };

        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {_authToken}"
            }
        };

        await _messageBroker.PublishAsync(orderEvent, options);
    }
}
```

## Step 6: Subscribe to Messages

The decorator automatically validates tokens on incoming messages:

```csharp
public class PaymentService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                // Token has already been validated
                await ProcessPaymentAsync(message);
                await context.Acknowledge!();
            },
            cancellationToken: cancellationToken);

        await _messageBroker.StartAsync(cancellationToken);
    }
}
```

## Configuration Options

### appsettings.json

```json
{
  "MessageBroker": {
    "Authentication": {
      "EnableAuthentication": true,
      "JwtIssuer": "https://your-app.com",
      "JwtAudience": "message-api",
      "TokenCacheTtl": "00:05:00"
    }
  }
}
```

### Environment Variables

```bash
JWT_SIGNING_KEY=your-base64-encoded-256-bit-key
JWT_ISSUER=https://your-app.com
JWT_AUDIENCE=message-api
```

## Common Scenarios

### Scenario 1: Service-to-Service Communication

```csharp
// Service A (Publisher)
authzOptions.PublishPermissions["service-a"] = new List<string> { "OrderCreated" };

// Service B (Consumer)
authzOptions.SubscribePermissions["service-b"] = new List<string> { "OrderCreated" };
```

### Scenario 2: Admin Access

```csharp
// Admin can publish and subscribe to everything
authzOptions.PublishPermissions["admin"] = new List<string> { "*" };
authzOptions.SubscribePermissions["admin"] = new List<string> { "*" };
```

### Scenario 3: Multi-Tenant

```csharp
// Tenant-specific permissions
authzOptions.PublishPermissions["tenant-a-service"] = new List<string> 
{ 
    "TenantA.OrderCreated",
    "TenantA.OrderUpdated"
};
```

## Troubleshooting

### Error: "Authentication token is required"

**Solution**: Add the `Authorization` header to your publish options:
```csharp
options.Headers["Authorization"] = $"Bearer {token}";
```

### Error: "Invalid authentication token"

**Possible causes**:
1. Token has expired
2. Wrong signing key
3. Issuer/audience mismatch

**Solution**: Verify your token configuration and regenerate the token.

### Error: "Insufficient permissions"

**Solution**: Check that the user's role has the required permissions:
```csharp
authzOptions.PublishPermissions["your-role"] = new List<string> { "YourMessageType" };
```

## Next Steps

- Read the [full documentation](AUTHENTICATION_README.md)
- Check out [examples](AUTHENTICATION_EXAMPLE.md)
- Learn about [Azure AD integration](AUTHENTICATION_README.md#azure-ad-integration)
- Explore [OAuth2 integration](AUTHENTICATION_README.md#oauth2-integration)

## Security Best Practices

1. ✅ Use HTTPS/TLS for all connections
2. ✅ Use short-lived tokens (15-60 minutes)
3. ✅ Store signing keys securely (Azure Key Vault, AWS Secrets Manager)
4. ✅ Implement key rotation
5. ✅ Monitor security events
6. ✅ Use strong keys (256-bit minimum)
7. ✅ Validate audience to prevent token reuse
8. ✅ Grant least privilege permissions

## Support

For issues or questions:
- Check the [troubleshooting guide](AUTHENTICATION_README.md#troubleshooting)
- Review [examples](AUTHENTICATION_EXAMPLE.md)
- Check security event logs for details
