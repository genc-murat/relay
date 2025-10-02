# Exception Handling Sample

This sample demonstrates the usage of `IRequestExceptionHandler` and `IRequestExceptionAction` in Relay framework for comprehensive exception management.

## Features Demonstrated

### 1. **IRequestExceptionHandler**
Exception handlers can catch and handle specific exception types, optionally providing a fallback response:
- **Handle exceptions gracefully**: Return alternative responses instead of crashing
- **Type-safe exception handling**: Specific handlers for specific exception types
- **Exception hierarchy support**: Handlers work with exception base types
- **Multiple handler support**: Chain handlers until one handles the exception

### 2. **IRequestExceptionAction**
Exception actions execute for side effects like logging, monitoring, or analytics:
- **Side effects only**: Cannot suppress exceptions
- **Monitoring and observability**: Log exceptions, send metrics, notify teams
- **Multiple actions**: All registered actions execute regardless of failures
- **Non-intrusive**: Doesn't affect the exception flow

## Sample Components

### Exception Handlers

1. **InsufficientFundsHandler**: Handles payment failures due to insufficient balance
2. **InvalidCardHandler**: Handles invalid card number errors
3. **UserNotFoundHandler**: Provides default user when user doesn't exist

### Exception Actions

1. **PaymentExceptionLogger**: Logs payment exceptions to monitoring system
2. **GeneralExceptionLogger**: General-purpose exception logging

## Architecture

```
Request
  ↓
[ExceptionHandlerBehavior]
  ↓
[ExceptionActionBehavior]
  ↓
[Handler] → May throw exception
  ↓
Exception thrown?
  ├─ Yes → Action executes → Handler attempts to handle → Response or rethrow
  └─ No → Response returned normally
```

## Running the Sample

```bash
cd samples/ExceptionHandlingSample
dotnet run
```

## Expected Output

```
=== Relay Exception Handling Sample ===

1. Processing payment with insufficient funds...

  [Handler] Processing payment for John Doe
  [Handler] Card: 1234-5678, Amount: $1,000.00
  [ExceptionAction - Monitoring] Payment exception occurred
  [ExceptionAction - Monitoring] Customer: John Doe
  [ExceptionAction - Monitoring] Exception: InsufficientFundsException
  [ExceptionAction - Monitoring] Logging to monitoring system...
  [ExceptionHandler] Handling insufficient funds
  [ExceptionHandler] Required: $1,000.00, Available: $500.00
Result: InsufficientFunds - Payment declined. Available balance: $500.00

------------------------------------------------------------

2. Processing payment with invalid card...

  [Handler] Processing payment for Jane Smith
  [Handler] Card: INVALID, Amount: $50.00
  [ExceptionAction - Monitoring] Payment exception occurred
  [ExceptionAction - Monitoring] Customer: Jane Smith
  [ExceptionAction - Monitoring] Exception: InvalidCardException
  [ExceptionAction - Monitoring] Logging to monitoring system...
  [ExceptionHandler] Handling invalid card
  [ExceptionHandler] Card: INVALID
Result: InvalidCard - Payment declined. Please check your card number.

------------------------------------------------------------

3. Getting non-existent user...

  [Handler] Looking up user ID: 999
  [ExceptionAction - General] Exception occurred
  [ExceptionAction - General] Request: GetUserCommand(UserId=999)
  [ExceptionAction - General] Exception Type: UserNotFoundException
  [ExceptionHandler] User not found: 999
  [ExceptionHandler] Returning default user
User: Unknown User (ID: 0)

------------------------------------------------------------

4. Processing successful payment...

  [Handler] Processing payment for Alice Johnson
  [Handler] Card: VALID-CARD, Amount: $50.00
Result: Success - Payment of $50.00 processed successfully

=== Sample Completed ===
```

## Key Concepts

### Exception Handler Registration

```csharp
// Register exception handling behaviors (required)
services.AddRelayExceptionHandlers();

// Register specific exception handlers
services.AddExceptionHandler<ProcessPaymentCommand, PaymentResult, InsufficientFundsException, InsufficientFundsHandler>();
services.AddExceptionHandler<ProcessPaymentCommand, PaymentResult, InvalidCardException, InvalidCardHandler>();
```

### Exception Action Registration

```csharp
// Register exception actions for side effects
services.AddExceptionAction<ProcessPaymentCommand, PaymentException, PaymentExceptionLogger>();
services.AddExceptionAction<GetUserCommand, Exception, GeneralExceptionLogger>();
```

### Handler Implementation

```csharp
public class InsufficientFundsHandler
    : IRequestExceptionHandler<ProcessPaymentCommand, PaymentResult, InsufficientFundsException>
{
    public ValueTask<ExceptionHandlerResult<PaymentResult>> HandleAsync(
        ProcessPaymentCommand request,
        InsufficientFundsException exception,
        CancellationToken cancellationToken)
    {
        // Handle the exception and return a response
        var result = new PaymentResult("InsufficientFunds", "Payment declined.");

        return new ValueTask<ExceptionHandlerResult<PaymentResult>>(
            ExceptionHandlerResult<PaymentResult>.Handle(result));
    }
}
```

### Action Implementation

```csharp
public class PaymentExceptionLogger
    : IRequestExceptionAction<ProcessPaymentCommand, PaymentException>
{
    public ValueTask ExecuteAsync(
        ProcessPaymentCommand request,
        PaymentException exception,
        CancellationToken cancellationToken)
    {
        // Log to monitoring system, send metrics, etc.
        _logger.LogError(exception, "Payment failed for {Customer}", request.CustomerName);
        _metrics.Increment("payment.errors");

        return default;
    }
}
```

## Benefits

1. **Graceful Degradation**: Handle errors without crashing the application
2. **Separation of Concerns**: Exception handling logic separate from business logic
3. **Type Safety**: Strongly typed exception handlers
4. **Observability**: Exception actions for monitoring and logging
5. **Flexibility**: Multiple handlers can be chained
6. **Exception Hierarchy**: Handlers work with base exception types
7. **Reusability**: Share exception handlers across multiple request types

## Exception Handler vs Exception Action

| Feature | Exception Handler | Exception Action |
|---------|------------------|------------------|
| **Can suppress exception** | ✅ Yes | ❌ No |
| **Can provide response** | ✅ Yes | ❌ No |
| **Primary use** | Recovery/fallback | Logging/monitoring |
| **Multiple execution** | Until one handles | All execute |
| **Failure handling** | Stops on first handle | Continues on error |

## MediatR Compatibility

This implementation is fully compatible with MediatR's exception handling interfaces:
- `IRequestExceptionHandler<TRequest, TResponse, TException>`
- `IRequestExceptionAction<TRequest, TException>`

Making migration from MediatR seamless.

## Real-World Use Cases

- **Payment Processing**: Handle different payment failure scenarios
- **API Integrations**: Handle external service failures gracefully
- **User Management**: Provide defaults for missing data
- **Monitoring**: Track errors in production systems
- **Resilience**: Implement fallback strategies
- **Compliance**: Audit exception occurrences
