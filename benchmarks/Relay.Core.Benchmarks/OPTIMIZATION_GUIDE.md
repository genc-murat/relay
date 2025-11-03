# Transaction System Optimization Guide

This document provides optimization recommendations for the Relay.Core transaction system based on performance benchmarking best practices and common patterns.

## Running Benchmarks

Before optimizing, always run benchmarks to establish a baseline:

```bash
cd benchmarks/Relay.Core.Benchmarks
dotnet run -c Release --filter "*"
```

## Key Performance Areas

### 1. Hot Path Optimization

**Target**: Simple transaction scenarios (most common use case)

**Optimizations**:
- **Minimize allocations**: Use object pooling for frequently created objects
- **Reduce virtual calls**: Consider sealed classes where inheritance isn't needed
- **Inline small methods**: Use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for critical paths
- **Avoid unnecessary async state machines**: Use `ValueTask` instead of `Task` where appropriate

**Implementation Locations**:
- `TransactionBehavior.cs` - Main execution path
- `TransactionContext.cs` - Context creation and management
- `TransactionCoordinator.cs` - Transaction lifecycle management

**Example Optimization**:
```csharp
// Before: Allocates Task
public async Task<IDbTransaction> BeginTransactionAsync(...)
{
    // ...
}

// After: Uses ValueTask to avoid allocation when result is synchronous
public ValueTask<IDbTransaction> BeginTransactionAsync(...)
{
    if (CanReturnSynchronously())
        return new ValueTask<IDbTransaction>(result);
    return new ValueTask<IDbTransaction>(BeginTransactionAsyncCore(...));
}
```

### 2. Metrics Collection Optimization

**Target**: Reduce overhead of metrics collection

**Optimizations**:
- **Use lock-free counters**: Replace locks with `Interlocked` operations
- **Batch metric updates**: Collect metrics in batches rather than per-operation
- **Conditional compilation**: Use `#if` directives to completely remove metrics code when disabled
- **Use struct for metric data**: Avoid heap allocations for metric snapshots

**Implementation Locations**:
- `TransactionMetricsCollector.cs`
- `TransactionHealthCheck.cs`

**Example Optimization**:
```csharp
// Before: Lock-based counter
private readonly object _lock = new();
private long _transactionCount;

public void IncrementCount()
{
    lock (_lock)
    {
        _transactionCount++;
    }
}

// After: Lock-free counter
private long _transactionCount;

public void IncrementCount()
{
    Interlocked.Increment(ref _transactionCount);
}
```

### 3. Context Propagation Optimization

**Target**: AsyncLocal overhead in nested transactions

**Optimizations**:
- **Minimize AsyncLocal reads**: Cache context reference in local variables
- **Reduce context size**: Store only essential data in AsyncLocal
- **Use struct for immutable context data**: Avoid heap allocations
- **Lazy initialization**: Create context objects only when needed

**Implementation Locations**:
- `TransactionContextAccessor.cs`
- `TransactionContext.cs`

**Example Optimization**:
```csharp
// Before: Multiple AsyncLocal reads
public void ProcessTransaction()
{
    var id = _contextAccessor.Current?.TransactionId;
    var level = _contextAccessor.Current?.NestingLevel;
    var isolation = _contextAccessor.Current?.IsolationLevel;
}

// After: Single AsyncLocal read
public void ProcessTransaction()
{
    var context = _contextAccessor.Current;
    if (context != null)
    {
        var id = context.TransactionId;
        var level = context.NestingLevel;
        var isolation = context.IsolationLevel;
    }
}
```

### 4. Savepoint Management Optimization

**Target**: Reduce overhead of savepoint operations

**Optimizations**:
- **Use pooled collections**: Pool Dictionary instances for savepoint storage
- **Optimize name validation**: Use span-based string operations
- **Reduce string allocations**: Use string interning for common savepoint names
- **Batch savepoint operations**: Group multiple savepoint operations when possible

**Implementation Locations**:
- `SavepointManager.cs`
- `Savepoint.cs`

**Example Optimization**:
```csharp
// Before: Allocates new dictionary each time
private Dictionary<string, ISavepoint> _savepoints = new();

// After: Use pooled dictionary
private static readonly ObjectPool<Dictionary<string, ISavepoint>> _dictionaryPool = 
    ObjectPool.Create<Dictionary<string, ISavepoint>>();

private Dictionary<string, ISavepoint> _savepoints = _dictionaryPool.Get();

public void Dispose()
{
    _savepoints.Clear();
    _dictionaryPool.Return(_savepoints);
}
```

### 5. Retry Logic Optimization

**Target**: Minimize overhead when retries aren't needed

**Optimizations**:
- **Fast path for no retries**: Skip retry logic entirely when not configured
- **Optimize delay calculation**: Pre-calculate delays for common scenarios
- **Reduce exception overhead**: Use result objects instead of exceptions for transient errors
- **Cache retry strategies**: Reuse strategy instances

**Implementation Locations**:
- `TransactionRetryHandler.cs`
- `ExponentialBackoffRetryStrategy.cs`
- `LinearRetryStrategy.cs`

**Example Optimization**:
```csharp
// Before: Always creates retry context
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    var context = new RetryContext();
    // ... retry logic
}

// After: Fast path when no retry configured
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    if (_retryPolicy == null || _retryPolicy.MaxRetries == 0)
        return await operation();
    
    return await ExecuteWithRetryCore(operation);
}
```

### 6. Event Publishing Optimization

**Target**: Reduce overhead of transaction event handlers

**Optimizations**:
- **Check for handlers before creating event context**: Avoid allocations when no handlers registered
- **Use cached handler lists**: Avoid repeated handler resolution
- **Parallel execution**: Execute independent handlers in parallel
- **Optimize event context creation**: Use struct or pooled objects

**Implementation Locations**:
- `TransactionEventPublisher.cs`
- `TransactionEventContext.cs`

**Example Optimization**:
```csharp
// Before: Always creates event context
public async Task PublishEventAsync(TransactionEventType eventType)
{
    var context = new TransactionEventContext { /* ... */ };
    await InvokeHandlersAsync(context);
}

// After: Check for handlers first
public async Task PublishEventAsync(TransactionEventType eventType)
{
    if (!HasHandlers(eventType))
        return;
    
    var context = CreateEventContext(eventType);
    await InvokeHandlersAsync(context);
}
```

## Memory Optimization Strategies

### 1. Reduce Allocations

**Techniques**:
- Use `ArrayPool<T>` for temporary buffers
- Use `stackalloc` for small, short-lived arrays
- Reuse StringBuilder instances
- Use `Span<T>` and `Memory<T>` for buffer operations

### 2. Object Pooling

**Candidates for Pooling**:
- TransactionContext instances
- Event context objects
- Metric snapshot objects
- Dictionary instances for savepoints
- StringBuilder for logging

**Implementation**:
```csharp
private static readonly ObjectPool<TransactionContext> _contextPool = 
    ObjectPool.Create(new TransactionContextPoolPolicy());

public ITransactionContext CreateContext()
{
    var context = _contextPool.Get();
    context.Reset();
    return context;
}

public void ReturnContext(TransactionContext context)
{
    _contextPool.Return(context);
}
```

### 3. Struct vs Class

**Use structs for**:
- Small, immutable data (< 16 bytes)
- Frequently created temporary objects
- Data that doesn't need to be boxed

**Examples**:
- Metric snapshots
- Configuration values
- Small result objects

## CPU Optimization Strategies

### 1. Reduce Virtual Calls

**Techniques**:
- Seal classes that don't need inheritance
- Use `sealed override` for virtual methods
- Consider static methods for utilities

### 2. Optimize Branching

**Techniques**:
- Order if-else by likelihood (most common first)
- Use switch expressions for multiple conditions
- Minimize nested conditionals

### 3. Async Optimization

**Techniques**:
- Use `ValueTask` for frequently called methods
- Avoid async for synchronous operations
- Use `ConfigureAwait(false)` in library code
- Cache Task.CompletedTask and ValueTask.CompletedTask

## Benchmarking Best Practices

### 1. Establish Baselines

Always run benchmarks before and after optimizations:

```bash
# Before optimization
dotnet run -c Release --filter "*SimpleTransactionBenchmarks*" > baseline.txt

# After optimization
dotnet run -c Release --filter "*SimpleTransactionBenchmarks*" > optimized.txt

# Compare results
```

### 2. Focus on Real-World Scenarios

Prioritize optimizations that affect:
- Simple transactions (most common)
- Nested transactions (2-3 levels)
- Scenarios without retries or failures

### 3. Measure Impact

For each optimization:
- Measure execution time improvement
- Measure memory allocation reduction
- Verify no regression in other scenarios
- Document the improvement

### 4. Profile Before Optimizing

Use profiling tools to identify actual bottlenecks:
- dotTrace for CPU profiling
- dotMemory for memory profiling
- PerfView for detailed analysis

## Optimization Checklist

Before considering an optimization complete:

- [ ] Benchmark shows measurable improvement (>5%)
- [ ] No regression in other scenarios
- [ ] Memory allocations reduced or unchanged
- [ ] Code remains readable and maintainable
- [ ] Tests still pass
- [ ] Documentation updated

## Common Anti-Patterns to Avoid

### 1. Premature Optimization

Don't optimize without benchmarks showing a problem.

### 2. Micro-Optimizations

Focus on hot paths, not rarely executed code.

### 3. Sacrificing Readability

Maintain code clarity unless performance gain is significant (>20%).

### 4. Ignoring Allocations

CPU time isn't everything - GC pressure matters too.

### 5. Over-Engineering

Simple solutions are often faster and more maintainable.

## Monitoring Performance in Production

### 1. Key Metrics to Track

- Average transaction duration
- P95/P99 transaction duration
- Transaction throughput (transactions/second)
- Memory allocation rate
- GC pause time

### 2. Performance Budgets

Set performance budgets for critical operations:
- Simple transaction: < 1ms overhead
- Nested transaction (2 levels): < 2ms overhead
- Savepoint creation: < 0.5ms
- Retry attempt: < 10ms delay

### 3. Alerting

Set up alerts for:
- Transaction duration > P99 threshold
- High GC pressure (Gen2 collections)
- Memory leaks (growing memory usage)
- Timeout rate > 1%

## Conclusion

Performance optimization is an iterative process:

1. **Measure**: Run benchmarks to establish baseline
2. **Identify**: Find the actual bottlenecks
3. **Optimize**: Apply targeted optimizations
4. **Verify**: Confirm improvements with benchmarks
5. **Monitor**: Track performance in production

Focus on the hot paths and real-world scenarios. Don't optimize code that isn't causing problems.
