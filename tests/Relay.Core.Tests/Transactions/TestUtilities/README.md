# Transaction Test Utilities

This directory contains comprehensive test utilities for testing transaction functionality in Relay.Core.

## Overview

The test utilities provide four main components:

1. **InMemoryUnitOfWork** - In-memory implementation of IUnitOfWork for testing
2. **TransactionSpy** - Spy implementation that records all operations for verification
3. **TransactionTestFixture** - Test fixture with automatic setup and cleanup
4. **TransactionFailureSimulator** - Utilities for simulating various failure scenarios

## Components

### InMemoryUnitOfWork

An in-memory implementation of `IUnitOfWork` that simulates transaction operations without requiring a real database.

**Features:**
- Records all operations in an operation log
- Supports all transaction features (savepoints, isolation levels, read-only mode)
- Configurable to throw exceptions on specific operations
- Tracks call counts and last isolation level used

**Example Usage:**
```csharp
var unitOfWork = new InMemoryUnitOfWork();

// Begin a transaction
var transaction = await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);

// Perform operations
await unitOfWork.SaveChangesAsync();

// Commit
await transaction.CommitAsync();

// Verify operations
Assert.Contains("BeginTransaction(ReadCommitted)", unitOfWork.OperationLog);
Assert.Contains("SaveChanges", unitOfWork.OperationLog);
Assert.Equal(IsolationLevel.ReadCommitted, unitOfWork.LastIsolationLevel);
```

**Configuration Options:**
```csharp
var unitOfWork = new InMemoryUnitOfWork
{
    ShouldThrowOnSave = true,
    SaveException = new InvalidOperationException("Database error"),
    ShouldThrowOnBeginTransaction = false
};
```

### TransactionSpy

A spy implementation that records all transaction operations and provides verification methods.

**Features:**
- Records all operations with timestamps and metadata
- Provides fluent verification methods
- Supports operation order verification
- Tracks operation counts

**Example Usage:**
```csharp
var spy = new TransactionSpy();

// Perform operations
var transaction = await spy.BeginTransactionAsync(IsolationLevel.Serializable);
await spy.SaveChangesAsync();
await transaction.CommitAsync();

// Verify operations
spy.VerifyTransactionBegan(IsolationLevel.Serializable);
spy.VerifySaveChangesCalled();
spy.VerifyTransactionCommitted();

// Verify operation order
spy.VerifyOperationOrder(
    TransactionOperationType.BeginTransaction,
    TransactionOperationType.SaveChanges,
    TransactionOperationType.Commit);

// Verify operation counts
spy.VerifyOperationCount(TransactionOperationType.SaveChanges, 1);
```

**Verification Methods:**
- `VerifyTransactionBegan(IsolationLevel?)` - Verifies transaction was begun
- `VerifyTransactionCommitted()` - Verifies transaction was committed
- `VerifyTransactionRolledBack()` - Verifies transaction was rolled back
- `VerifySaveChangesCalled()` - Verifies SaveChanges was called
- `VerifySavepointCreated(string)` - Verifies savepoint was created
- `VerifyRollbackToSavepoint(string)` - Verifies rollback to savepoint
- `VerifyOperationOrder(params TransactionOperationType[])` - Verifies operation order
- `VerifyOperationCount(TransactionOperationType, int)` - Verifies operation count

### TransactionTestFixture

A test fixture that provides automatic setup and cleanup for transaction tests.

**Features:**
- Implements `IAsyncLifetime` for proper async initialization
- Automatic transaction rollback on disposal
- Service provider integration
- Helper methods for common test scenarios
- Configurable options

**Example Usage:**
```csharp
public class MyTransactionTests : IAsyncLifetime
{
    private TransactionTestFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new TransactionTestFixture
        {
            UseInMemoryUnitOfWork = false, // Use spy by default
            AutoRollback = true,
            ConfigureOptions = options =>
            {
                options.DefaultTimeout = TimeSpan.FromSeconds(10);
            }
        };
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }

    [Fact]
    public async Task My_Test()
    {
        // Use the fixture
        await _fixture.ExecuteInTransactionAsync(async () =>
        {
            await _fixture.UnitOfWork.SaveChangesAsync();
        }, IsolationLevel.ReadCommitted);

        // Verify
        _fixture.TransactionSpy.VerifyTransactionBegan();
    }
}
```

**Alternative: Using TransactionTestBase**
```csharp
public class MyTransactionTests : TransactionTestBase
{
    protected override void ConfigureFixture(TransactionTestFixture fixture)
    {
        fixture.UseInMemoryUnitOfWork = true;
        fixture.AutoRollback = true;
    }

    [Fact]
    public async Task My_Test()
    {
        // Fixture is automatically initialized
        await Fixture.ExecuteInTransactionAsync(async () =>
        {
            await Fixture.UnitOfWork.SaveChangesAsync();
        });
    }
}
```

### TransactionFailureSimulator

Utilities for simulating various transaction failure scenarios.

**Features:**
- Simulate failures at different points (BeginTransaction, SaveChanges, Commit, Rollback)
- Simulate transient errors (deadlocks, timeouts, connection errors)
- Simulate slow operations
- Fail on Nth call
- Fluent builder API for complex scenarios

**Example Usage:**

**Simple Failures:**
```csharp
// Fail on SaveChanges
var unitOfWork = TransactionFailureSimulator.CreateFailOnSaveChanges(
    new InvalidOperationException("Database error"));

// Fail with transient error
var unitOfWork = TransactionFailureSimulator.CreateFailOnSaveChangesWithTransientError();

// Fail on Nth call
var unitOfWork = TransactionFailureSimulator.CreateFailOnNthSaveChanges(2);
```

**Complex Scenarios with Builder:**
```csharp
var unitOfWork = new TransactionFailureScenarioBuilder()
    .FailOnNthSaveChanges(3)
    .WithTransientError()
    .WithDelay(TimeSpan.FromMilliseconds(100))
    .Build();
```

**Simulating Slow Operations:**
```csharp
var unitOfWork = TransactionFailureSimulator.CreateSlowUnitOfWork(
    TimeSpan.FromSeconds(1));

// Operations will be delayed by 1 second
await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);
```

**Testing Timeout Scenarios:**
```csharp
var cts = TransactionFailureSimulator.CreateCancellationTokenSource(
    TimeSpan.FromMilliseconds(100));

var unitOfWork = TransactionFailureSimulator.CreateSlowUnitOfWork(
    TimeSpan.FromSeconds(1));

// This will timeout
await Assert.ThrowsAsync<OperationCanceledException>(async () =>
    await unitOfWork.BeginTransactionAsync(
        IsolationLevel.ReadCommitted, 
        cts.Token));
```

## Common Test Patterns

### Testing Transaction Lifecycle
```csharp
var spy = new TransactionSpy();

var transaction = await spy.BeginTransactionAsync(IsolationLevel.ReadCommitted);
await spy.SaveChangesAsync();
await transaction.CommitAsync();

spy.VerifyOperationOrder(
    TransactionOperationType.BeginTransaction,
    TransactionOperationType.SaveChanges,
    TransactionOperationType.Commit);
```

### Testing Rollback on Failure
```csharp
var spy = new TransactionSpy();
spy.ShouldThrowOnSave = true;

var transaction = await spy.BeginTransactionAsync(IsolationLevel.ReadCommitted);

await Assert.ThrowsAsync<InvalidOperationException>(
    async () => await spy.SaveChangesAsync());

await transaction.RollbackAsync();

spy.VerifyTransactionRolledBack();
```

### Testing Savepoints
```csharp
var unitOfWork = new InMemoryUnitOfWork();
await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);

var savepoint = await unitOfWork.CreateSavepointAsync("checkpoint");
await unitOfWork.SaveChangesAsync();
await unitOfWork.RollbackToSavepointAsync("checkpoint");

Assert.Contains("CreateSavepoint(checkpoint)", unitOfWork.OperationLog);
Assert.Contains("RollbackToSavepoint(checkpoint)", unitOfWork.OperationLog);
```

### Testing Retry Logic
```csharp
var unitOfWork = TransactionFailureSimulator.CreateFailOnNthSaveChanges(2);

// First attempt succeeds
await unitOfWork.SaveChangesAsync();

// Second attempt fails (can be retried)
await Assert.ThrowsAsync<InvalidOperationException>(
    async () => await unitOfWork.SaveChangesAsync());

// Third attempt succeeds
await unitOfWork.SaveChangesAsync();
```

### Testing Read-Only Transactions
```csharp
var unitOfWork = new InMemoryUnitOfWork();
unitOfWork.IsReadOnly = true;

await Assert.ThrowsAsync<ReadOnlyTransactionViolationException>(
    async () => await unitOfWork.SaveChangesAsync());
```

## Best Practices

1. **Use TransactionSpy for behavior verification** - When you need to verify that specific operations were called in a specific order.

2. **Use InMemoryUnitOfWork for state testing** - When you need to test the state of the unit of work or simulate complex scenarios.

3. **Use TransactionTestFixture for integration tests** - When you need a complete test environment with dependency injection.

4. **Use TransactionFailureSimulator for error handling tests** - When you need to test how your code handles various failure scenarios.

5. **Reset state between tests** - Always call `Reset()` on test doubles when reusing them across tests.

6. **Use the builder pattern for complex scenarios** - The `TransactionFailureScenarioBuilder` makes it easy to create complex failure scenarios.

## Requirements Mapping

These test utilities fulfill the following requirements from the design document:

- **Requirement 10.1**: InMemoryUnitOfWork provides a test double implementation
- **Requirement 10.2**: TransactionSpy provides operation recording and verification
- **Requirement 10.3**: TransactionFailureSimulator provides failure simulation
- **Requirement 10.5**: TransactionTestFixture provides test infrastructure with automatic rollback

## See Also

- [Transaction Design Document](../../../../.kiro/specs/relay-transactions-enhancement/design.md)
- [Transaction Requirements](../../../../.kiro/specs/relay-transactions-enhancement/requirements.md)
- [Example Tests](./TestUtilitiesExampleTests.cs)
- [Basic Tests](./BasicTestUtilitiesTests.cs)
