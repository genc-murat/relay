# Contract Validation Pipeline Behavior

Relay provides built-in support for contract validation through pipeline behaviors. This feature allows you to automatically validate request and response contracts against their JSON schemas.

## 🚀 Quick Start

### 1. Enable Contract Validation

To enable contract validation, call `AddRelayContractValidation()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayContractValidation(); // Enable contract validation
```

### 2. Define Requests with Contract Validation

Mark requests as requiring contract validation by applying the `ValidateContractAttribute`:

```csharp
[ValidateContract]
public record CreateUserRequest(string Name, string Email) : IRequest<User>;
```

### 3. Use Contract Validation

Contract validation happens automatically when you send requests:

```csharp
try
{
    var request = new CreateUserRequest("John Doe", "john.doe@example.com");
    var user = await relay.SendAsync(request); // Will validate request and response contracts
}
catch (ContractValidationException ex)
{
    Console.WriteLine($"Contract validation failed: {ex.Message}");
}
```

## 🎯 Key Features

### ValidateContract Attribute

The `ValidateContractAttribute` enables contract validation for specific request types:

```csharp
[ValidateContract(ValidateRequest = true, ValidateResponse = false)] // Only validate request
public record CreateOrderRequest(int UserId, decimal Total) : IRequest<Order>;

[ValidateContract(ThrowOnValidationFailure = false)] // Don't throw exception on failure
public record GetUserRequest(int UserId) : IRequest<User>;
```

### Contract Validator

Relay provides a default contract validator that uses JSON schema validation:

```csharp
public class CustomContractValidator : IContractValidator
{
    public async ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        // Custom request validation logic
    }
    
    public async ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default)
    {
        // Custom response validation logic
    }
}
```

### JSON Schema Integration

Contract validation works with the existing JSON schema generation:

```csharp
// Schemas are automatically generated at compile time
var metadata = EndpointMetadataRegistry.GetEndpointsForRequestType<CreateUserRequest>();
var requestSchema = metadata.First().RequestSchema;
var responseSchema = metadata.First().ResponseSchema;
```

## 🛠️ Advanced Configuration

### Handler-Specific Configuration

Configure contract validation options for specific handlers:

```csharp
services.ConfigureContractValidation<CreateUserRequest>(options =>
{
    options.ValidateRequests = true;
    options.ValidateResponses = true;
    options.ThrowOnValidationFailure = true;
});
```

### Global Contract Validation

Enable automatic contract validation for all requests:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultContractValidationOptions.EnableAutomaticContractValidation = true;
    options.DefaultContractValidationOptions.ValidateRequests = true;
    options.DefaultContractValidationOptions.ValidateResponses = true;
});
```

### Exception Handling

Configure whether to throw exceptions when validation fails:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultContractValidationOptions.ThrowOnValidationFailure = false;
});
```

## ⚡ Performance

Contract validation is designed to be lightweight and efficient:

- **Configurable**: Only executes when contract validation is enabled
- **Schema-Based**: Uses pre-generated JSON schemas for validation
- **Async Support**: Fully asynchronous implementation
- **Early Exit**: Stops validation when appropriate

## 🧪 Testing

Requests with contract validation can be tested by providing invalid data and verifying validation behavior.