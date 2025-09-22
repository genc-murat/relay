# Authorization Pipeline Behavior

Relay provides built-in support for authorization through pipeline behaviors. This feature allows you to protect your handlers with role-based or policy-based authorization.

## 🚀 Quick Start

### 1. Enable Authorization

To enable authorization, call `AddRelayAuthorization()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayAuthorization(); // Enable authorization
```

### 2. Define Authorized Requests

Mark requests as requiring authorization by applying the `AuthorizeAttribute`:

```csharp
[Authorize("Admin", "User")] // Requires either Admin or User role
public record GetUserRequest(int UserId) : IRequest<User>;
```

### 3. Use Authorization

Authorization happens automatically when you send requests:

```csharp
try
{
    var request = new GetUserRequest(123);
    var user = await relay.SendAsync(request); // May throw AuthorizationException
}
catch (AuthorizationException ex)
{
    Console.WriteLine($"Authorization failed: {ex.Message}");
}
```

## 🎯 Key Features

### Authorize Attribute

The `AuthorizeAttribute` enables authorization for specific request types:

```csharp
[Authorize("Admin")] // Requires Admin role
public record DeleteUserRequest(int UserId) : IRequest<bool>;

[Authorize(true, "CanViewOrders")] // Requires CanViewOrders policy
public record GetOrderRequest(int OrderId) : IRequest<Order>;
```

### Custom Authorization Services

Implement custom authorization logic by implementing the `IAuthorizationService` interface:

```csharp
public class CustomAuthorizationService : IAuthorizationService
{
    public async ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        // Custom authorization logic
        return context.UserRoles.Contains("Admin");
    }
}
```

### Authorization Context

The `IAuthorizationContext` provides information about the current user:

```csharp
public interface IAuthorizationContext
{
    IEnumerable<Claim> UserClaims { get; }
    IEnumerable<string> UserRoles { get; }
    IDictionary<string, object> Properties { get; }
}
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure authorization options for specific handlers:

```csharp
services.ConfigureAuthorization<GetUserRequest>(options =>
{
    options.ThrowOnAuthorizationFailure = false;
});
```

### Global Authorization

Enable automatic authorization for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultAuthorizationOptions.EnableAutomaticAuthorization = true;
});
```

### Exception Handling

Configure whether to throw exceptions when authorization fails:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultAuthorizationOptions.ThrowOnAuthorizationFailure = false;
});
```

## ⚡ Performance

Authorization is designed to be lightweight and efficient:

- **Early Execution**: Authorization pipeline behavior runs early in the pipeline to fail fast
- **Minimal Overhead**: Only executes when authorization is required
- **Async Support**: Fully asynchronous implementation

## 🧪 Testing

Authorized requests can be tested by configuring the authorization context and catching the `AuthorizationException`.