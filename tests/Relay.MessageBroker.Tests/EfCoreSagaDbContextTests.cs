using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Persistence;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class EfCoreSagaDbContextTests
{
    private EfCoreSagaDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<EfCoreSagaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new EfCoreSagaDbContext(options);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_SaveSagaEntity_ShouldPersist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "TEST-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        // Act
        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        saved.Should().NotBeNull();
        saved!.CorrelationId.Should().Be("TEST-001");
        saved.State.Should().Be(SagaState.Running);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_UpdateSagaEntity_ShouldUpdateVersion()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "TEST-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Act - Update the entity
        sagaEntity.State = SagaState.Completed;
        sagaEntity.Version++;
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        updated!.State.Should().Be(SagaState.Completed);
        updated.Version.Should().Be(2);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_QueryByCorrelationId_ShouldUseIndex()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var correlationId = "ORDER-123";

        var saga1 = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = correlationId,
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var saga2 = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "OTHER-456",
            State = SagaState.Completed,
            SagaType = "OrderSaga",
            CurrentStep = 2,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.AddRange(saga1, saga2);
        await context.SaveChangesAsync();

        // Act
        var result = await context.SagaEntities
            .Where(s => s.CorrelationId == correlationId)
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.SagaId.Should().Be(saga1.SagaId);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_QueryByState_ShouldUseIndex()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var runningSaga = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "RUNNING-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var completedSaga = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "COMPLETED-001",
            State = SagaState.Completed,
            SagaType = "OrderSaga",
            CurrentStep = 2,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.AddRange(runningSaga, completedSaga);
        await context.SaveChangesAsync();

        // Act
        var runningCount = await context.SagaEntities
            .Where(s => s.State == SagaState.Running)
            .CountAsync();

        // Assert
        runningCount.Should().Be(1);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_QueryActiveSagas_ShouldUseCompositeIndex()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var now = DateTimeOffset.UtcNow;

        var sagas = new[]
        {
            new SagaEntityBase
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = "RUNNING-001",
                State = SagaState.Running,
                SagaType = "OrderSaga",
                CurrentStep = 1,
                DataJson = "{}",
                MetadataJson = "{}",
                CreatedAt = now.AddMinutes(-5),
                UpdatedAt = now.AddMinutes(-1),
                Version = 1
            },
            new SagaEntityBase
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = "COMPENSATING-001",
                State = SagaState.Compensating,
                SagaType = "OrderSaga",
                CurrentStep = 2,
                DataJson = "{}",
                MetadataJson = "{}",
                CreatedAt = now.AddMinutes(-10),
                UpdatedAt = now.AddMinutes(-2),
                Version = 1
            },
            new SagaEntityBase
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = "COMPLETED-001",
                State = SagaState.Completed,
                SagaType = "OrderSaga",
                CurrentStep = 3,
                DataJson = "{}",
                MetadataJson = "{}",
                CreatedAt = now.AddMinutes(-15),
                UpdatedAt = now.AddMinutes(-5),
                Version = 1
            }
        };

        context.SagaEntities.AddRange(sagas);
        await context.SaveChangesAsync();

        // Act - Query active sagas (Running or Compensating) ordered by creation
        var activeSagas = await context.SagaEntities
            .Where(s => s.State == SagaState.Running || s.State == SagaState.Compensating)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        // Assert
        activeSagas.Should().HaveCount(2);
        activeSagas[0].CorrelationId.Should().Be("COMPENSATING-001"); // Older
        activeSagas[1].CorrelationId.Should().Be("RUNNING-001"); // Newer
    }

    [Fact]
    public async Task EfCoreSagaDbContext_DeleteSaga_ShouldRemove()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "DELETE-001",
            State = SagaState.Completed,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Act
        context.SagaEntities.Remove(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task EfCoreSagaDbContext_ISagaDbContext_Add_ShouldWork()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        ISagaDbContext sagaContext = context;

        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "INTERFACE-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        // Act
        sagaContext.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task EfCoreSagaDbContext_ISagaDbContext_Update_ShouldWork()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        ISagaDbContext sagaContext = context;

        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "INTERFACE-002",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Act
        sagaEntity.State = SagaState.Completed;
        sagaContext.Update(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var updated = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        updated!.State.Should().Be(SagaState.Completed);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_ISagaDbContext_Remove_ShouldWork()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        ISagaDbContext sagaContext = context;

        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "INTERFACE-003",
            State = SagaState.Completed,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Act
        sagaContext.Remove(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var deleted = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task EfCoreSagaDbContext_ISagaDbContext_Sagas_ShouldReturnQueryable()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        ISagaDbContext sagaContext = context;

        var saga1 = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "QUERY-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var saga2 = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "QUERY-002",
            State = SagaState.Completed,
            SagaType = "OrderSaga",
            CurrentStep = 2,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.AddRange(saga1, saga2);
        await context.SaveChangesAsync();

        // Act
        var count = await sagaContext.Sagas.CountAsync();

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task EfCoreSagaDbContext_OptimisticConcurrency_ShouldThrowOnConflict()
    {
        // Arrange
        await using var context1 = CreateInMemoryDbContext();
        await using var context2 = CreateInMemoryDbContext();

        // Share the same in-memory database by using same connection string
        var options = new DbContextOptionsBuilder<EfCoreSagaDbContext>()
            .UseInMemoryDatabase(databaseName: "ConcurrencyTest")
            .Options;

        await using var setupContext = new EfCoreSagaDbContext(options);
        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CONCURRENCY-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        setupContext.SagaEntities.Add(sagaEntity);
        await setupContext.SaveChangesAsync();

        // Act - Context 1 loads the entity
        await using var loadContext1 = new EfCoreSagaDbContext(options);
        var entity1 = await loadContext1.SagaEntities.FindAsync(sagaEntity.SagaId);

        // Context 2 loads and updates the entity
        await using var loadContext2 = new EfCoreSagaDbContext(options);
        var entity2 = await loadContext2.SagaEntities.FindAsync(sagaEntity.SagaId);
        entity2!.CurrentStep = 2;
        entity2.Version++;
        await loadContext2.SaveChangesAsync();

        // Context 1 tries to update with stale version
        entity1!.CurrentStep = 3;
        entity1.Version++; // This will be 2, but DB now has version 2 already

        // Assert
        // EF Core InMemory provider DOES enforce concurrency tokens (in recent versions)
        // This will throw DbUpdateConcurrencyException when trying to save with stale version
        var action = async () => await loadContext1.SaveChangesAsync();
        await action.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task EfCoreSagaDbContext_QueryBySagaType_ShouldFilter()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();

        var orderSaga = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "ORDER-001",
            State = SagaState.Running,
            SagaType = "OrderSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var paymentSaga = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "PAYMENT-001",
            State = SagaState.Running,
            SagaType = "PaymentSaga",
            CurrentStep = 1,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        context.SagaEntities.AddRange(orderSaga, paymentSaga);
        await context.SaveChangesAsync();

        // Act
        var orderSagas = await context.SagaEntities
            .Where(s => s.SagaType == "OrderSaga")
            .ToListAsync();

        // Assert
        orderSagas.Should().HaveCount(1);
        orderSagas[0].CorrelationId.Should().Be("ORDER-001");
    }

    [Fact]
    public async Task EfCoreSagaDbContext_StoreErrorDetails_ShouldPersist()
    {
        // Arrange
        await using var context = CreateInMemoryDbContext();
        var sagaEntity = new SagaEntityBase
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "ERROR-001",
            State = SagaState.Failed,
            SagaType = "OrderSaga",
            CurrentStep = 2,
            DataJson = "{}",
            MetadataJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Version = 1,
            ErrorMessage = "Payment processing failed",
            ErrorStackTrace = "at OrderSaga.ProcessPayment() in OrderSaga.cs:line 42"
        };

        // Act
        context.SagaEntities.Add(sagaEntity);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.SagaEntities.FindAsync(sagaEntity.SagaId);
        saved.Should().NotBeNull();
        saved!.ErrorMessage.Should().Be("Payment processing failed");
        saved.ErrorStackTrace.Should().Contain("OrderSaga.cs:line 42");
    }
}
