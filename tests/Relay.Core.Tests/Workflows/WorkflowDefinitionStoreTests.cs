using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Relay.Core.Workflows;
using Relay.Core.Workflows.Infrastructure;
using Xunit;

namespace Relay.Core.Tests.Workflows;

public class WorkflowDefinitionStoreTests
{
    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldSaveAndRetrieveDefinition()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        // Act
        await store.SaveDefinitionAsync(definition);
        var retrieved = await store.GetDefinitionAsync(definition.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(definition.Id);
        retrieved.Name.Should().Be(definition.Name);
        retrieved.Steps.Should().HaveCount(definition.Steps.Count);
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldAutoIncrementVersion()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        // Act - Save version 1
        await store.SaveDefinitionAsync(definition);
        var entities1 = await context.WorkflowDefinitions.Where(d => d.Id == definition.Id).ToListAsync();

        // Act - Save version 2
        definition.Description = "Updated description";
        await store.SaveDefinitionAsync(definition);
        var entities2 = await context.WorkflowDefinitions.Where(d => d.Id == definition.Id).ToListAsync();

        // Assert
        entities1.Should().HaveCount(1);
        entities1[0].Version.Should().Be(1);

        entities2.Should().HaveCount(2); // Both versions exist
        entities2.Where(e => e.Version == 2).Should().HaveCount(1);
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldDeactivatePreviousVersions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        // Act - Save v1 and v2
        await store.SaveDefinitionAsync(definition);
        await store.SaveDefinitionAsync(definition);

        var allVersions = await context.WorkflowDefinitions
            .Where(d => d.Id == definition.Id)
            .ToListAsync();

        // Assert
        allVersions.Should().HaveCount(2);
        allVersions.Count(d => d.IsActive).Should().Be(1); // Only one active
        allVersions.First(d => d.Version == 2).IsActive.Should().BeTrue();
        allVersions.First(d => d.Version == 1).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldGetOnlyActiveVersion()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        // Act - Save multiple versions
        await store.SaveDefinitionAsync(definition);
        definition.Name = "Updated Name v2";
        await store.SaveDefinitionAsync(definition);
        definition.Name = "Updated Name v3";
        await store.SaveDefinitionAsync(definition);

        var retrieved = await store.GetDefinitionAsync(definition.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be("Updated Name v3"); // Latest version
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldGetAllActiveDefinitions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);

        var def1 = CreateTestDefinition("workflow-1", "Workflow 1");
        var def2 = CreateTestDefinition("workflow-2", "Workflow 2");
        var def3 = CreateTestDefinition("workflow-3", "Workflow 3");

        // Act
        await store.SaveDefinitionAsync(def1);
        await store.SaveDefinitionAsync(def2);
        await store.SaveDefinitionAsync(def3);

        // Save another version of def1 to test filtering
        def1.Name = "Updated Workflow 1";
        await store.SaveDefinitionAsync(def1);

        var allDefinitions = (await store.GetAllDefinitionsAsync()).ToList();

        // Assert
        allDefinitions.Should().HaveCount(3); // Three unique workflows
        allDefinitions.Should().Contain(d => d.Id == "workflow-1");
        allDefinitions.Should().Contain(d => d.Id == "workflow-2");
        allDefinitions.Should().Contain(d => d.Id == "workflow-3");
        allDefinitions.First(d => d.Id == "workflow-1").Name.Should().Be("Updated Workflow 1");
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldDeleteAllVersions()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        await store.SaveDefinitionAsync(definition);
        await store.SaveDefinitionAsync(definition); // Create v2

        // Act
        var deleted = await store.DeleteDefinitionAsync(definition.Id);

        // Assert
        deleted.Should().BeTrue();
        var remaining = await context.WorkflowDefinitions
            .Where(d => d.Id == definition.Id)
            .ToListAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldReturnFalseWhenDeletingNonExistent()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);

        // Act
        var deleted = await store.DeleteDefinitionAsync("non-existent");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task EfCoreWorkflowDefinitionStore_ShouldPersistComplexSteps()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new WorkflowDbContext(options);
        var store = new EfCoreWorkflowDefinitionStore(context);
        var definition = CreateTestDefinition();

        // Add complex step
        definition.Steps.Add(new WorkflowStep
        {
            Name = "ConditionalStep",
            Type = StepType.Conditional,
            Condition = "status == 'approved'",
            ElseSteps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "RejectionHandler", Type = StepType.Request, RequestType = "HandleRejection" }
            }
        });

        // Act
        await store.SaveDefinitionAsync(definition);
        var retrieved = await store.GetDefinitionAsync(definition.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Steps.Should().HaveCount(3);
        var conditionalStep = retrieved.Steps.First(s => s.Name == "ConditionalStep");
        conditionalStep.Condition.Should().Be("status == 'approved'");
        conditionalStep.ElseSteps.Should().HaveCount(1);
    }

    private static WorkflowDefinition CreateTestDefinition(string? id = null, string? name = null)
    {
        return new WorkflowDefinition
        {
            Id = id ?? Guid.NewGuid().ToString(),
            Name = name ?? "Test Workflow",
            Description = "A test workflow definition",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Request, RequestType = "TestRequest" },
                new WorkflowStep { Name = "Step2", Type = StepType.Wait, WaitTimeMs = 1000 }
            }
        };
    }
}
