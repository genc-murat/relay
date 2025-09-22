# Validation Pipeline Behavior

Relay provides built-in support for automatic request validation through pipeline behaviors. This feature allows you to define validation rules that are automatically executed before requests reach their handlers.

## 🚀 Quick Start

### 1. Enable Validation

To enable validation, call `AddRelayValidation()` when configuring services:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
builder.Services.AddRelayValidation(); // Enable validation
builder.Services.AddValidationRulesFromCallingAssembly(); // Auto-register validation rules
```

### 2. Define Validation Rules

Create validation rules by implementing the `IValidationRule<TRequest>` interface:

```csharp
public class CreateUserRequestValidationRule : IValidationRule<CreateUserRequest>
{
    public async ValueTask<IEnumerable<string>> ValidateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name is required.");
        }
        else if (request.Name.Length < 2)
        {
            errors.Add("Name must be at least 2 characters long.");
        }
        
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!request.Email.Contains("@"))
        {
            errors.Add("Email must be a valid email address.");
        }
        
        return errors;
    }
}
```

### 3. Register Validation Rules

You can register validation rules manually or automatically:

```csharp
// Manual registration
services.AddTransient<IValidationRule<CreateUserRequest>, CreateUserRequestValidationRule>();

// Or auto-registration (recommended)
services.AddValidationRulesFromCallingAssembly();
```

### 4. Use Validation

Validation happens automatically when you send requests:

```csharp
try
{
    var request = new CreateUserRequest("", "invalid-email");
    var user = await relay.SendAsync(request);
}
catch (ValidationException ex)
{
    Console.WriteLine($"Validation failed: {string.Join(", ", ex.Errors)}");
}
```

## 🎯 Key Features

### Validation Rules

Validation rules implement the `IValidationRule<TRequest>` interface:

```csharp
public interface IValidationRule<in TRequest>
{
    ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

### Validation Pipeline Behavior

The `ValidationPipelineBehavior<TRequest, TResponse>` automatically validates requests before they reach handlers. If validation fails, a `ValidationException` is thrown.

### Validation Order

You can control the execution order of validation rules using the `ValidationRuleAttribute`:

```csharp
[ValidationRule(Order = 1)]
public class FirstValidationRule : IValidationRule<CreateUserRequest>
{
    // Implementation
}

[ValidationRule(Order = 2)]
public class SecondValidationRule : IValidationRule<CreateUserRequest>
{
    // Implementation
}
```

### Continue on Error

By default, validation stops when the first rule fails. You can change this behavior:

```csharp
[ValidationRule(Order = 1, ContinueOnError = true)]
public class FirstValidationRule : IValidationRule<CreateUserRequest>
{
    // Implementation
}
```

## 🛠️ Advanced Configuration

### Custom Validator

You can implement custom validators by implementing the `IValidator<TRequest>` interface:

```csharp
public class CustomValidator<TRequest> : IValidator<TRequest>
{
    private readonly IEnumerable<IValidationRule<TRequest>> _validationRules;

    public CustomValidator(IEnumerable<IValidationRule<TRequest>> validationRules)
    {
        _validationRules = validationRules;
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        // Custom validation logic
        var errors = new List<string>();
        foreach (var rule in _validationRules)
        {
            var ruleErrors = await rule.ValidateAsync(request, cancellationToken);
            errors.AddRange(ruleErrors);
        }
        return errors;
    }
}
```

### Configuration Options

You can configure validation behavior through `ValidationOptions`:

```csharp
services.Configure<RelayOptions>(options =>
{
    options.DefaultValidationOptions.EnableAutomaticValidation = true;
    options.DefaultValidationOptions.ThrowOnValidationFailure = true;
    options.DefaultValidationOptions.ContinueOnFailure = false;
    options.DefaultValidationOptions.DefaultOrder = -1000;
});
```

## 🧪 Testing

Validation rules can be easily tested in isolation:

```csharp
[Test]
public async Task Should_Validate_CreateUserRequest()
{
    // Arrange
    var rule = new CreateUserRequestValidationRule();
    var request = new CreateUserRequest("", "invalid-email");
    
    // Act
    var errors = await rule.ValidateAsync(request);
    
    // Assert
    Assert.That(errors, Contains.Item("Name is required."));
    Assert.That(errors, Contains.Item("Email must be a valid email address."));
}
```

## ⚡ Performance

Validation rules are registered as transient services and executed efficiently in the pipeline. The validation pipeline behavior runs early in the pipeline (by default at order -1000) to fail fast on invalid requests.