# Notification Handler Order Control

## Overview

Relay provides **comprehensive handler ordering control** for notification handlers, going beyond MediatR's capabilities with multiple ordering strategies, dependency management, and group-based execution.

## Features

✅ **Order-Based Execution** - Simple numeric ordering (MediatR compatible)  
✅ **Dependency-Based Execution** - Explicit before/after relationships  
✅ **Group-Based Execution** - Logical grouping with parallel execution within groups  
✅ **Execution Mode Control** - Sequential, parallel, high/low priority, fire-and-forget  
✅ **Exception Handling** - Configurable exception suppression per handler  
✅ **Topological Sorting** - Automatic dependency resolution with circular dependency detection  

## Quick Start

### 1. Basic Order Control (MediatR Compatible)

```csharp
using Relay.Core;
using Relay.Core.Publishing;

// Configure ordered notification publisher
services.AddRelay();
services.UseOrderedNotificationPublisher();

// Define handlers with explicit order
[NotificationHandlerOrder(1)]
public class LoggingHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("Order created: {OrderId}", notification.OrderId);
    }
}

[NotificationHandlerOrder(2)]
public class EmailHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(notification.OrderId);
    }
}

[NotificationHandlerOrder(3)]
public class InventoryHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _inventory.UpdateStockAsync(notification.Items);
    }
}
```

**Execution Order:** LoggingHandler → EmailHandler → InventoryHandler

### 2. Dependency-Based Execution

```csharp
// ValidationHandler must execute first
public class ValidationHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Validate order
    }
}

// PaymentHandler executes after validation
[ExecuteAfter(typeof(ValidationHandler))]
public class PaymentHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Process payment
    }
}

// FulfillmentHandler executes after payment
[ExecuteAfter(typeof(PaymentHandler))]
public class FulfillmentHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Start fulfillment
    }
}
```

**Execution Order:** ValidationHandler → PaymentHandler → FulfillmentHandler

### 3. Group-Based Execution

```csharp
// Group 1: Logging (can execute in parallel)
[NotificationHandlerGroup("Logging", groupOrder: 1)]
public class FileLogger : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _fileLogger.LogAsync(notification);
    }
}

[NotificationHandlerGroup("Logging", groupOrder: 1)]
public class DatabaseLogger : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _dbLogger.LogAsync(notification);
    }
}

// Group 2: Notifications (execute after logging completes, can run in parallel)
[NotificationHandlerGroup("Notifications", groupOrder: 2)]
public class EmailNotifier : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _emailService.SendAsync(notification);
    }
}

[NotificationHandlerGroup("Notifications", groupOrder: 2)]
public class SmsNotifier : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _smsService.SendAsync(notification);
    }
}

// Group 3: Analytics (execute after notifications, runs in parallel)
[NotificationHandlerGroup("Analytics", groupOrder: 3)]
public class AnalyticsTracker : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        await _analytics.TrackAsync(notification);
    }
}
```

**Execution Flow:**
1. **Group 1 (Logging)** - FileLogger + DatabaseLogger execute in parallel
2. Wait for Group 1 to complete
3. **Group 2 (Notifications)** - EmailNotifier + SmsNotifier execute in parallel
4. Wait for Group 2 to complete
5. **Group 3 (Analytics)** - AnalyticsTracker executes

## Advanced Features

### Execution Mode Control

```csharp
// Always execute sequentially (even if parallel mode is enabled)
[NotificationExecutionMode(NotificationExecutionMode.Sequential)]
public class CriticalHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Critical operation that must not run in parallel
    }
}

// High priority - executes before normal priority handlers
[NotificationExecutionMode(NotificationExecutionMode.HighPriority)]
public class UrgentHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Urgent operation
    }
}

// Fire and forget - doesn't block other handlers
[NotificationExecutionMode(NotificationExecutionMode.FireAndForget)]
public class BackgroundHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Background operation (errors may not be caught)
    }
}

// Suppress exceptions from this handler
[NotificationExecutionMode(NotificationExecutionMode.Default, 
    AllowParallelExecution = true,
    SuppressExceptions = true)]
public class OptionalHandler : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // Optional operation - failures won't stop other handlers
    }
}
```

### Combining Multiple Attributes

```csharp
// Complex handler with multiple constraints
[NotificationHandlerOrder(5)]
[NotificationHandlerGroup("Payment", groupOrder: 2)]
[ExecuteAfter(typeof(ValidationHandler))]
[ExecuteBefore(typeof(FulfillmentHandler))]
[NotificationExecutionMode(NotificationExecutionMode.Sequential, 
    AllowParallelExecution = false,
    SuppressExceptions = false)]
public class PaymentProcessor : INotificationHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated notification, CancellationToken ct)
    {
        // This handler:
        // - Has order 5
        // - Belongs to "Payment" group (group order 2)
        // - Must execute after ValidationHandler
        // - Must execute before FulfillmentHandler
        // - Always runs sequentially
        // - Exceptions are not suppressed
    }
}
```

## Configuration

### Basic Configuration

```csharp
// Use ordered notification publisher with default settings
services.UseOrderedNotificationPublisher();
```

### Advanced Configuration

```csharp
// Configure exception handling and parallelism
services.UseOrderedNotificationPublisher(
    continueOnException: true,           // Continue even if handlers fail
    maxDegreeOfParallelism: 4            // Max 4 handlers run concurrently
);
```

### Publisher Strategy Comparison

```csharp
// Sequential - handlers execute one at a time (safest, slowest)
services.UseSequentialNotificationPublisher();

// Parallel - all handlers execute concurrently (fastest, least control)
services.UseParallelNotificationPublisher();

// ParallelWhenAll - concurrent with exception tolerance
services.UseParallelWhenAllNotificationPublisher(continueOnException: true);

// Ordered - respects attributes and dependencies (most flexible)
services.UseOrderedNotificationPublisher();
```

## Real-World Examples

### E-Commerce Order Processing

```csharp
public record OrderCreatedNotification(int OrderId, List<OrderItem> Items) : INotification;

// Step 1: Validation (must run first, sequential)
[NotificationHandlerOrder(1)]
[NotificationExecutionMode(NotificationExecutionMode.Sequential)]
public class OrderValidationHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _validator.ValidateOrderAsync(notification.OrderId);
    }
}

// Step 2: Payment Processing (after validation, sequential)
[NotificationHandlerOrder(2)]
[ExecuteAfter(typeof(OrderValidationHandler))]
[NotificationExecutionMode(NotificationExecutionMode.Sequential)]
public class PaymentProcessingHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _paymentService.ProcessPaymentAsync(notification.OrderId);
    }
}

// Step 3: Inventory Update (after payment, sequential - critical)
[NotificationHandlerOrder(3)]
[ExecuteAfter(typeof(PaymentProcessingHandler))]
[NotificationExecutionMode(NotificationExecutionMode.Sequential)]
public class InventoryUpdateHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _inventory.ReserveStockAsync(notification.Items);
    }
}

// Step 4: Notifications (after inventory, parallel - can fail)
[NotificationHandlerGroup("Notifications", groupOrder: 4)]
[NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
public class EmailNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _emailService.SendOrderConfirmationAsync(notification.OrderId);
    }
}

[NotificationHandlerGroup("Notifications", groupOrder: 4)]
[NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
public class SmsNotificationHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _smsService.SendOrderConfirmationAsync(notification.OrderId);
    }
}

// Step 5: Analytics (last, fire-and-forget)
[NotificationHandlerOrder(100)]
[NotificationExecutionMode(NotificationExecutionMode.FireAndForget, SuppressExceptions = true)]
public class AnalyticsHandler : INotificationHandler<OrderCreatedNotification>
{
    public async ValueTask HandleAsync(OrderCreatedNotification notification, CancellationToken ct)
    {
        await _analytics.TrackOrderCreatedAsync(notification.OrderId);
    }
}
```

**Execution Flow:**
1. OrderValidationHandler (sequential)
2. PaymentProcessingHandler (sequential, after validation)
3. InventoryUpdateHandler (sequential, after payment)
4. EmailNotificationHandler + SmsNotificationHandler (parallel, non-critical)
5. AnalyticsHandler (fire-and-forget, non-blocking)

### Banking Transaction Processing

```csharp
public record TransactionCompletedNotification(string TransactionId, decimal Amount) : INotification;

// Critical path - must execute in order
[NotificationHandlerOrder(1)]
[ExecuteB

efore(typeof(AccountUpdateHandler))]
public class TransactionAuditHandler : INotificationHandler<TransactionCompletedNotification>
{
    // Audit logging - must complete first
}

[NotificationHandlerOrder(2)]
[ExecuteAfter(typeof(TransactionAuditHandler))]
[ExecuteBefore(typeof(BalanceNotificationHandler))]
public class AccountUpdateHandler : INotificationHandler<TransactionCompletedNotification>
{
    // Update account balance - critical
}

[NotificationHandlerOrder(3)]
[ExecuteAfter(typeof(AccountUpdateHandler))]
public class BalanceNotificationHandler : INotificationHandler<TransactionCompletedNotification>
{
    // Notify customer of new balance
}

// Non-critical parallel operations
[NotificationHandlerGroup("Reporting", groupOrder: 100)]
[NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
public class ReportingHandler : INotificationHandler<TransactionCompletedNotification>
{
    // Generate reports - can fail without affecting transaction
}

[NotificationHandlerGroup("Reporting", groupOrder: 100)]
[NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
public class ComplianceHandler : INotificationHandler<TransactionCompletedNotification>
{
    // Check compliance rules - can fail without affecting transaction
}
```

## Comparison with MediatR

| Feature | MediatR | Relay |
|---------|---------|-------|
| **Order Attribute** | ❌ Not built-in | ✅ NotificationHandlerOrderAttribute |
| **Dependency Management** | ❌ Not built-in | ✅ ExecuteAfter / ExecuteBefore |
| **Group Execution** | ❌ Not built-in | ✅ NotificationHandlerGroupAttribute |
| **Execution Modes** | ❌ Limited | ✅ Sequential, Parallel, HighPriority, etc. |
| **Exception Control** | ❌ Global only | ✅ Per-handler suppression |
| **Topological Sort** | ❌ Not built-in | ✅ Automatic dependency resolution |
| **Circular Dependency Detection** | ❌ Not available | ✅ Built-in with warnings |
| **Mixed Sequential/Parallel** | ❌ Not available | ✅ Within groups |

## Best Practices

### 1. Use Order for Simple Cases

```csharp
// Simple sequential execution
[NotificationHandlerOrder(1)]
public class FirstHandler { }

[NotificationHandlerOrder(2)]
public class SecondHandler { }

[NotificationHandlerOrder(3)]
public class ThirdHandler { }
```

### 2. Use Dependencies for Complex Relationships

```csharp
// When order is based on dependencies
[ExecuteAfter(typeof(ValidationHandler))]
[ExecuteAfter(typeof(AuthorizationHandler))]
public class ProcessingHandler { }
```

### 3. Use Groups for Parallel Execution

```csharp
// When multiple handlers can run concurrently
[NotificationHandlerGroup("Logging", 1)]
public class FileLogger { }

[NotificationHandlerGroup("Logging", 1)]
public class DatabaseLogger { }

[NotificationHandlerGroup("Logging", 1)]
public class CloudLogger { }
```

### 4. Suppress Non-Critical Failures

```csharp
// For handlers that shouldn't stop the flow
[NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
public class OptionalAnalyticsHandler { }
```

### 5. Avoid Circular Dependencies

```csharp
// ❌ Bad: Circular dependency
[ExecuteAfter(typeof(HandlerB))]
public class HandlerA { }

[ExecuteAfter(typeof(HandlerA))]  // Creates circular dependency!
public class HandlerB { }

// ✅ Good: Clear dependency chain
[ExecuteAfter(typeof(HandlerA))]
public class HandlerB { }

[ExecuteAfter(typeof(HandlerB))]
public class HandlerC { }
```

## Migration from MediatR

If you're using MediatR community packages for handler ordering, migration is straightforward:

### Before (MediatR with community package)

```csharp
// MediatR doesn't have built-in ordering
// Community solutions vary
public class FirstHandler : INotificationHandler<MyNotification>
{
    // No standard way to control order
}
```

### After (Relay)

```csharp
[NotificationHandlerOrder(1)]
public class FirstHandler : INotificationHandler<MyNotification>
{
    // Clear, explicit ordering
}
```

## Performance Considerations

- **Order-based execution**: Minimal overhead, simple sorting
- **Dependency-based execution**: Topological sort adds ~1ms for 100 handlers
- **Group-based execution**: Optimal parallelism within groups
- **Sequential mode**: Safest but slowest
- **Parallel mode**: Fastest but least control
- **Ordered mode**: Best balance of control and performance

## Summary

Relay's notification handler ordering provides:

✅ **MediatR Compatibility**: Familiar patterns for MediatR users  
✅ **Enhanced Control**: Multiple ordering strategies  
✅ **Flexibility**: Mix sequential and parallel execution  
✅ **Safety**: Dependency resolution and circular dependency detection  
✅ **Performance**: Optimized execution with parallelism support  
✅ **Reliability**: Per-handler exception control  

This makes Relay's notification system more mature and feature-rich than MediatR's built-in capabilities!
