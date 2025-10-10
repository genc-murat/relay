using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Persistence;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaPersistenceTests
{
    [Fact]
    public async Task InMemoryPersistence_SaveAndRetrieve_ShouldWork()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-001",
            OrderId = "ORDER-001",
            Amount = 100m,
            State = SagaState.Running
        };

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data.SagaId, retrieved!.SagaId);
        Assert.Equal(data.CorrelationId, retrieved.CorrelationId);
        Assert.Equal(data.OrderId, retrieved.OrderId);
        Assert.Equal(data.Amount, retrieved.Amount);
        Assert.Equal(data.State, retrieved.State);
    }

    [Fact]
    public async Task InMemoryPersistence_GetByCorrelationId_ShouldWork()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-002",
            OrderId = "ORDER-002",
            Amount = 200m
        };

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByCorrelationIdAsync("CORR-002");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data.SagaId, retrieved!.SagaId);
        Assert.Equal("CORR-002", retrieved.CorrelationId);
    }

    [Fact]
    public async Task InMemoryPersistence_Update_ShouldOverwriteExisting()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var sagaId = Guid.NewGuid();
        var data1 = new OrderSagaData
        {
            SagaId = sagaId,
            CorrelationId = "CORR-003",
            OrderId = "ORDER-003",
            Amount = 100m,
            State = SagaState.Running
        };

        // Act
        await persistence.SaveAsync(data1);
        
        var data2 = new OrderSagaData
        {
            SagaId = sagaId,
            CorrelationId = "CORR-003",
            OrderId = "ORDER-003",
            Amount = 150m,
            State = SagaState.Completed
        };
        await persistence.SaveAsync(data2);

        var retrieved = await persistence.GetByIdAsync(sagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(150m, retrieved!.Amount);
        Assert.Equal(SagaState.Completed, retrieved.State);
    }

    [Fact]
    public async Task InMemoryPersistence_Delete_ShouldRemoveSaga()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-004",
            OrderId = "ORDER-004",
            Amount = 100m
        };

        // Act
        await persistence.SaveAsync(data);
        await persistence.DeleteAsync(data.SagaId);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemoryPersistence_GetActiveSagas_ShouldReturnOnlyActive()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        
        var runningData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-005",
            State = SagaState.Running
        };

        var compensatingData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-006",
            State = SagaState.Compensating
        };

        var completedData = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-007",
            State = SagaState.Completed
        };

        await persistence.SaveAsync(runningData);
        await persistence.SaveAsync(compensatingData);
        await persistence.SaveAsync(completedData);

        // Act
        var activeSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetActiveSagasAsync())
        {
            activeSagas.Add(saga);
        }

        // Assert
        Assert.Equal(2, activeSagas.Count);
        Assert.Contains(activeSagas, s => s.State == SagaState.Running);
        Assert.Contains(activeSagas, s => s.State == SagaState.Compensating);
        Assert.DoesNotContain(activeSagas, s => s.State == SagaState.Completed);
    }

    [Fact]
    public async Task InMemoryPersistence_GetByState_ShouldFilterCorrectly()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        
        for (int i = 0; i < 3; i++)
        {
            await persistence.SaveAsync(new OrderSagaData
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = $"CORR-{i}",
                State = SagaState.Completed
            });
        }

        for (int i = 0; i < 2; i++)
        {
            await persistence.SaveAsync(new OrderSagaData
            {
                SagaId = Guid.NewGuid(),
                CorrelationId = $"CORR-FAIL-{i}",
                State = SagaState.Failed
            });
        }

        // Act
        var completedSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetByStateAsync(SagaState.Completed))
        {
            completedSagas.Add(saga);
        }

        var failedSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetByStateAsync(SagaState.Failed))
        {
            failedSagas.Add(saga);
        }

        // Assert
        Assert.Equal(3, completedSagas.Count);
        Assert.Equal(2, failedSagas.Count);
    }

    [Fact]
    public async Task DatabasePersistence_WithInMemoryContext_ShouldWork()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "CORR-DB-001",
            OrderId = "ORDER-DB-001",
            Amount = 300m,
            State = SagaState.Running
        };

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data.SagaId, retrieved!.SagaId);
        Assert.Equal(data.OrderId, retrieved.OrderId);
        Assert.Equal(data.Amount, retrieved.Amount);
    }

    [Fact]
    public async Task DatabasePersistence_Update_ShouldIncrementVersion()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        
        var sagaId = Guid.NewGuid();
        var data = new OrderSagaData
        {
            SagaId = sagaId,
            CorrelationId = "CORR-DB-002",
            OrderId = "ORDER-DB-002",
            Amount = 100m,
            State = SagaState.Running
        };

        // Act - Save twice
        await persistence.SaveAsync(data);
        
        data.Amount = 200m;
        data.State = SagaState.Completed;
        await persistence.SaveAsync(data);

        var entity = dbContext.Sagas.FirstOrDefault(s => s.SagaId == sagaId);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(2, entity!.Version);
        Assert.Equal(SagaState.Completed, entity.State);
    }

    [Fact]
    public async Task DatabasePersistence_GetActiveSagas_ShouldWork()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        
        await persistence.SaveAsync(new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "ACTIVE-1",
            State = SagaState.Running
        });

        await persistence.SaveAsync(new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "ACTIVE-2",
            State = SagaState.Compensating
        });

        await persistence.SaveAsync(new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "INACTIVE-1",
            State = SagaState.Completed
        });

        // Act
        var activeSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetActiveSagasAsync())
        {
            activeSagas.Add(saga);
        }

        // Assert
        Assert.Equal(2, activeSagas.Count);
    }

    [Fact]
    public async Task DatabasePersistence_Delete_ShouldRemove()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        
        var sagaId = Guid.NewGuid();
        await persistence.SaveAsync(new OrderSagaData
        {
            SagaId = sagaId,
            CorrelationId = "TO-DELETE",
            State = SagaState.Running
        });

        // Act
        await persistence.DeleteAsync(sagaId);
        var retrieved = await persistence.GetByIdAsync(sagaId);

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task SagaWithPersistence_ExecuteAndRestore_ShouldWork()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var saga = new TestOrderSaga();
        
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "SAGA-PERSIST-001",
            OrderId = "ORDER-PERSIST-001",
            Amount = 100m,
            FailAtStep = "ProcessPayment" // Fail at step 2
        };

        // Act - Execute saga and it will fail
        var result = await saga.ExecuteAsync(data);
        await persistence.SaveAsync(result.Data);

        // Retrieve and check state
        var restored = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(SagaState.Compensated, restored!.State);
        Assert.Equal(1, restored.CurrentStep);
        Assert.True(restored.ReserveInventoryExecuted);
        Assert.True(restored.ReserveInventoryCompensated);
    }

    [Fact]
    public async Task SagaWithPersistence_ResumeFromFailure_ShouldContinue()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var saga = new TestOrderSaga();
        
        // First execution - complete first step
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "RESUME-001",
            OrderId = "ORDER-RESUME-001",
            Amount = 100m,
            CurrentStep = 0 // Start from beginning
        };

        // Manually set as if first step completed
        data.ReserveInventoryExecuted = true;
        data.CurrentStep = 1;
        data.State = SagaState.Running;
        await persistence.SaveAsync(data);

        // Act - Resume from step 2
        var restored = await persistence.GetByIdAsync(data.SagaId);
        Assert.NotNull(restored);
        
        var result = await saga.ExecuteAsync(restored!);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(SagaState.Completed, result.Data.State);
        Assert.True(result.Data.ReserveInventoryExecuted); // Was already done
        Assert.True(result.Data.ProcessPaymentExecuted); // Done in this run
        Assert.True(result.Data.ShipOrderExecuted); // Done in this run
    }

    [Fact]
    public async Task SagaMetadata_ShouldBePersisted()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var data = new OrderSagaData
        {
            SagaId = Guid.NewGuid(),
            CorrelationId = "META-001",
            OrderId = "ORDER-META-001",
            Amount = 100m
        };
        
        data.Metadata["userId"] = "user-123";
        data.Metadata["ipAddress"] = "192.168.1.1";
        data.Metadata["timestamp"] = DateTimeOffset.UtcNow.ToString();

        // Act
        await persistence.SaveAsync(data);
        var retrieved = await persistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved!.Metadata.ContainsKey("userId"));
        Assert.True(retrieved.Metadata.ContainsKey("ipAddress"));
        Assert.True(retrieved.Metadata.ContainsKey("timestamp"));
    }

    [Fact]
    public async Task InMemoryPersistence_GetByCorrelationId_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();

        // Act
        var retrieved = await persistence.GetByCorrelationIdAsync("NON-EXISTENT-CORR-ID");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemoryPersistence_Delete_WhenNotFound_ShouldNotThrow()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        var sagaId = Guid.NewGuid();

        // Act & Assert
        await persistence.DeleteAsync(sagaId); // Should not throw
    }

    [Fact]
    public async Task DatabasePersistence_GetByCorrelationId_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);

        // Act
        var retrieved = await persistence.GetByCorrelationIdAsync("NON-EXISTENT-CORR-ID");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DatabasePersistence_Delete_WhenNotFound_ShouldNotThrow()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        var sagaId = Guid.NewGuid();

        // Act & Assert
        await persistence.DeleteAsync(sagaId); // Should not throw
    }

    [Fact]
    public async Task InMemoryPersistence_GetActiveSagas_WhenNone_ShouldReturnEmpty()
    {
        // Arrange
        var persistence = new InMemorySagaPersistence<OrderSagaData>();
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Completed });
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Failed });

        // Act
        var activeSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetActiveSagasAsync())
        {
            activeSagas.Add(saga);
        }

        // Assert
        Assert.Empty(activeSagas);
    }

    [Fact]
    public async Task DatabasePersistence_GetActiveSagas_WhenNone_ShouldReturnEmpty()
    {
        // Arrange
        var dbContext = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<OrderSagaData>(dbContext);
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Completed });
        await persistence.SaveAsync(new OrderSagaData { State = SagaState.Failed });

        // Act
        var activeSagas = new List<OrderSagaData>();
        await foreach (var saga in persistence.GetActiveSagasAsync())
        {
            activeSagas.Add(saga);
        }

        // Assert
        Assert.Empty(activeSagas);
    }
}
