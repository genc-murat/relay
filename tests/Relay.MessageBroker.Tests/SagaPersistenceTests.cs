using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Persistence;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SagaPersistenceTests : IDisposable
{
    private readonly InMemorySagaPersistence<OrderSagaData> _inMemoryPersistence;
    private readonly DatabaseSagaPersistence<OrderSagaData> _databasePersistence;
    private readonly InMemorySagaDbContext _dbContext;

    public SagaPersistenceTests()
    {
        _inMemoryPersistence = new InMemorySagaPersistence<OrderSagaData>();
        _dbContext = new InMemorySagaDbContext();
        _databasePersistence = new DatabaseSagaPersistence<OrderSagaData>(_dbContext);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_SaveAndRetrieve_ShouldWork(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_GetByCorrelationId_ShouldWork(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_Update_ShouldOverwriteExisting(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_Delete_ShouldRemoveSaga(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_GetActiveSagas_ShouldReturnOnlyActive(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);

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

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_GetByState_ShouldFilterCorrectly(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);

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
    public async Task DatabasePersistence_Update_ShouldIncrementVersion()
    {
        // Arrange
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
        await _databasePersistence.SaveAsync(data);

        data.Amount = 200m;
        data.State = SagaState.Completed;
        await _databasePersistence.SaveAsync(data);

        var entity = _dbContext.Sagas.FirstOrDefault(s => s.SagaId == sagaId);

        // Assert
        Assert.NotNull(entity);
        Assert.Equal(2, entity!.Version);
        Assert.Equal(SagaState.Completed, entity.State);
    }





    [Fact]
    public async Task SagaWithPersistence_ExecuteAndRestore_ShouldWork()
    {
        // Arrange
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
        await _inMemoryPersistence.SaveAsync(result.Data);

        // Retrieve and check state
        var restored = await _inMemoryPersistence.GetByIdAsync(data.SagaId);

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
        await _inMemoryPersistence.SaveAsync(data);

        // Act - Resume from step 2
        var restored = await _inMemoryPersistence.GetByIdAsync(data.SagaId);
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
        await _inMemoryPersistence.SaveAsync(data);
        var retrieved = await _inMemoryPersistence.GetByIdAsync(data.SagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved!.Metadata.ContainsKey("userId"));
        Assert.True(retrieved.Metadata.ContainsKey("ipAddress"));
        Assert.True(retrieved.Metadata.ContainsKey("timestamp"));
    }

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_GetByCorrelationId_WhenNotFound_ShouldReturnNull(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);

        // Act
        var retrieved = await persistence.GetByCorrelationIdAsync("NON-EXISTENT-CORR-ID");

        // Assert
        Assert.Null(retrieved);
    }

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_Delete_WhenNotFound_ShouldNotThrow(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
        var sagaId = Guid.NewGuid();

        // Act & Assert
        await persistence.DeleteAsync(sagaId); // Should not throw
    }

    [Theory]
    [InlineData("InMemory")]
    [InlineData("Database")]
    public async Task Persistence_GetActiveSagas_WhenNone_ShouldReturnEmpty(string persistenceType)
    {
        // Arrange
        var persistence = GetPersistence(persistenceType);
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

    private ISagaPersistence<OrderSagaData> GetPersistence(string type) =>
        type switch
        {
            "InMemory" => _inMemoryPersistence,
            "Database" => _databasePersistence,
            _ => throw new ArgumentException("Invalid persistence type")
        };
}

