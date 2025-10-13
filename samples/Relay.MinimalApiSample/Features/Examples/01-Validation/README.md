# 01 - Validation Feature Example

This example demonstrates Relay's automatic request validation feature.

## Features

- ✅ Automatic validation
- ✅ Comprehensive error messages
- ✅ Multiple validation rules
- ✅ Regex-based validation
- ✅ Custom validation logic

## Usage

### 1. Define Request and Response

```csharp
public record RegisterUserRequest(
    string Username,
    string Email,
    string Password,
    int Age
) : IRequest<RegisterUserResponse>;
```

### 2. Create Validator

```csharp
public class RegisterUserValidator : IValidationRule<RegisterUserRequest>
{
    public ValueTask<IEnumerable<string>> ValidateAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Validation rules
        if (string.IsNullOrWhiteSpace(request.Username))
            errors.Add("Username is required");

        // ... other rules

        return ValueTask.FromResult<IEnumerable<string>>(errors);
    }
}
```

### 3. Register in Program.cs

```csharp
// Enable validation
builder.Services.AddRelayValidation();
```

### 4. Minimal API Endpoint

```csharp
app.MapPost("/api/examples/register", async (RegisterUserRequest request, IRelay relay) =>
{
    // Validation runs automatically
    var response = await relay.SendAsync(request);
    return Results.Created($"/api/users/{response.UserId}", response);
})
.WithName("RegisterUser")
.WithTags("Validation Examples")
.WithSummary("Register a new user with validation")
.Produces<RegisterUserResponse>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest);
```

## Test Scenarios

### ✅ Successful Registration

```bash
curl -X POST https://localhost:5001/api/examples/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "john_doe",
    "email": "john@example.com",
    "password": "SecurePass123!",
    "age": 25
  }'
```

**Response (201 Created):**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "username": "john_doe",
  "message": "User registered successfully!"
}
```

### ❌ Validation Errors

```bash
curl -X POST https://localhost:5001/api/examples/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "ab",
    "email": "invalid-email",
    "password": "weak",
    "age": 15
  }'
```

**Response (400 Bad Request):**
```json
{
  "isValid": false,
  "errors": [
    "Username must be at least 3 characters",
    "Email must be a valid email address",
    "Password must be at least 8 characters",
    "Password must contain at least one uppercase letter",
    "Password must contain at least one number",
    "Password must contain at least one special character",
    "User must be at least 18 years old"
  ]
}
```

## Validation Rules

### Username
- ✅ Required
- ✅ Minimum 3 characters
- ✅ Maximum 50 characters
- ✅ Only letters, numbers, and underscore (_)

### Email
- ✅ Required
- ✅ Valid email format

### Password
- ✅ Required
- ✅ Minimum 8 characters
- ✅ At least one uppercase letter
- ✅ At least one lowercase letter
- ✅ At least one number
- ✅ At least one special character

### Age
- ✅ Minimum 18
- ✅ Maximum 120

## Advantages

1. **Automatic Validation**: Checks before handler executes
2. **Clean Code**: No validation logic in handlers
3. **Reusable**: Validators are independent and testable
4. **Comprehensive Error Messages**: All errors returned at once
5. **Performance**: Invalid requests never reach handler

## Advanced Usage

### Multiple Validators

```csharp
// First validator
public class BasicValidator : IValidationRule<RegisterUserRequest> { }

// Second validator
public class AdvancedValidator : IValidationRule<RegisterUserRequest> { }

// Both run automatically
```

### Async Validation

```csharp
public async ValueTask<IEnumerable<string>> ValidateAsync(
    RegisterUserRequest request,
    CancellationToken cancellationToken)
{
    var errors = new List<string>();

    // Check database
    var userExists = await _userRepository.ExistsAsync(request.Username);
    if (userExists)
        errors.Add("Username already exists");

    return errors;
}
```

## Best Practices

1. ✅ Consolidate all validation rules in validator
2. ✅ Use clear and understandable error messages
3. ✅ Define regex patterns statically (performance)
4. ✅ Use cancellationToken for async operations
5. ✅ Unit test validators

## Related Examples

- [02-PrePostProcessors](../02-PrePostProcessors/README.md)
- [03-ExceptionHandling](../03-ExceptionHandling/README.md)
