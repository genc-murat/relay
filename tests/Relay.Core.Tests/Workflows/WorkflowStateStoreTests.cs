using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Relay.Core.Workflows;
using Relay.Core.Workflows.Infrastructure;
using Relay.Core.Workflows.Stores;
using Xunit;

namespace Relay.Core.Tests.Workflows;

public class WorkflowStateStoreTests
{
    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldSaveAndRetrieveExecution()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();
        var execution = CreateTestExecution();

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(execution.Id, retrieved!.Id);
        Assert.Equal(execution.WorkflowDefinitionId, retrieved.WorkflowDefinitionId);
        Assert.Equal(execution.Status, retrieved.Status);
        Assert.Equal(execution.CurrentStepIndex, retrieved.CurrentStepIndex);
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldUpdateExecution()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();
        var execution = CreateTestExecution();
        await store.SaveExecutionAsync(execution);

        // Act - Update
        execution.Status = WorkflowStatus.Completed;
        execution.CompletedAt = DateTime.UtcNow;
        execution.CurrentStepIndex = 5;
        await store.SaveExecutionAsync(execution);

        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(WorkflowStatus.Completed, retrieved!.Status);
        Assert.NotNull(retrieved.CompletedAt);
        Assert.Equal(5, retrieved.CurrentStepIndex);
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldReturnNullForNonExistentExecution()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();

        // Act
        var retrieved = await store.GetExecutionAsync("non-existent-id");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldStoreContext()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();
        var execution = CreateTestExecution();
        execution.Context["key1"] = "value1";
        execution.Context["key2"] = 123;

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved!.Context.ContainsKey("key1"));
        Assert.True(retrieved.Context.ContainsKey("key2"));
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldStoreStepExecutions()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();
        var execution = CreateTestExecution();
        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step1",
            Status = StepStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        });

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Single(retrieved!.StepExecutions);
        Assert.Equal("Step1", retrieved.StepExecutions[0].StepName);
        Assert.Equal(StepStatus.Completed, retrieved.StepExecutions[0].Status);
    }

    [Fact]
    public async Task EfCoreWorkflowStateStore_ShouldSaveAndRetrieveExecution()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);
        var execution = CreateTestExecution();

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(execution.Id, retrieved!.Id);
        Assert.Equal(execution.WorkflowDefinitionId, retrieved.WorkflowDefinitionId);
        Assert.Equal(execution.Status, retrieved.Status);
    }

    [Fact]
    public async Task EfCoreWorkflowStateStore_ShouldIncrementVersionOnUpdate()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);
        var execution = CreateTestExecution();

        // Act - First save
        await store.SaveExecutionAsync(execution);
        var entity1 = await context.WorkflowExecutions.FirstAsync(e => e.Id == execution.Id);
        var version1 = entity1.Version;

        // Act - Second save
        execution.CurrentStepIndex = 1;
        await store.SaveExecutionAsync(execution);
        var entity2 = await context.WorkflowExecutions.FirstAsync(e => e.Id == execution.Id);
        var version2 = entity2.Version;

        // Assert
        Assert.True(version2 > version1);
    }

    [Fact]
    public async Task EfCoreWorkflowStateStore_ShouldSerializeComplexContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);
        var execution = CreateTestExecution();
        execution.Context["data"] = new { Name = "Test", Value = 42 };

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.True(retrieved!.Context.ContainsKey("data"));
    }

    [Fact]
    public async Task EfCoreWorkflowStateStore_ShouldPersistStepExecutions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);
        var execution = CreateTestExecution();

        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "InitialStep",
            Status = StepStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-2),
            CompletedAt = DateTime.UtcNow.AddMinutes(-1)
        });

        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "SecondStep",
            Status = StepStatus.Running,
            StartedAt = DateTime.UtcNow
        });

        // Act
        await store.SaveExecutionAsync(execution);
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.StepExecutions.Count);
        Assert.Equal("InitialStep", retrieved.StepExecutions[0].StepName);
        Assert.Equal("SecondStep", retrieved.StepExecutions[1].StepName);
        Assert.Equal(StepStatus.Running, retrieved.StepExecutions[1].Status);
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldCloneExecutionToPreventMutation()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();
        var execution = CreateTestExecution();
        await store.SaveExecutionAsync(execution);

        // Act - Modify original
        execution.Status = WorkflowStatus.Failed;
        execution.Error = "Modified externally";

        // Retrieve and verify it wasn't affected
        var retrieved = await store.GetExecutionAsync(execution.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(WorkflowStatus.Running, retrieved!.Status); // Original status
        Assert.Null(retrieved.Error); // No error
    }

    [Fact]
    public void EfCoreWorkflowStateStore_Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EfCoreWorkflowStateStore(null!));
    }

    [Fact]
    public async Task EfCoreWorkflowStateStore_SaveExecutionAsync_ShouldThrowArgumentNullException_WhenExecutionIsNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SaveExecutionAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EfCoreWorkflowStateStore_SaveExecutionAsync_ShouldThrowArgumentException_WhenExecutionIdIsEmpty(string executionId)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);
        var execution = CreateTestExecution();
        execution.Id = executionId!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => await store.SaveExecutionAsync(execution));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EfCoreWorkflowStateStore_GetExecutionAsync_ShouldReturnNull_WhenExecutionIdIsEmpty(string executionId)
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);

        // Act
        var result = await store.GetExecutionAsync(executionId!);

        // Assert
        Assert.Null(result);
    }

    // Note: Concurrency test skipped for in-memory database as it doesn't support EF Core concurrency control
    // In a real database, this would test DbUpdateConcurrencyException handling

    [Fact]
    public async Task EfCoreWorkflowStateStore_GetExecutionAsync_ShouldHandleCorruptedJsonDataGracefully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowStateStore(context);

        // Create entity with corrupted JSON data
        var entity = new WorkflowExecutionEntity
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowDefinitionId = "test-workflow",
            Status = "Running",
            StartedAt = DateTime.UtcNow,
            CurrentStepIndex = 0,
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
            // Corrupted JSON data
            InputData = "{invalid json",
            OutputData = "also invalid",
            ContextData = "not a dictionary",
            StepExecutionsData = "not a list"
        };

        context.WorkflowExecutions.Add(entity);
        await context.SaveChangesAsync();

        // Act
        var result = await store.GetExecutionAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result!.Id);
        Assert.Equal("test-workflow", result.WorkflowDefinitionId);
        Assert.Equal(WorkflowStatus.Running, result.Status);
        Assert.Equal(0, result.CurrentStepIndex);

        // Corrupted data should be null/default
        Assert.Null(result.Input);
        Assert.Null(result.Output);
        Assert.Empty(result.Context);
        Assert.Empty(result.StepExecutions);
    }

    private static WorkflowExecution CreateTestExecution()
    {
        return new WorkflowExecution
        {
            Id = Guid.NewGuid().ToString(),
            WorkflowDefinitionId = "test-workflow",
            Status = WorkflowStatus.Running,
            StartedAt = DateTime.UtcNow,
            CurrentStepIndex = 0,
            Context = new Dictionary<string, object>(),
            StepExecutions = new List<WorkflowStepExecution>()
        };
    }
}
