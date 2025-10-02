# Pre/Post Processor Sample

This sample demonstrates the usage of `IRequestPreProcessor` and `IRequestPostProcessor` in Relay framework.

## Features Demonstrated

### 1. **IRequestPreProcessor**
Pre-processors execute **before** the handler and all pipeline behaviors. They are useful for:
- Request logging
- Input validation
- Request enrichment
- Setting up context

### 2. **IRequestPostProcessor**
Post-processors execute **after** the handler completes successfully. They are useful for:
- Response logging
- Audit trails
- Notifications
- Cleanup operations
- Analytics

## Sample Components

### Pre-Processors

1. **LoggingPreProcessor**: Logs incoming requests
2. **ValidationPreProcessor**: Validates request data before processing

### Post-Processors

1. **AuditPostProcessor**: Creates audit logs after successful operations
2. **NotificationPostProcessor**: Sends notifications (e.g., welcome emails)

## Execution Order

```
Request
  ↓
[PreProcessor 1] → Logging
  ↓
[PreProcessor 2] → Validation
  ↓
[Handler] → Business Logic
  ↓
[PostProcessor 1] → Audit
  ↓
[PostProcessor 2] → Notification
  ↓
Response
```

## Running the Sample

```bash
cd samples/PrePostProcessorSample
dotnet run
```

## Expected Output

```
=== Relay Pre/Post Processor Sample ===

1. Creating a new user with pre/post processors...

  [PreProcessor - Logging] Request received: CreateUser 'John Doe'
  [PreProcessor - Validation] Validating user data...
  [PreProcessor - Validation] ✓ Validation passed
  [Handler] Creating user: John Doe
  [PostProcessor - Audit] User created - ID: 1, Name: John Doe
  [PostProcessor - Audit] Audit log saved at 2025-01-15 10:30:45 UTC
  [PostProcessor - Notification] Sending welcome email to john@example.com
  [PostProcessor - Notification] ✓ Welcome email queued

✓ User created: John Doe (ID: 1)

------------------------------------------------------------

2. Updating user with pre/post processors...

  [PreProcessor - Logging] Request received: UpdateUser ID 1
  [Handler] Updating user ID 1
  [PostProcessor - Audit] User updated - ID: 1, Name: John Smith
  [PostProcessor - Audit] Audit log saved at 2025-01-15 10:30:45 UTC

✓ User updated: John Smith (ID: 1)

=== Sample Completed ===
```

## Key Concepts

### Registration

```csharp
// Enable pre/post processor behaviors
services.AddRelayPrePostProcessors();

// Register pre-processors
services.AddPreProcessor<CreateUserCommand, LoggingPreProcessor>();
services.AddPreProcessor<CreateUserCommand, ValidationPreProcessor>();

// Register post-processors
services.AddPostProcessor<CreateUserCommand, User, AuditPostProcessor>();
services.AddPostProcessor<CreateUserCommand, User, NotificationPostProcessor>();
```

### Implementation

#### Pre-Processor
```csharp
public class ValidationPreProcessor : IRequestPreProcessor<CreateUserCommand>
{
    public ValueTask ProcessAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Validate before handler executes
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        return default;
    }
}
```

#### Post-Processor
```csharp
public class AuditPostProcessor : IRequestPostProcessor<CreateUserCommand, User>
{
    public ValueTask ProcessAsync(
        CreateUserCommand request,
        User response,
        CancellationToken cancellationToken)
    {
        // Audit after handler succeeds
        Console.WriteLine($"User created: {response.Id}");
        return default;
    }
}
```

## Benefits

1. **Separation of Concerns**: Keep cross-cutting concerns separate from business logic
2. **Reusability**: Share pre/post processors across multiple handlers
3. **Order Control**: Execute in registration order
4. **Type Safety**: Strongly typed request and response
5. **Clean Code**: Keep handlers focused on business logic

## MediatR Compatibility

This implementation is compatible with MediatR's `IRequestPreProcessor<TRequest>` and `IRequestPostProcessor<TRequest, TResponse>` interfaces, making migration easier.
