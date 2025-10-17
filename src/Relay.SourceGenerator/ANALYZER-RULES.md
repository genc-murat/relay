# Relay Analyzer Rules

## Overview

The RelayAnalyzer provides compile-time validation for Relay framework handler methods, attributes, and configurations. This document lists all analyzer rules, their severity, and how to fix them.

---

## 📚 Rule Categories

- [Error Rules](#error-rules) - Must fix (compilation issues)
- [Warning Rules](#warning-rules) - Should fix (potential issues)
- [Info Rules](#info-rules) - Optional (suggestions)

---

## ❌ Error Rules

### RELAY_GEN_001: Source Generator Error

**Severity**: Error
**Category**: Relay.Generator

**Description**: A general source generator error occurred during code generation.

**Example**:
```csharp
// Error occurs during source generation
// Usually indicates an internal issue
```

**How to Fix**:
- Check the full error message for details
- Verify all Relay attributes are correctly applied
- Ensure Relay.Core is properly referenced

---

### RELAY_GEN_002: Invalid Handler Signature

**Severity**: Error
**Category**: Relay.Generator

**Description**: Handler method has an invalid signature that doesn't match Relay's requirements.

**Example**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(TestRequest request, string invalidParam)
{
    // ❌ ERROR: Invalid additional parameter
}
```

**How to Fix**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
{
    // ✅ CORRECT: Only request and CancellationToken parameters
}
```

**Common Causes**:
- Extra parameters beyond request and CancellationToken
- CancellationToken not in last position
- ref/out parameters
- params parameters
- Generic method parameters

---

### RELAY_GEN_003: Duplicate Handler Registration

**Severity**: Error
**Category**: Relay.Generator

**Description**: Multiple unnamed handlers found for the same request type.

**Example**:
```csharp
public class Handler1
{
    [Handle]  // ❌ ERROR: Duplicate unnamed handler
    public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct) { }
}

public class Handler2
{
    [Handle]  // ❌ ERROR: Duplicate unnamed handler
    public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct) { }
}
```

**How to Fix**:
```csharp
// Option 1: Use named handlers
public class Handler1
{
    [Handle(Name = "Primary")]  // ✅ Named handler
    public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct) { }
}

public class Handler2
{
    [Handle(Name = "Secondary")]  // ✅ Named handler
    public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct) { }
}

// Option 2: Remove duplicate handler
// Keep only one handler per request type
```

---

### RELAY_GEN_004: Missing Relay.Core Reference

**Severity**: Error
**Category**: Relay.Generator

**Description**: The Relay.Core package is not referenced in the project.

**How to Fix**:
```xml
<!-- Add to .csproj file -->
<ItemGroup>
  <PackageReference Include="Relay.Core" Version="*" />
</ItemGroup>
```

---

### RELAY_GEN_005: Named Handler Conflict

**Severity**: Error
**Category**: Relay.Generator

**Description**: Multiple handlers with the same name exist for the same request type.

**Example**:
```csharp
[Handle(Name = "MyHandler")]  // ❌ ERROR: Duplicate name
public ValueTask<string> Handle1(...) { }

[Handle(Name = "MyHandler")]  // ❌ ERROR: Duplicate name
public ValueTask<string> Handle2(...) { }
```

**How to Fix**:
```csharp
[Handle(Name = "PrimaryHandler")]  // ✅ Unique name
public ValueTask<string> Handle1(...) { }

[Handle(Name = "SecondaryHandler")]  // ✅ Unique name
public ValueTask<string> Handle2(...) { }
```

---

### RELAY_GEN_202: Invalid Handler Return Type

**Severity**: Error
**Category**: Relay.Generator

**Description**: Handler return type doesn't match the request's expected response type.

**Example**:
```csharp
public class MyRequest : IRequest<string> { }

[Handle]
public ValueTask<int> HandleAsync(MyRequest request, CancellationToken ct)
{
    // ❌ ERROR: Returns int but request expects string
}
```

**How to Fix**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct)
{
    // ✅ CORRECT: Returns string as expected
    return ValueTask.FromResult("result");
}
```

**Valid Return Types**:
- For `IRequest<TResponse>`: `TResponse`, `Task<TResponse>`, `ValueTask<TResponse>`
- For `IRequest`: `Task`, `ValueTask`
- For `IStreamRequest<T>`: `IAsyncEnumerable<T>`

---

### RELAY_GEN_203: Invalid Stream Handler Return Type

**Severity**: Error
**Category**: Relay.Generator

**Description**: Stream handler doesn't return `IAsyncEnumerable<T>`.

**Example**:
```csharp
public class MyStreamRequest : IStreamRequest<string> { }

[Handle]
public IEnumerable<string> HandleAsync(MyStreamRequest request, CancellationToken ct)
{
    // ❌ ERROR: Must return IAsyncEnumerable<T>, not IEnumerable<T>
}
```

**How to Fix**:
```csharp
[Handle]
public async IAsyncEnumerable<string> HandleAsync(
    MyStreamRequest request,
    [EnumeratorCancellation] CancellationToken ct)
{
    // ✅ CORRECT: Returns IAsyncEnumerable<string>
    yield return "item1";
    yield return "item2";
}
```

---

### RELAY_GEN_204: Invalid Notification Handler Return Type

**Severity**: Error
**Category**: Relay.Generator

**Description**: Notification handler doesn't return `Task` or `ValueTask`.

**Example**:
```csharp
[Notification]
public string HandleAsync(MyNotification notification, CancellationToken ct)
{
    // ❌ ERROR: Must return Task or ValueTask
}
```

**How to Fix**:
```csharp
[Notification]
public ValueTask HandleAsync(MyNotification notification, CancellationToken ct)
{
    // ✅ CORRECT: Returns ValueTask
    return ValueTask.CompletedTask;
}
```

---

### RELAY_GEN_205: Handler Missing Request Parameter

**Severity**: Error
**Category**: Relay.Generator

**Description**: Handler method doesn't have a request parameter.

**Example**:
```csharp
[Handle]
public ValueTask<string> HandleAsync()
{
    // ❌ ERROR: Missing request parameter
}
```

**How to Fix**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct)
{
    // ✅ CORRECT: Has request parameter
}
```

---

### RELAY_GEN_206: Handler Invalid Request Parameter Type

**Severity**: Error
**Category**: Relay.Generator

**Description**: First parameter doesn't implement `IRequest` or `IRequest<T>`.

**Example**:
```csharp
public class InvalidRequest { }  // ❌ Doesn't implement IRequest

[Handle]
public ValueTask<string> HandleAsync(InvalidRequest request, CancellationToken ct)
{
    // ❌ ERROR: request must implement IRequest<T>
}
```

**How to Fix**:
```csharp
public class MyRequest : IRequest<string> { }  // ✅ Implements IRequest<T>

[Handle]
public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct)
{
    // ✅ CORRECT
}
```

---

### RELAY_GEN_208: Notification Handler Missing Parameter

**Severity**: Error
**Category**: Relay.Generator

**Description**: Notification handler doesn't have a notification parameter.

**Example**:
```csharp
[Notification]
public Task HandleAsync()
{
    // ❌ ERROR: Missing notification parameter
}
```

**How to Fix**:
```csharp
[Notification]
public Task HandleAsync(MyNotification notification, CancellationToken ct)
{
    // ✅ CORRECT: Has notification parameter
}
```

---

### RELAY_GEN_209: Invalid Priority Value

**Severity**: Error
**Category**: Relay.Generator

**Description**: Priority attribute value is not a valid integer.

**Example**:
```csharp
[Handle(Priority = "high")]  // ❌ ERROR: Must be int, not string
public ValueTask<string> HandleAsync(...) { }
```

**How to Fix**:
```csharp
[Handle(Priority = 100)]  // ✅ CORRECT: Integer value
public ValueTask<string> HandleAsync(...) { }
```

---

### RELAY_GEN_211: Configuration Conflict

**Severity**: Error
**Category**: Relay.Generator

**Description**: Configuration conflict detected (e.g., mixed named/unnamed handlers).

**Example**:
```csharp
// For same request type:
[Handle]  // ❌ Unnamed
public ValueTask<string> Handler1(...) { }

[Handle(Name = "Named")]  // ❌ Named
public ValueTask<string> Handler2(...) { }
// ERROR: Mixing named and unnamed handlers
```

**How to Fix**:
```csharp
// Option 1: All named
[Handle(Name = "Handler1")]
public ValueTask<string> Handler1(...) { }

[Handle(Name = "Handler2")]
public ValueTask<string> Handler2(...) { }

// Option 2: Keep only one unnamed handler
[Handle]
public ValueTask<string> Handler1(...) { }
```

---

### RELAY_GEN_212: Invalid Pipeline Scope

**Severity**: Error
**Category**: Relay.Generator

**Description**: Pipeline scope is invalid for the method.

---

## ⚠️ Warning Rules

### RELAY_GEN_101: Unused Handler

**Severity**: Warning
**Category**: Relay.Generator

**Description**: Handler is registered but may not be reachable.

**When It Appears**:
- Handler with very low priority (< -1000) might never be selected
- Handler might be shadowed by other handlers

**How to Fix**:
- Review handler priority values
- Ensure handler can actually be invoked
- Remove if truly unused

---

### RELAY_GEN_102: Performance Warning

**Severity**: Warning
**Category**: Relay.Generator

**Description**: Handler has potential performance implications.

**Common Cases**:

#### Case 1: Non-async method not returning Task/ValueTask
```csharp
[Handle]
public string HandleAsync(MyRequest request, CancellationToken ct)
{
    // ⚠️ WARNING: Should return Task/ValueTask for optimal async performance
    return "result";
}
```

**Fix**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(MyRequest request, CancellationToken ct)
{
    return ValueTask.FromResult("result");
}
```

#### Case 2: Extreme priority values
```csharp
[Handle(Priority = 5000)]  // ⚠️ WARNING: Very high priority
[Handle(Priority = -5000)] // ⚠️ WARNING: Very low priority
```

#### Case 3: Common naming conflicts
```csharp
[Handle(Name = "default")]  // ⚠️ WARNING: Might conflict with common patterns
[Handle(Name = "main")]     // ⚠️ WARNING: Might conflict with common patterns
```

---

### RELAY_GEN_207: Handler Missing CancellationToken

**Severity**: Warning
**Category**: Relay.Generator

**Description**: Handler doesn't have a CancellationToken parameter.

**Example**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(MyRequest request)
{
    // ⚠️ WARNING: Missing CancellationToken for proper cancellation support
}
```

**How to Fix**:
```csharp
[Handle]
public ValueTask<string> HandleAsync(MyRequest request, CancellationToken cancellationToken)
{
    // ✅ Can respect cancellation
    cancellationToken.ThrowIfCancellationRequested();
    return ValueTask.FromResult("result");
}
```

**Why It's Important**:
- Enables graceful cancellation
- Prevents wasted resources
- Improves application responsiveness

---

## ℹ️ Info Rules

### RELAY_DEBUG: Relay Generator Debug Information

**Severity**: Info
**Category**: Relay.Generator

**Description**: Debug information from the generator (disabled by default).

---

### RELAY_INFO: Relay Generator Information

**Severity**: Info
**Category**: Relay.Generator

**Description**: Informational messages from the generator.

---

## 📋 Rule Summary Table

| Rule ID | Severity | Category | Description |
|---------|----------|----------|-------------|
| RELAY_DEBUG | Info | Relay.Generator | Debug information |
| RELAY_INFO | Info | Relay.Generator | Informational messages |
| RELAY_GEN_001 | Error | Relay.Generator | Source generator error |
| RELAY_GEN_002 | Error | Relay.Generator | Invalid handler signature |
| RELAY_GEN_003 | Error | Relay.Generator | Duplicate handler |
| RELAY_GEN_004 | Error | Relay.Generator | Missing Relay.Core reference |
| RELAY_GEN_005 | Error | Relay.Generator | Named handler conflict |
| RELAY_GEN_101 | Warning | Relay.Generator | Unused handler |
| RELAY_GEN_102 | Warning | Relay.Generator | Performance warning |
| RELAY_GEN_201 | Error | Relay.Generator | Duplicate pipeline order |
| RELAY_GEN_202 | Error | Relay.Generator | Invalid handler return type |
| RELAY_GEN_203 | Error | Relay.Generator | Invalid stream handler return type |
| RELAY_GEN_204 | Error | Relay.Generator | Invalid notification handler return type |
| RELAY_GEN_205 | Error | Relay.Generator | Handler missing request parameter |
| RELAY_GEN_206 | Error | Relay.Generator | Handler invalid request parameter |
| RELAY_GEN_207 | Warning | Relay.Generator | Handler missing CancellationToken |
| RELAY_GEN_208 | Error | Relay.Generator | Notification handler missing parameter |
| RELAY_GEN_209 | Error | Relay.Generator | Invalid priority value |
| RELAY_GEN_210 | Warning | Relay.Generator | No handlers found |
| RELAY_GEN_211 | Error | Relay.Generator | Configuration conflict |
| RELAY_GEN_212 | Error | Relay.Generator | Invalid pipeline scope |

---

## 🛠️ Configuration

### Suppressing Rules

```csharp
// Suppress specific rule for a method
[Handle]
#pragma warning disable RELAY_GEN_207 // Missing CancellationToken is intentional
public ValueTask<string> HandleAsync(MyRequest request)
{
    return ValueTask.FromResult("result");
}
#pragma warning restore RELAY_GEN_207

// Suppress for entire file
#pragma warning disable RELAY_GEN_207
```

### EditorConfig

```ini
# .editorconfig
[*.cs]
# Set severity levels
dotnet_diagnostic.RELAY_GEN_207.severity = none        # Disable warning
dotnet_diagnostic.RELAY_GEN_102.severity = suggestion  # Reduce to suggestion
dotnet_diagnostic.RELAY_GEN_003.severity = error       # Enforce as error
```

---

## 📖 Best Practices

### 1. Always Include CancellationToken
```csharp
✅ GOOD:
[Handle]
public async ValueTask<Result> HandleAsync(
    MyRequest request,
    CancellationToken cancellationToken)
{
    await SomeAsyncOperation(cancellationToken);
}

❌ BAD:
[Handle]
public ValueTask<Result> HandleAsync(MyRequest request)
{
    // Can't cancel operations
}
```

### 2. Use Appropriate Return Types
```csharp
✅ GOOD:
[Handle]
public ValueTask<string> HandleAsync(...)  // ValueTask for potentially synchronous
{
    if (cachedResult != null)
        return ValueTask.FromResult(cachedResult);

    return new ValueTask<string>(SlowAsync());
}

✅ GOOD:
[Handle]
public Task<string> HandleAsync(...)  // Task for always asynchronous
{
    return SlowAsync();
}
```

### 3. Name Handlers Descriptively
```csharp
✅ GOOD:
[Handle(Name = "PrimaryUserCreation")]
[Handle(Name = "LegacyUserCreation")]

❌ BAD:
[Handle(Name = "default")]
[Handle(Name = "main")]
```

### 4. Use Reasonable Priority Values
```csharp
✅ GOOD:
[Handle(Priority = 0)]    // Default
[Handle(Priority = 10)]   // Slightly higher
[Handle(Priority = -10)]  // Slightly lower

❌ BAD:
[Handle(Priority = 99999)]  // Unreasonably high
[Handle(Priority = -99999)] // Unreasonably low
```

---

## 🔍 Troubleshooting

### "I'm getting RELAY_GEN_003 but I only have one handler"

Check for:
- Base class with [Handle] attribute
- Partial class with duplicate [Handle]
- Override methods both marked with [Handle]

### "RELAY_GEN_202 but my return type matches"

Ensure:
- Return type exactly matches request's TResponse
- Using correct Task/ValueTask wrapper
- No implicit conversions expected

### "RELAY_GEN_207 warning but I don't need cancellation"

You can:
1. Add CancellationToken parameter anyway (recommended)
2. Suppress the warning if truly unnecessary
3. Document why cancellation isn't needed

---

## 📚 Additional Resources

- [Relay Documentation](https://relay-framework.com)
- [Handler Signatures Guide](https://relay-framework.com/docs/handlers/signatures)
- [Attribute Reference](https://relay-framework.com/docs/attributes)
- [Performance Best Practices](https://relay-framework.com/docs/performance)

---

**Last Updated**: 2025-10-17
**Analyzer Version**: 1.1.0
**Relay.Core Version**: Compatible with all 1.x versions
