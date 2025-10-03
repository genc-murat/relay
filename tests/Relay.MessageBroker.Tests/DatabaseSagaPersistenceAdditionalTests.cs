using FluentAssertions;
using Relay.MessageBroker.Saga;
using Relay.MessageBroker.Saga.Persistence;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DatabaseSagaPersistenceAdditionalTests
{
    [Fact]
    public void DatabaseSagaPersistence_Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act
        Action act = () => new DatabaseSagaPersistence<TestSagaData>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dbContext");
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
        context.Sagas.Should().HaveCount(1);
        var entity = context.Sagas.First();
        entity.SagaId.Should().Be(sagaData.SagaId);
        entity.State.Should().Be(SagaState.Running);
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
        context.Sagas.Should().HaveCount(1);
        var entity = context.Sagas.First();
        entity.State.Should().Be(SagaState.Completed);
        entity.Version.Should().Be(2);
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
        result.Should().NotBeNull();
        result!.SagaId.Should().Be(originalData.SagaId);
        result.Value.Should().Be(42);
        result.State.Should().Be(SagaState.Running);
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
        result.Should().BeNull();
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
        result.Should().NotBeNull();
        result!.CorrelationId.Should().Be(correlationId);
        result.Value.Should().Be(42);
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
        context.Sagas.Should().BeEmpty();
    }

    [Fact]
    public void SagaEntityBase_DefaultValues_ShouldBeSet()
    {
        // Act
        var entity = new SagaEntityBase();

        // Assert - Just check that the entity can be created with default values
        entity.Should().NotBeNull();
        entity.CorrelationId.Should().NotBeNull();
        entity.State.Should().Be(SagaState.NotStarted);
        entity.CurrentStep.Should().Be(0);
        entity.SagaType.Should().NotBeNull();
        entity.DataJson.Should().NotBeNull();
        entity.Version.Should().Be(0);
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
        entity.SagaId.Should().Be(sagaId);
        entity.CorrelationId.Should().Be(correlationId);
        entity.State.Should().Be(SagaState.Completed);
        entity.CurrentStep.Should().Be(3);
        entity.SagaType.Should().Be("TestSaga");
        entity.DataJson.Should().Be("{\"value\":42}");
        entity.MetadataJson.Should().Be("{\"key\":\"value\"}");
        entity.Version.Should().Be(5);
        entity.CreatedAt.Should().Be(createdAt);
        entity.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void InMemorySagaDbContext_Constructor_ShouldInitializeDbSet()
    {
        // Act
        var context = new InMemorySagaDbContext();

        // Assert
        context.Sagas.Should().NotBeNull();
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
        context.Sagas.Should().Contain(entity);
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
        found.Should().NotBeNull();
        found!.State.Should().Be(SagaState.Completed);
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
        context.Sagas.Should().NotContain(entity);
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
        result.Should().BeGreaterThanOrEqualTo(0);
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
        context.Sagas.Should().HaveCount(1);
        context.Sagas.Should().Contain(entity2);
        context.Sagas.Should().NotContain(entity1);
    }
}
