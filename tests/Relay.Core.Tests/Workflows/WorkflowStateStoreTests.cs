using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
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
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(execution.Id);
        retrieved.WorkflowDefinitionId.Should().Be(execution.WorkflowDefinitionId);
        retrieved.Status.Should().Be(execution.Status);
        retrieved.CurrentStepIndex.Should().Be(execution.CurrentStepIndex);
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
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(WorkflowStatus.Completed);
        retrieved.CompletedAt.Should().NotBeNull();
        retrieved.CurrentStepIndex.Should().Be(5);
    }

    [Fact]
    public async Task InMemoryWorkflowStateStore_ShouldReturnNullForNonExistentExecution()
    {
        // Arrange
        var store = new InMemoryWorkflowStateStore();

        // Act
        var retrieved = await store.GetExecutionAsync("non-existent-id");

        // Assert
        retrieved.Should().BeNull();
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
        retrieved.Should().NotBeNull();
        retrieved!.Context.Should().ContainKey("key1");
        retrieved.Context.Should().ContainKey("key2");
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
        retrieved.Should().NotBeNull();
        retrieved!.StepExecutions.Should().HaveCount(1);
        retrieved.StepExecutions[0].StepName.Should().Be("Step1");
        retrieved.StepExecutions[0].Status.Should().Be(StepStatus.Completed);
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
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(execution.Id);
        retrieved.WorkflowDefinitionId.Should().Be(execution.WorkflowDefinitionId);
        retrieved.Status.Should().Be(execution.Status);
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
        version2.Should().BeGreaterThan(version1);
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
        retrieved.Should().NotBeNull();
        retrieved!.Context.Should().ContainKey("data");
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
        retrieved.Should().NotBeNull();
        retrieved!.StepExecutions.Should().HaveCount(2);
        retrieved.StepExecutions[0].StepName.Should().Be("InitialStep");
        retrieved.StepExecutions[1].StepName.Should().Be("SecondStep");
        retrieved.StepExecutions[1].Status.Should().Be(StepStatus.Running);
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
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be(WorkflowStatus.Running); // Original status
        retrieved.Error.Should().BeNull(); // No error
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
