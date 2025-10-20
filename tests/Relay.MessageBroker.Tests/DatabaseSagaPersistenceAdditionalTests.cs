using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Interfaces;
using Relay.MessageBroker.Saga.Persistence;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DatabaseSagaPersistenceAdditionalTests
{
    [Fact]
    public void DatabaseSagaPersistence_Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new DatabaseSagaPersistence<TestSagaData>(null!));
        Assert.Equal("dbContext", exception.ParamName);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_SaveAsync_WithNewSaga_ShouldCreateEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);
        var sagaData = new TestSagaData
        {
            Value = 42,
            State = SagaState.Running
        };

        // Act
        await persistence.SaveAsync(sagaData);

        // Assert
        Assert.Single(context.Sagas);
        var entity = context.Sagas.First();
        Assert.Equal(sagaData.SagaId, entity.SagaId);
        Assert.Equal(SagaState.Running, entity.State);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_SaveAsync_WithExistingSaga_ShouldUpdateEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);
        var sagaData = new TestSagaData
        {
            Value = 42,
            State = SagaState.Running
        };

        await persistence.SaveAsync(sagaData);

        // Act - Update the saga
        sagaData.State = SagaState.Completed;
        sagaData.Value = 100;
        await persistence.SaveAsync(sagaData);

        // Assert
        Assert.Single(context.Sagas);
        var entity = context.Sagas.First();
        Assert.Equal(SagaState.Completed, entity.State);
        Assert.Equal(2, entity.Version);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_GetByIdAsync_WithExistingSaga_ShouldReturnData()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);
        var originalData = new TestSagaData
        {
            Value = 42,
            State = SagaState.Running
        };
        await persistence.SaveAsync(originalData);

        // Act
        var result = await persistence.GetByIdAsync(originalData.SagaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(originalData.SagaId, result!.SagaId);
        Assert.Equal(42, result.Value);
        Assert.Equal(SagaState.Running, result.State);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);

        // Act
        var result = await persistence.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_GetByCorrelationIdAsync_WithExistingSaga_ShouldReturnData()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);
        var correlationId = "CORR-123";
        var sagaData = new TestSagaData
        {
            CorrelationId = correlationId,
            Value = 42
        };
        await persistence.SaveAsync(sagaData);

        // Act
        var result = await persistence.GetByCorrelationIdAsync(correlationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(correlationId, result!.CorrelationId);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task DatabaseSagaPersistence_DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var persistence = new DatabaseSagaPersistence<TestSagaData>(context);
        var sagaData = new TestSagaData { Value = 42 };
        await persistence.SaveAsync(sagaData);

        // Act
        await persistence.DeleteAsync(sagaData.SagaId);

        // Assert
        Assert.Empty(context.Sagas);
    }

    [Fact]
    public void SagaEntityBase_DefaultValues_ShouldBeSet()
    {
        // Act
        var entity = new SagaEntityBase();

        // Assert - Just check that the entity can be created with default values
        Assert.NotNull(entity);
        Assert.NotNull(entity.CorrelationId);
        Assert.Equal(SagaState.NotStarted, entity.State);
        Assert.Equal(0, entity.CurrentStep);
        Assert.NotNull(entity.SagaType);
        Assert.NotNull(entity.DataJson);
        Assert.Equal(0, entity.Version);
    }

    [Fact]
    public void SagaEntityBase_CanSetAllProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var correlationId = "CORR-123";
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        var entity = new SagaEntityBase
        {
            SagaId = sagaId,
            CorrelationId = correlationId,
            State = SagaState.Completed,
            CurrentStep = 3,
            SagaType = "TestSaga",
            DataJson = "{\"value\":42}",
            MetadataJson = "{\"key\":\"value\"}",
            Version = 5,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        Assert.Equal(sagaId, entity.SagaId);
        Assert.Equal(correlationId, entity.CorrelationId);
        Assert.Equal(SagaState.Completed, entity.State);
        Assert.Equal(3, entity.CurrentStep);
        Assert.Equal("TestSaga", entity.SagaType);
        Assert.Equal("{\"value\":42}", entity.DataJson);
        Assert.Equal("{\"key\":\"value\"}", entity.MetadataJson);
        Assert.Equal(5, entity.Version);
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal(updatedAt, entity.UpdatedAt);
    }

    [Fact]
    public void InMemorySagaDbContext_Constructor_ShouldInitializeDbSet()
    {
        // Act
        var context = new InMemorySagaDbContext();

        // Assert
        Assert.NotNull(context.Sagas);
    }

    [Fact]
    public void InMemorySagaDbContext_Add_ShouldAddEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var entity = new SagaEntityBase { SagaId = Guid.NewGuid() };

        // Act
        context.Add(entity);

        // Assert
        Assert.Contains(entity, context.Sagas);
    }

    [Fact]
    public void InMemorySagaDbContext_Update_ShouldUpdateEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var entity = new SagaEntityBase { SagaId = Guid.NewGuid(), State = SagaState.Running };
        context.Add(entity);

        // Act
        entity.State = SagaState.Completed;
        context.Update(entity);

        // Assert
        var found = context.Sagas.FirstOrDefault(s => s.SagaId == entity.SagaId);
        Assert.NotNull(found);
        Assert.Equal(SagaState.Completed, found!.State);
    }

    [Fact]
    public void InMemorySagaDbContext_Remove_ShouldRemoveEntity()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var entity = new SagaEntityBase { SagaId = Guid.NewGuid() };
        context.Add(entity);

        // Act
        context.Remove(entity);

        // Assert
        Assert.DoesNotContain(entity, context.Sagas);
    }

    [Fact]
    public async Task InMemorySagaDbContext_SaveChangesAsync_ShouldReturnCount()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var entity = new SagaEntityBase { SagaId = Guid.NewGuid() };
        context.Add(entity);

        // Act
        var result = await context.SaveChangesAsync();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void InMemorySagaDbContext_MultipleOperations_ShouldWork()
    {
        // Arrange
        var context = new InMemorySagaDbContext();
        var entity1 = new SagaEntityBase { SagaId = Guid.NewGuid() };
        var entity2 = new SagaEntityBase { SagaId = Guid.NewGuid() };

        // Act
        context.Add(entity1);
        context.Add(entity2);
        context.Remove(entity1);

        // Assert
        Assert.Single(context.Sagas);
        Assert.Contains(entity2, context.Sagas);
        Assert.DoesNotContain(entity1, context.Sagas);
    }
}
