using Microsoft.EntityFrameworkCore;
using Relay.Core.EventSourcing.Infrastructure;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.EventSourcing.Tests;

public class EfCoreSnapshotStoreEdgeCasesTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly EfCoreSnapshotStore _snapshotStore;

    public EfCoreSnapshotStoreEdgeCasesTests()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EventStoreDbContext(options);
        _snapshotStore = new EfCoreSnapshotStore(_context);
    }

    [Fact]
    public async Task Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EfCoreSnapshotStore(null!));
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithNullSnapshot_ThrowsArgumentNullException()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        TestSnapshot snapshot = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _snapshotStore.SaveSnapshotAsync(aggregateId, snapshot, 1));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithCorruptedJsonData_ThrowsJsonException()
    {
        // Arrange - Manually insert corrupted snapshot data
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "{ invalid json - corrupted",
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithEmptyJsonData_ThrowsJsonException()
    {
        // Arrange - Manually insert empty snapshot data
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = string.Empty,
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));
    }



    [Fact]
    public async Task GetSnapshotAsync_WithJsonArrayInsteadOfObject_ThrowsJsonException()
    {
        // Arrange - Manually insert array instead of object
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "[\"this\", \"is\", \"an\", \"array\"]",
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithJsonDataMissingRequiredProperties_UsesDefaultValues()
    {
        // Arrange - Manually insert JSON missing required properties
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "{\"name\": \"Test\"}", // Missing Value property
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert - System.Text.Json doesn't throw for missing properties, uses defaults
        Assert.NotNull(result);
        var snapshot = result.Value.Snapshot;
        Assert.Equal("Test", snapshot.Name);
        Assert.Equal(0, snapshot.Value); // Default value for int
    }

    [Fact]
    public async Task GetSnapshotAsync_WithJsonDataWrongPropertyTypes_ThrowsJsonException()
    {
        // Arrange - Manually insert JSON with wrong property types
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "{\"name\": 123, \"value\": \"not-a-number\"}",
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));
    }

    [Fact]
    public async Task GetSnapshotAsync_WithInvalidJsonData_ThrowsJsonException()
    {
        // Arrange - Manually insert invalid JSON data
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "\"this is just a string\"", // Invalid JSON for TestSnapshot
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var complexSnapshot = new ComplexTestSnapshot
        {
            Name = "Complex Snapshot",
            NestedData = new NestedData
            {
                Items = new[] { "item1", "item2", "item3" },
                Count = 42
            },
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2"
            }
        };

        // Act
        await _snapshotStore.SaveSnapshotAsync(aggregateId, complexSnapshot, 5);
        var result = await _snapshotStore.GetSnapshotAsync<ComplexTestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.Value.Version);
        var retrievedSnapshot = result.Value.Snapshot;
        Assert.Equal("Complex Snapshot", retrievedSnapshot.Name);
        Assert.Equal(3, retrievedSnapshot.NestedData.Items.Length);
        Assert.Equal(42, retrievedSnapshot.NestedData.Count);
        Assert.Equal(2, retrievedSnapshot.Metadata.Count);
        Assert.Equal("value1", retrievedSnapshot.Metadata["key1"]);
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithSnapshotContainingNullValues_SerializesCorrectly()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var snapshotWithNulls = new TestSnapshotWithNulls
        {
            Name = "Snapshot with nulls",
            OptionalValue = null,
            RequiredValue = 42
        };

        // Act
        await _snapshotStore.SaveSnapshotAsync(aggregateId, snapshotWithNulls, 1);
        var result = await _snapshotStore.GetSnapshotAsync<TestSnapshotWithNulls>(aggregateId);

        // Assert
        Assert.NotNull(result);
        var retrievedSnapshot = result.Value.Snapshot;
        Assert.Equal("Snapshot with nulls", retrievedSnapshot.Name);
        Assert.Null(retrievedSnapshot.OptionalValue);
        Assert.Equal(42, retrievedSnapshot.RequiredValue);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithNoSnapshotsToDelete_DoesNotThrow()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act - No exception should be thrown
        await _snapshotStore.DeleteOldSnapshotsAsync(aggregateId, 10);

        // Assert - Verify no snapshots exist
        var snapshots = await _context.Snapshots.Where(s => s.AggregateId == aggregateId).ToListAsync();
        Assert.Empty(snapshots);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithOlderThanVersionEqualToLatest_KeepsLatest()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        await _snapshotStore.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Snapshot", Value = 1 }, 5);

        // Act - Delete snapshots older than version 5 (should keep version 5)
        await _snapshotStore.DeleteOldSnapshotsAsync(aggregateId, 5);

        // Assert
        var remainingSnapshots = await _context.Snapshots.Where(s => s.AggregateId == aggregateId).ToListAsync();
        Assert.Single(remainingSnapshots);
        Assert.Equal(5, remainingSnapshots.First().Version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithNoSnapshotExists_ReturnsNull()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithJsonNullData_ThrowsInvalidOperationException()
    {
        // Arrange - Manually insert snapshot with "null" JSON data
        var aggregateId = Guid.NewGuid();
        var snapshotEntity = new SnapshotEntity
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            Version = 1,
            AggregateType = typeof(TestSnapshot).AssemblyQualifiedName!,
            SnapshotData = "null", // JSON null value
            Timestamp = DateTime.UtcNow
        };

        _context.Snapshots.Add(snapshotEntity);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _snapshotStore.GetSnapshotAsync<TestSnapshot>(aggregateId));

        Assert.Contains($"Failed to deserialize snapshot for aggregate {aggregateId}", exception.Message);
        Assert.Contains("at version 1", exception.Message);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Test snapshot classes for edge case testing
public class TestSnapshot
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class DifferentTestSnapshot
{
    public string Description { get; set; } = string.Empty;
}

public class ComplexTestSnapshot
{
    public string Name { get; set; } = string.Empty;
    public NestedData NestedData { get; set; } = new();
    public System.Collections.Generic.Dictionary<string, string> Metadata { get; set; } = new();
}

public class NestedData
{
    public string[] Items { get; set; } = System.Array.Empty<string>();
    public int Count { get; set; }
}

public class TestSnapshotWithNulls
{
    public string Name { get; set; } = string.Empty;
    public int? OptionalValue { get; set; }
    public int RequiredValue { get; set; }
}
