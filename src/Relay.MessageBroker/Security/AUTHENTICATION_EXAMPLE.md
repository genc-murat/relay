# Authentication and Authorization Examples

This document provides practical examples of using message authentication and authorization.

## Example 1: Basic JWT Authentication

### Setup

```csharp
// Program.cs or Startup.cs
services.AddMessageAuthentication(authOptions =>
{
    authOptions.EnableAuthentication = true;
    authOptions.JwtIssuer = "https://myapp.com";
    authOptions.JwtAudience = "message-api";
    authOptions.JwtSigningKey = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes("your-256-bit-secret-key-here-32chars"));
}, authzOptions =>
{
    authzOptions.RoleClaimType = "role";
    authzOptions.AllowByDefault = false;
    
    // Admin can do everything
    authzOptions.PublishPermissions["admin"] = new List<string> { "*" };
    authzOptions.SubscribePermissions["admin"] = new List<string> { "*" };
    
    // Order service can publish and subscribe to order events
    authzOptions.PublishPermissions["order-service"] = new List<string> 
    { 
        "OrderCreated", 
        "OrderUpdated", 
        "OrderCancelled" 
    };
    authzOptions.SubscribePermissions["order-service"] = new List<string> 
    { 
        "PaymentProcessed" 
    };
    
    // Payment service can publish payment events and subscribe to orders
    authzOptions.PublishPermissions["payment-service"] = new List<string> 
    { 
        "PaymentProcessed", 
        "PaymentFailed" 
    };
    authzOptions.SubscribePermissions["payment-service"] = new List<string> 
    { 
        "OrderCreated" 
    };
});

services.AddRabbitMQMessageBroker(/* ... */);
services.DecorateWithSecurity();
```

### Publishing Messages

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly string _authToken;

    public OrderService(IMessageBroker messageBroker, IConfiguration configuration)
    {
        _messageBroker = messageBroker;
        _authToken = configuration["MessageBroker:AuthToken"];
    }

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        // Create order
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Items = request.Items,
            TotalAmount = request.Items.Sum(i => i.Price * i.Quantity)
        };

        // Save to database
        await _orderRepository.SaveAsync(order);

        // Publish event with authentication
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.TotalAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {_authToken}"
            }
        };

        await _messageBroker.PublishAsync(orderCreatedEvent, publishOptions);
    }
}
```

### Subscribing to Messages

```csharp
public class PaymentService
{
    private readonly IMessageBroker _messageBroker;
    private readonly string _authToken;

    public PaymentService(IMessageBroker messageBroker, IConfiguration configuration)
    {
        _messageBroker = messageBroker;
        _authToken = configuration["MessageBroker:AuthToken"];
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            HandleOrderCreatedAsync,
            new SubscriptionOptions
            {
                // Note: Token is typically added by infrastructure/middleware
                // For manual testing, you can add it here
            },
            cancellationToken);

        await _messageBroker.StartAsync(cancellationToken);
    }

    private async ValueTask HandleOrderCreatedAsync(
        OrderCreatedEvent message,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        // Process payment
        var paymentResult = await _paymentProcessor.ProcessAsync(
            message.CustomerId,
            message.TotalAmount);

        // Publish payment result
        var paymentEvent = paymentResult.Success
            ? new PaymentProcessedEvent
            {
                OrderId = message.OrderId,
                Amount = message.TotalAmount,
                TransactionId = paymentResult.TransactionId
            }
            : (object)new PaymentFailedEvent
            {
                OrderId = message.OrderId,
                Reason = paymentResult.ErrorMessage
            };

        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {_authToken}",
                ["CorrelationId"] = context.CorrelationId ?? Guid.NewGuid().ToString()
            }
        };

        await _messageBroker.PublishAsync(paymentEvent, publishOptions, cancellationToken);
        await context.Acknowledge!();
    }
}
```

## Example 2: Azure AD Integration

### Setup

```csharp
services.AddMessageAuthenticationWithAzureAd(
    authOptions =>
    {
        authOptions.EnableAuthentication = true;
        authOptions.JwtIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        authOptions.JwtAudience = clientId;
    },
    authzOptions =>
    {
        authzOptions.RoleClaimType = "roles"; // Azure AD uses "roles" claim
        authzOptions.PublishPermissions["Message.Publisher"] = new List<string> { "*" };
        authzOptions.SubscribePermissions["Message.Consumer"] = new List<string> { "*" };
    },
    azureAdOptions =>
    {
        azureAdOptions.TenantId = configuration["AzureAd:TenantId"];
        azureAdOptions.ClientId = configuration["AzureAd:ClientId"];
        azureAdOptions.ClientSecret = configuration["AzureAd:ClientSecret"];
    });

services.AddRabbitMQMessageBroker(/* ... */);
services.DecorateWithSecurity();
```

### Acquiring Azure AD Token

```csharp
public class AzureAdTokenProvider
{
    private readonly IConfidentialClientApplication _app;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry;

    public AzureAdTokenProvider(IConfiguration configuration)
    {
        _app = ConfidentialClientApplicationBuilder
            .Create(configuration["AzureAd:ClientId"])
            .WithClientSecret(configuration["AzureAd:ClientSecret"])
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{configuration["AzureAd:TenantId"]}"))
            .Build();
    }

    public async Task<string> GetTokenAsync()
    {
        // Return cached token if still valid
        if (_cachedToken != null && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        // Acquire new token
        var scopes = new[] { $"{configuration["AzureAd:ClientId"]}/.default" };
        var result = await _app.AcquireTokenForClient(scopes).ExecuteAsync();

        _cachedToken = result.AccessToken;
        _tokenExpiry = result.ExpiresOn.AddMinutes(-5); // Refresh 5 minutes before expiry

        return _cachedToken;
    }
}
```

### Using Azure AD Token

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly AzureAdTokenProvider _tokenProvider;

    public OrderService(IMessageBroker messageBroker, AzureAdTokenProvider tokenProvider)
    {
        _messageBroker = messageBroker;
        _tokenProvider = tokenProvider;
    }

    public async Task PublishOrderEventAsync(OrderCreatedEvent orderEvent)
    {
        var token = await _tokenProvider.GetTokenAsync();

        var publishOptions = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        };

        await _messageBroker.PublishAsync(orderEvent, publishOptions);
    }
}
```

## Example 3: Custom Identity Provider

### Implementing Custom Provider

```csharp
public class CustomIdentityProvider : IIdentityProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _validationEndpoint;

    public CustomIdentityProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _validationEndpoint = configuration["CustomIdP:ValidationEndpoint"];
    }

    public async ValueTask<bool> ValidateTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, _validationEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask<TokenValidationInfo> GetValidationInfoAsync(
        CancellationToken cancellationToken = default)
    {
        // Fetch validation info from your identity provider
        var response = await _httpClient.GetFromJsonAsync<IdPMetadata>(
            $"{_validationEndpoint}/metadata",
            cancellationToken);

        return new TokenValidationInfo
        {
            Issuer = response.Issuer,
            Audience = response.Audience,
            SigningKeys = response.SigningKeys
        };
    }
}
```

### Registering Custom Provider

```csharp
services.AddMessageAuthentication(/* ... */);
services.AddSingleton<IIdentityProvider, CustomIdentityProvider>();
services.AddHttpClient<CustomIdentityProvider>();
services.DecorateWithSecurity();
```

## Example 4: Multi-Tenant Authorization

### Setup

```csharp
services.AddMessageAuthentication(authOptions =>
{
    authOptions.EnableAuthentication = true;
    authOptions.JwtIssuer = "https://myapp.com";
    authOptions.JwtAudience = "message-api";
    authOptions.JwtSigningKey = signingKey;
}, authzOptions =>
{
    authzOptions.RoleClaimType = "role";
    
    // Tenant-specific roles
    authzOptions.PublishPermissions["tenant-a-publisher"] = new List<string> 
    { 
        "TenantA.OrderCreated",
        "TenantA.OrderUpdated"
    };
    authzOptions.PublishPermissions["tenant-b-publisher"] = new List<string> 
    { 
        "TenantB.OrderCreated",
        "TenantB.OrderUpdated"
    };
    
    authzOptions.SubscribePermissions["tenant-a-consumer"] = new List<string> 
    { 
        "TenantA.OrderCreated"
    };
    authzOptions.SubscribePermissions["tenant-b-consumer"] = new List<string> 
    { 
        "TenantB.OrderCreated"
    };
});

services.DecorateWithSecurity();
```

### Publishing Tenant-Specific Messages

```csharp
public class MultiTenantOrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ITenantContext _tenantContext;

    public async Task CreateOrderAsync(CreateOrderRequest request)
    {
        var tenantId = _tenantContext.CurrentTenantId;
        var token = _tenantContext.CurrentUserToken;

        var orderEvent = new OrderCreatedEvent
        {
            TenantId = tenantId,
            OrderId = Guid.NewGuid().ToString(),
            // ... other properties
        };

        var publishOptions = new PublishOptions
        {
            RoutingKey = $"{tenantId}.OrderCreated",
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {token}",
                ["TenantId"] = tenantId
            }
        };

        await _messageBroker.PublishAsync(orderEvent, publishOptions);
    }
}
```

## Example 5: Testing with Authentication

### Creating Test Tokens

```csharp
public class JwtTokenGenerator
{
    public static string GenerateToken(
        string issuer,
        string audience,
        string signingKey,
        string[] roles,
        TimeSpan? expiration = null)
    {
        var securityKey = new SymmetricSecurityKey(
            Convert.FromBase64String(signingKey));
        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
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
            expires: DateTime.UtcNow.Add(expiration ?? TimeSpan.FromHours(1)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Integration Tests

```csharp
public class AuthenticationIntegrationTests : IClassFixture<MessageBrokerFixture>
{
    private readonly IMessageBroker _messageBroker;
    private readonly string _signingKey;

    [Fact]
    public async Task PublishAsync_WithValidToken_Succeeds()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateToken(
            "https://myapp.com",
            "message-api",
            _signingKey,
            new[] { "admin" });

        var message = new OrderCreatedEvent { OrderId = "123" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        };

        // Act
        await _messageBroker.PublishAsync(message, options);

        // Assert - no exception thrown
    }

    [Fact]
    public async Task PublishAsync_WithExpiredToken_ThrowsAuthenticationException()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateToken(
            "https://myapp.com",
            "message-api",
            _signingKey,
            new[] { "admin" },
            TimeSpan.FromSeconds(-1)); // Expired

        var message = new OrderCreatedEvent { OrderId = "123" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _messageBroker.PublishAsync(message, options));
    }

    [Fact]
    public async Task PublishAsync_WithInsufficientPermissions_ThrowsAuthenticationException()
    {
        // Arrange
        var token = JwtTokenGenerator.GenerateToken(
            "https://myapp.com",
            "message-api",
            _signingKey,
            new[] { "viewer" }); // No publish permissions

        var message = new OrderCreatedEvent { OrderId = "123" };
        var options = new PublishOptions
        {
            Headers = new Dictionary<string, object>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<AuthenticationException>(
            () => _messageBroker.PublishAsync(message, options));
    }
}
```

## Example 6: Middleware for Automatic Token Injection

### ASP.NET Core Middleware

```csharp
public class MessageBrokerAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public MessageBrokerAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMessageBrokerContext brokerContext)
    {
        // Extract token from HTTP request
        var token = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(token))
        {
            // Store token in message broker context for automatic injection
            brokerContext.SetAuthToken(token);
        }

        await _next(context);
    }
}

public interface IMessageBrokerContext
{
    string? AuthToken { get; }
    void SetAuthToken(string token);
}

public class MessageBrokerContext : IMessageBrokerContext
{
    private static readonly AsyncLocal<string?> _authToken = new();

    public string? AuthToken => _authToken.Value;

    public void SetAuthToken(string token)
    {
        _authToken.Value = token;
    }
}
```

### Auto-Injecting Decorator

```csharp
public class AutoAuthMessageBrokerDecorator : IMessageBroker
{
    private readonly IMessageBroker _innerBroker;
    private readonly IMessageBrokerContext _context;

    public async ValueTask PublishAsync<TMessage>(
        TMessage message,
        PublishOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PublishOptions();
        options.Headers ??= new Dictionary<string, object>();

        // Auto-inject token if not already present
        if (!options.Headers.ContainsKey("Authorization") && 
            !string.IsNullOrEmpty(_context.AuthToken))
        {
            options.Headers["Authorization"] = _context.AuthToken;
        }

        await _innerBroker.PublishAsync(message, options, cancellationToken);
    }

    // ... other methods
}
```

This allows you to publish messages without manually adding the token each time:

```csharp
// Token is automatically injected from HTTP context
await _messageBroker.PublishAsync(orderEvent);
```
