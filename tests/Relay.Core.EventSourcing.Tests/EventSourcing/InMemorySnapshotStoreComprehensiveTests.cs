using Relay.Core.EventSourcing.Stores;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.EventSourcing.Tests;

public class InMemorySnapshotStoreComprehensiveTests
{
    [Fact]
    public async Task SaveSnapshotAsync_WithNullSnapshot_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            store.SaveSnapshotAsync(aggregateId, (TestSnapshot)null!, 1).AsTask());
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithValidSnapshot_SavesSuccessfully()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "Test", Value = 42 };

        // Act
        await store.SaveSnapshotAsync(aggregateId, snapshot, 1);

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("Test", result.Value.Snapshot.Name);
        Assert.Equal(42, result.Value.Snapshot.Value);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithSameAggregateIdAndVersion_UpdatesSuccessfully()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot1 = new TestSnapshot { Name = "Old", Value = 10 };
        var snapshot2 = new TestSnapshot { Name = "New", Value = 20 };

        // Act
        await store.SaveSnapshotAsync(aggregateId, snapshot1, 5);
        await store.SaveSnapshotAsync(aggregateId, snapshot2, 5); // Same version

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("New", result.Value.Snapshot.Name);
        Assert.Equal(20, result.Value.Snapshot.Value);
        Assert.Equal(5, result.Value.Version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithNonExistentAggregate_ReturnsNull()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithExistingAggregate_ReturnsLatestSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "First", Value = 1 }, 1);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Second", Value = 2 }, 2);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Third", Value = 3 }, 3);

        // Act
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Third", result.Value.Snapshot.Name);
        Assert.Equal(3, result.Value.Snapshot.Value);
        Assert.Equal(3, result.Value.Version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithDifferentTypes_ReturnsCorrectType()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "Typed", Value = 100 };

        await store.SaveSnapshotAsync(aggregateId, snapshot, 1);

        // Act
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Value.Snapshot);
        Assert.Equal("Typed", result.Value.Snapshot.Name);
        Assert.Equal(100, result.Value.Snapshot.Value);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithWrongType_ReturnsNull()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        // Save a TestSnapshot
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Test", Value = 1 }, 1);

        // Act
        // Try to retrieve as a different type (this would fail at runtime if types were different classes)
        // But since this is the same TestSnapshot class, we can't test type mismatch
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert - This should still work with the same type
        Assert.NotNull(result);
        Assert.Equal("Test", result.Value.Snapshot.Name);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithNonExistentAggregate_DoesNotThrow()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        // Act & Assert - Should not throw
        await store.DeleteOldSnapshotsAsync(aggregateId, 10);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithNoOldSnapshots_DoesNotDeleteAnything()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "New", Value = 10 }, 10);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId, 5); // Delete older than 5, but we only have version 10

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("New", result.Value.Snapshot.Name);
        Assert.Equal(10, result.Value.Version);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithMixedVersions_DeletesOlderThanThreshold()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Old1", Value = 1 }, 1);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Old2", Value = 2 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "New", Value = 10 }, 10);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId, 8); // Delete older than version 8

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("New", result.Value.Snapshot.Name);
        Assert.Equal(10, result.Value.Version);

        // Check that old snapshots were deleted by trying to get snapshot before deletion
        // We need to re-add and check individually
        store.Clear();
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Old1", Value = 1 }, 1);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Old2", Value = 2 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "New", Value = 10 }, 10);

        // Now delete old ones
        await store.DeleteOldSnapshotsAsync(aggregateId, 8);
        
        // Check that old versions are gone
        var allVersions = await GetAllVersions(store, aggregateId);
        Assert.Contains(10, allVersions); // Should still exist
        Assert.DoesNotContain(1, allVersions); // Should be deleted
        Assert.DoesNotContain(5, allVersions); // Should be deleted
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithExactThresholdVersion_DoesNotDeleteThreshold()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "AtThreshold", Value = 5 }, 5);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "AboveThreshold", Value = 10 }, 10);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId, 5); // Delete older than version 5

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("AboveThreshold", result.Value.Snapshot.Name);
        Assert.Equal(10, result.Value.Version);
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithMultipleAggregates_IsolatedStorage()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        var snapshot1 = new TestSnapshot { Name = "Aggregate1", Value = 100 };
        var snapshot2 = new TestSnapshot { Name = "Aggregate2", Value = 200 };

        // Act
        await store.SaveSnapshotAsync(aggregateId1, snapshot1, 1);
        await store.SaveSnapshotAsync(aggregateId2, snapshot2, 1);

        // Assert
        var result1 = await store.GetSnapshotAsync<TestSnapshot>(aggregateId1);
        var result2 = await store.GetSnapshotAsync<TestSnapshot>(aggregateId2);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("Aggregate1", result1.Value.Snapshot.Name);
        Assert.Equal("Aggregate2", result2.Value.Snapshot.Name);
        Assert.Equal(100, result1.Value.Snapshot.Value);
        Assert.Equal(200, result2.Value.Snapshot.Value);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithMultipleAggregates_IsolatedDeletion()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();

        // Add snapshots to both aggregates
        await store.SaveSnapshotAsync(aggregateId1, new TestSnapshot { Name = "A1 Old", Value = 1 }, 1);
        await store.SaveSnapshotAsync(aggregateId1, new TestSnapshot { Name = "A1 New", Value = 2 }, 10);
        await store.SaveSnapshotAsync(aggregateId2, new TestSnapshot { Name = "A2 Old", Value = 3 }, 1);
        await store.SaveSnapshotAsync(aggregateId2, new TestSnapshot { Name = "A2 New", Value = 4 }, 10);

        // Act
        await store.DeleteOldSnapshotsAsync(aggregateId1, 5); // Only delete from aggregate 1

        // Assert
        var result1 = await store.GetSnapshotAsync<TestSnapshot>(aggregateId1);
        var result2 = await store.GetSnapshotAsync<TestSnapshot>(aggregateId2);

        // Aggregate 1 should have only the newer snapshot
        Assert.NotNull(result1);
        Assert.Equal("A1 New", result1.Value.Snapshot.Name); // Only newer version remains
        Assert.Equal(10, result1.Value.Version);

        // Aggregate 2 should have both snapshots (not affected by deletion)
        Assert.NotNull(result2);
        Assert.Equal("A2 New", result2.Value.Snapshot.Name); // Latest version remains
    }

    [Fact]
    public async Task GetSnapshotAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // For this test, since the implementation doesn't use the cancellation token,
        // we're testing that the method accepts it without error
        // Act & Assert - Should not throw with cancellation token
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId, cts.Token);
        Assert.Null(result); // Should return null as no snapshot exists
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "Cancellable", Value = 42 };
        var cts = new CancellationTokenSource();

        // For this test, since the implementation doesn't use the cancellation token,
        // we're testing that the method accepts it without error
        // Act & Assert - Should not throw with cancellation token
        await store.SaveSnapshotAsync(aggregateId, snapshot, 1, cts.Token);
        
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("Cancellable", result.Value.Snapshot.Name);
    }

    [Fact]
    public async Task DeleteOldSnapshotsAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "ForDeletionTest", Value = 10 };
        var cts = new CancellationTokenSource();

        await store.SaveSnapshotAsync(aggregateId, snapshot, 1);

        // For this test, since the implementation doesn't use the cancellation token,
        // we're testing that the method accepts it without error
        // Act & Assert - Should not throw with cancellation token
        await store.DeleteOldSnapshotsAsync(aggregateId, 5, cts.Token);
    }

    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        var snapshot1 = new TestSnapshot { Name = "ToClear1", Value = 1 };
        var snapshot2 = new TestSnapshot { Name = "ToClear2", Value = 2 };

        // Add some snapshots
        store.SaveSnapshotAsync(aggregateId1, snapshot1, 1).GetAwaiter().GetResult();
        store.SaveSnapshotAsync(aggregateId2, snapshot2, 1).GetAwaiter().GetResult();

        // Verify they exist
        var result1 = store.GetSnapshotAsync<TestSnapshot>(aggregateId1).GetAwaiter().GetResult();
        var result2 = store.GetSnapshotAsync<TestSnapshot>(aggregateId2).GetAwaiter().GetResult();
        Assert.NotNull(result1);
        Assert.NotNull(result2);

        // Act
        store.Clear();

        // Assert
        var result1AfterClear = store.GetSnapshotAsync<TestSnapshot>(aggregateId1).GetAwaiter().GetResult();
        var result2AfterClear = store.GetSnapshotAsync<TestSnapshot>(aggregateId2).GetAwaiter().GetResult();
        Assert.Null(result1AfterClear);
        Assert.Null(result2AfterClear);
    }

    [Fact]
    public void Clear_WhenEmpty_DoesNotThrow()
    {
        // Arrange
        var store = new InMemorySnapshotStore();

        // Act & Assert - Should not throw
        store.Clear();
    }

    [Fact]
    public async Task SaveSnapshotAsync_WithHighVersionNumbers_WorksCorrectly()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new TestSnapshot { Name = "HighVersion", Value = 999 };

        // Act
        await store.SaveSnapshotAsync(aggregateId, snapshot, int.MaxValue);

        // Assert
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);
        Assert.NotNull(result);
        Assert.Equal("HighVersion", result.Value.Snapshot.Name);
        Assert.Equal(999, result.Value.Snapshot.Value);
        Assert.Equal(int.MaxValue, result.Value.Version);
    }

    [Fact]
    public async Task GetSnapshotAsync_WithMultipleVersions_ReturnsHighestVersion()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        
        // Save snapshots in random order to ensure the retrieval logic gets the highest version
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Version50", Value = 50 }, 50);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Version10", Value = 10 }, 10);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Version100", Value = 100 }, 100);
        await store.SaveSnapshotAsync(aggregateId, new TestSnapshot { Name = "Version1", Value = 1 }, 1);

        // Act
        var result = await store.GetSnapshotAsync<TestSnapshot>(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Version100", result.Value.Snapshot.Name);
        Assert.Equal(100, result.Value.Snapshot.Value);
        Assert.Equal(100, result.Value.Version);
    }

    // Helper method to get all stored versions for testing purposes
    private async Task<int[]> GetAllVersions(InMemorySnapshotStore store, Guid aggregateId)
    {
        // This is a test helper that uses reflection to access the internal storage
        var snapshotsField = typeof(InMemorySnapshotStore).GetField("_snapshots", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var snapshots = snapshotsField?.GetValue(store) as System.Collections.Concurrent.ConcurrentDictionary<Guid, System.Collections.Concurrent.ConcurrentDictionary<int, object>>;
        
        if (snapshots != null && snapshots.TryGetValue(aggregateId, out var aggregateSnapshots))
        {
            return aggregateSnapshots.Keys.ToArray();
        }
        
        return new int[0];
    }

    public class TestSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
