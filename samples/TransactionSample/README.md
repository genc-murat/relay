# Transaction & Unit of Work Sample

This sample demonstrates Relay's transaction management and Unit of Work pattern integration with Entity Framework Core.

## Features Demonstrated

### 1. **ITransactionalRequest Interface**
Marker interface for requests that should be wrapped in a database transaction:
- Automatic transaction creation
- Commit on success
- Rollback on exception
- Configurable isolation levels and timeouts

### 2. **IUnitOfWork Interface**
Abstraction for managing transactional operations:
- Automatic `SaveChangesAsync` after handler execution
- Works with EF Core DbContext
- Compatible with any data access pattern
- Optional: Save only for transactional requests

### 3. **TransactionBehavior**
Pipeline behavior that manages transactions:
```csharp
services.AddRelayTransactions(
    scopeOption: TransactionScopeOption.Required,
    isolationLevel: IsolationLevel.ReadCommitted,
    timeout: TimeSpan.FromMinutes(1));
```

### 4. **UnitOfWorkBehavior**
Pipeline behavior that automatically saves changes:
```csharp
services.AddRelayUnitOfWork(saveOnlyForTransactionalRequests: false);
```

## Running the Sample

```bash
cd samples/TransactionSample
dotnet run
```

## Architecture

```
Request (ITransactionalRequest)
  ↓
TransactionBehavior (creates TransactionScope)
  ↓
UnitOfWorkBehavior (calls SaveChangesAsync)
  ↓
Handler (business logic)
  ↓
SaveChanges (automatic)
  ↓
Transaction Commit (automatic)
```

## Key Concepts

### Transactional Commands

```csharp
public record CreateOrderCommand(int CustomerId, string[] Items, decimal TotalAmount)
    : IRequest<Order>, ITransactionalRequest<Order>;
```

**Benefits:**
- ✅ Automatic transaction wrapping
- ✅ Automatic SaveChanges
- ✅ Rollback on exception
- ✅ ACID guarantees

### Non-Transactional Queries

```csharp
public record GetOrderQuery(int OrderId) : IRequest<Order?>;
```

**Benefits:**
- ✅ No transaction overhead
- ✅ Better performance for reads
- ✅ No locks held

### EF Core Integration

```csharp
public class OrderDbContext : DbContext, IUnitOfWork
{
    // IUnitOfWork is automatically satisfied by DbContext.SaveChangesAsync
}

services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrdersDb"));

services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderDbContext>());
```

## Configuration Examples

### 1. Basic Setup (Default Settings)

```csharp
services.AddRelayTransactions();
services.AddRelayUnitOfWork();
```

**Defaults:**
- Scope Option: `Required`
- Isolation Level: `ReadCommitted`
- Timeout: 1 minute
- Save for all requests

### 2. Custom Transaction Settings

```csharp
services.AddRelayTransactions(
    scopeOption: TransactionScopeOption.RequiresNew,
    isolationLevel: IsolationLevel.Serializable,
    timeout: TimeSpan.FromMinutes(5));
```

### 3. Save Only for Transactional Requests

```csharp
services.AddRelayUnitOfWork(saveOnlyForTransactionalRequests: true);
```

**Use this when:**
- You want explicit control over when SaveChanges is called
- You have both commands (transactional) and queries (read-only)
- You want to optimize performance by skipping SaveChanges for queries

## Real-World Use Cases

### E-Commerce Order Processing

```csharp
public record PlaceOrderCommand(int CustomerId, CartItem[] Items, PaymentInfo Payment)
    : IRequest<OrderResult>, ITransactionalRequest<OrderResult>;

public class PlaceOrderHandler
{
    public async ValueTask<OrderResult> Handle(...)
    {
        // 1. Validate inventory
        // 2. Process payment
        // 3. Create order
        // 4. Update inventory
        // 5. Send notifications

        // All within a single transaction
        // SaveChanges automatic
        // Rollback if any step fails
    }
}
```

### Banking Transfers

```csharp
public record TransferMoneyCommand(int FromAccount, int ToAccount, decimal Amount)
    : IRequest<TransferResult>, ITransactionalRequest<TransferResult>;

public class TransferMoneyHandler
{
    public async ValueTask<TransferResult> Handle(...)
    {
        // 1. Debit from account
        // 2. Credit to account
        // 3. Record transaction

        // ACID guarantees ensure both accounts update or neither
    }
}
```

### Multi-Entity Updates

```csharp
public record UpdateUserProfileCommand(int UserId, ProfileData Data)
    : IRequest<User>, ITransactionalRequest<User>;

public class UpdateUserProfileHandler
{
    public async ValueTask<User> Handle(...)
    {
        // 1. Update User entity
        // 2. Update Address entity
        // 3. Update Preferences entity
        // 4. Add audit log entry

        // All changes committed together
    }
}
```

## Transaction Isolation Levels

| Level | Description | Use Case |
|-------|-------------|----------|
| **ReadUncommitted** | Lowest isolation, allows dirty reads | High performance, no consistency needed |
| **ReadCommitted** | ⭐ Default, prevents dirty reads | Most common scenarios |
| **RepeatableRead** | Prevents non-repeatable reads | Consistent data during transaction |
| **Serializable** | Highest isolation, full locks | Critical financial operations |
| **Snapshot** | Uses row versioning | SQL Server optimistic concurrency |

## Error Handling

### Automatic Rollback

```csharp
// If handler throws, transaction automatically rolls back
public async ValueTask<Order> CreateOrder(...)
{
    if (command.TotalAmount <= 0)
        throw new InvalidOperationException("Invalid amount");

    // Transaction will rollback, no changes saved
}
```

### Exception Propagation

```csharp
try
{
    await relay.SendAsync(new CreateOrderCommand(...));
}
catch (InvalidOperationException ex)
{
    // Transaction was rolled back
    // No changes persisted to database
    logger.LogError(ex, "Order creation failed");
}
```

## Performance Considerations

### Transaction Overhead

```
Without Transaction:
  Handler execution: 10ms
  Total: 10ms

With Transaction:
  Transaction begin: 1ms
  Handler execution: 10ms
  SaveChanges: 5ms
  Transaction commit: 2ms
  Total: 18ms (1.8x slower)
```

**Recommendation:** Only use transactions for write operations (commands), not for read operations (queries).

### SaveChanges Batching

```csharp
// Instead of multiple SaveChanges:
await _dbContext.SaveChangesAsync();  // Bad
await _dbContext.SaveChangesAsync();  // Bad
await _dbContext.SaveChangesAsync();  // Bad

// UnitOfWorkBehavior does one SaveChanges at the end:
// ... all entity changes ...
// SaveChanges (automatic, once)  // Good
```

## Best Practices

### 1. **Use Marker Interfaces**
```csharp
// Command (modifies data)
public record CreateOrderCommand : IRequest<Order>, ITransactionalRequest<Order>;

// Query (reads data)
public record GetOrderQuery : IRequest<Order?>;
```

### 2. **Keep Transactions Short**
```csharp
// ❌ Bad: Long-running transaction
public async ValueTask<Order> CreateOrder(...)
{
    await Task.Delay(5000);  // Don't do this!
    await _emailService.SendAsync(...);  // External calls!
    return order;
}

// ✅ Good: Quick transaction
public async ValueTask<Order> CreateOrder(...)
{
    var order = new Order { ... };
    _dbContext.Orders.Add(order);
    return order;  // SaveChanges happens immediately after
}

// Send emails in a separate handler or background job
```

### 3. **Handle Concurrency**
```csharp
public class Order
{
    [Timestamp]
    public byte[] RowVersion { get; set; }
}

try
{
    await relay.SendAsync(new UpdateOrderCommand(...));
}
catch (DbUpdateConcurrencyException)
{
    // Handle optimistic concurrency conflict
}
```

### 4. **Use Appropriate Isolation Levels**
```csharp
// For reads: ReadCommitted (default)
services.AddRelayTransactions(isolationLevel: IsolationLevel.ReadCommitted);

// For critical writes: Serializable
services.AddRelayTransactions(isolationLevel: IsolationLevel.Serializable);
```

## Testing

### Integration Testing

```csharp
[Test]
public async Task CreateOrder_Should_SaveChanges_And_CommitTransaction()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddDbContext<OrderDbContext>(options =>
        options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderDbContext>());
    services.AddRelay(typeof(Program).Assembly);
    services.AddRelayTransactions();
    services.AddRelayUnitOfWork();

    var provider = services.BuildServiceProvider();
    var relay = provider.GetRequiredService<IRelay>();

    // Act
    var order = await relay.SendAsync(new CreateOrderCommand(1, new[] { "A" }, 100m));

    // Assert
    var dbContext = provider.GetRequiredService<OrderDbContext>();
    var savedOrder = await dbContext.Orders.FindAsync(order.Id);
    Assert.NotNull(savedOrder);
}
```

## MediatR Compatibility

This implementation follows the same patterns as MediatR community packages:

```csharp
// MediatR.Extensions.TransactionBehavior (community package)
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactional { }

// Relay (built-in)
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ITransactionalRequest { }
```

## Migration from MediatR

```csharp
// MediatR (community packages)
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
services.AddScoped<IUnitOfWork, MyDbContext>();

// Relay (built-in)
services.AddRelayTransactions();
services.AddRelayUnitOfWork();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
```

## Advanced Scenarios

### Nested Transactions

```csharp
public async ValueTask<Order> CreateOrder(...)
{
    // Outer transaction
    var order = new Order { ... };
    _dbContext.Orders.Add(order);

    // Inner "transaction" (participates in outer)
    await relay.SendAsync(new CreateInvoiceCommand(order.Id));

    // Both commit together
}
```

### Custom Transaction Options

```csharp
services.Configure<TransactionOptions>(options =>
{
    options.ScopeOption = TransactionScopeOption.Required;
    options.IsolationLevel = IsolationLevel.ReadCommitted;
    options.Timeout = TimeSpan.FromMinutes(2);
});
```

## Benefits

✅ **Automatic Transaction Management**: No manual `BeginTransaction`/`Commit`/`Rollback`
✅ **Automatic SaveChanges**: No manual `SaveChangesAsync` calls
✅ **ACID Guarantees**: Data consistency and integrity
✅ **Rollback on Exception**: Automatic cleanup on errors
✅ **Configurable**: Control isolation levels, timeouts, scope options
✅ **Testable**: Easy to test with in-memory database
✅ **MediatR Compatible**: Same patterns as MediatR community packages

## Troubleshooting

### Problem: "Transaction has aborted"

**Solution:** Check your timeout settings and ensure handlers complete quickly.

```csharp
services.AddRelayTransactions(timeout: TimeSpan.FromMinutes(5));
```

### Problem: "Ambient transaction detected"

**Solution:** Ensure you're not manually creating TransactionScope in handlers.

```csharp
// ❌ Don't do this
public async ValueTask<Order> CreateOrder(...)
{
    using var scope = new TransactionScope();  // Conflicts with behavior!
}

// ✅ Do this
public async ValueTask<Order> CreateOrder(...)
{
    // TransactionBehavior handles it automatically
}
```

### Problem: SaveChanges not called

**Solution:** Ensure UnitOfWorkBehavior is registered:

```csharp
services.AddRelayUnitOfWork();
```
