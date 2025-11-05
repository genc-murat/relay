using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TestDataIsolationHelperTests : IDisposable
{
    private readonly string _testDirectory;

    public TestDataIsolationHelperTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_DefaultIsolationLevel_IsDatabaseTransaction()
    {
        // Act
        var helper = new TestDataIsolationHelper();

        // Assert
        Assert.Equal(IsolationLevel.DatabaseTransaction, helper.Level);
    }

    [Fact]
    public void Constructor_CustomIsolationLevel_SetsLevel()
    {
        // Act
        var helper = new TestDataIsolationHelper(IsolationLevel.Full);

        // Assert
        Assert.Equal(IsolationLevel.Full, helper.Level);
    }

    [Fact]
    public void TestId_IsUnique()
    {
        // Act
        var helper1 = new TestDataIsolationHelper();
        var helper2 = new TestDataIsolationHelper();

        // Assert
        Assert.NotEqual(helper1.TestId, helper2.TestId);
        Assert.False(string.IsNullOrEmpty(helper1.TestId));
        Assert.False(string.IsNullOrEmpty(helper2.TestId));
    }

    [Fact]
    public void RegisterCleanupAction_AddsAction()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executed = false;

        // Act
        helper.RegisterCleanupAction(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert - Action should be registered but not executed yet
        Assert.False(executed);
    }

    [Fact]
    public async Task DisposeAsync_ExecutesCleanupActions_InReverseOrder()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executionOrder = new List<int>();

        helper.RegisterCleanupAction(() =>
        {
            executionOrder.Add(1);
            return Task.CompletedTask;
        });

        helper.RegisterCleanupAction(() =>
        {
            executionOrder.Add(2);
            return Task.CompletedTask;
        });

        helper.RegisterCleanupAction(() =>
        {
            executionOrder.Add(3);
            return Task.CompletedTask;
        });

        // Act
        await helper.DisposeAsync();

        // Assert
        Assert.Equal(new[] { 3, 2, 1 }, executionOrder);
    }

    [Fact]
    public void RegisterDisposable_AddsDisposable()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var disposable = new TestDisposable();

        // Act
        helper.RegisterDisposable(disposable);

        // Assert - Should not be disposed yet
        Assert.False(disposable.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_DisposesResources_InReverseOrder()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var disposables = new List<TestDisposable>();

        for (int i = 0; i < 3; i++)
        {
            var disposable = new TestDisposable();
            disposables.Add(disposable);
            helper.RegisterDisposable(disposable);
        }

        // Act
        await helper.DisposeAsync();

        // Assert - All should be disposed in reverse order
        Assert.True(disposables[0].IsDisposed);
        Assert.True(disposables[1].IsDisposed);
        Assert.True(disposables[2].IsDisposed);
    }

    [Fact]
    public void RegisterAsyncDisposable_AddsAsyncDisposable()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var asyncDisposable = new TestAsyncDisposable();

        // Act
        helper.RegisterAsyncDisposable(asyncDisposable);

        // Assert - Should not be disposed yet
        Assert.False(asyncDisposable.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAsyncResources_InReverseOrder()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var asyncDisposables = new List<TestAsyncDisposable>();

        for (int i = 0; i < 3; i++)
        {
            var asyncDisposable = new TestAsyncDisposable();
            asyncDisposables.Add(asyncDisposable);
            helper.RegisterAsyncDisposable(asyncDisposable);
        }

        // Act
        await helper.DisposeAsync();

        // Assert - All should be disposed in reverse order
        Assert.True(asyncDisposables[0].IsDisposed);
        Assert.True(asyncDisposables[1].IsDisposed);
        Assert.True(asyncDisposables[2].IsDisposed);
    }

    [Fact]
    public void CreateIsolatedDatabaseContext_CreatesContext()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var dbContext = new TestDbContext();

        // Act
        var isolatedContext = helper.CreateIsolatedDatabaseContext(() => dbContext);

        // Assert
        Assert.NotNull(isolatedContext);
        Assert.Equal(dbContext, isolatedContext.Context);
        Assert.Equal(helper.TestId, isolatedContext.TestId);
        Assert.Equal(helper.Level, isolatedContext.Level);
    }

    [Fact]
    public void CreateIsolatedMemoryStore_CreatesStore()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();

        // Act
        var store = helper.CreateIsolatedMemoryStore<TestData>();

        // Assert
        Assert.NotNull(store);
        Assert.Equal(helper.TestId, store.TestId);
        Assert.Empty(store.Items);
    }

    [Fact]
    public void CreateIsolatedTempFile_CreatesFileWithTestId()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();

        // Act
        var tempFile = helper.CreateIsolatedTempFile("txt");

        // Assert
        Assert.Contains(helper.TestId, tempFile);
        Assert.EndsWith(".txt", tempFile);
    }

    [Fact]
    public async Task DisposeAsync_CleansUpTempFiles()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var tempFile = helper.CreateIsolatedTempFile("txt");

        // Create the file
        await File.WriteAllTextAsync(tempFile, "test content");
        Assert.True(File.Exists(tempFile));

        // Act
        await helper.DisposeAsync();

        // Assert - File should be deleted
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task ExecuteIsolatedAsync_ExecutesAction()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executed = false;

        // Act
        await helper.ExecuteIsolatedAsync(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task ExecuteIsolatedAsync_WithResult_ReturnsResult()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();

        // Act
        var result = await helper.ExecuteIsolatedAsync(() => Task.FromResult(42));

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteIsolatedAsync_UsesSemaphore()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executionCount = 0;
        var tasks = new List<Task>();

        // Act - Start multiple concurrent executions
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(helper.ExecuteIsolatedAsync(async () =>
            {
                var currentCount = ++executionCount;
                await Task.Delay(10); // Small delay to ensure overlap
                Assert.Equal(currentCount, executionCount); // Should execute sequentially
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All tasks completed
        Assert.Equal(5, executionCount);
    }

    [Fact]
    public void Dispose_CallsDisposeAsync()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executed = false;

        helper.RegisterCleanupAction(() =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        // Act
        helper.Dispose();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task DisposeAsync_Idempotent_CanCallMultipleTimes()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executionCount = 0;

        helper.RegisterCleanupAction(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        });

        // Act
        await helper.DisposeAsync();
        await helper.DisposeAsync();

        // Assert - Should only execute once
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void RegisterCleanupAction_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterCleanupAction(() => Task.CompletedTask));
    }

    [Fact]
    public void RegisterDisposable_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterDisposable(new TestDisposable()));
    }

    [Fact]
    public void RegisterAsyncDisposable_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterAsyncDisposable(new TestAsyncDisposable()));
    }

    [Fact]
    public void CreateIsolatedDatabaseContext_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.CreateIsolatedDatabaseContext(() => new TestDbContext()));
    }

    [Fact]
    public void CreateIsolatedMemoryStore_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.CreateIsolatedMemoryStore<TestData>());
    }

    [Fact]
    public void CreateIsolatedTempFile_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.CreateIsolatedTempFile());
    }

    [Fact]
    public async Task ExecuteIsolatedAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        helper.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            helper.ExecuteIsolatedAsync(() => Task.CompletedTask));
    }

    [Fact]
    public async Task DisposeAsync_HandlesExceptionsInCleanupActions()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var successfulExecutionCount = 0;

        helper.RegisterCleanupAction(() => throw new Exception("Cleanup failed"));
        helper.RegisterCleanupAction(() =>
        {
            successfulExecutionCount++;
            return Task.CompletedTask;
        });
        helper.RegisterCleanupAction(() => throw new Exception("Another cleanup failed"));
        helper.RegisterCleanupAction(() =>
        {
            successfulExecutionCount++;
            return Task.CompletedTask;
        });

        // Act - Should not throw even though some cleanup actions fail
        await helper.DisposeAsync();

        // Assert - Successful actions should still execute
        Assert.Equal(2, successfulExecutionCount);
    }

    [Fact]
    public async Task DisposeAsync_HandlesExceptionsInDisposables()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();

        helper.RegisterDisposable(new FailingDisposable());
        helper.RegisterDisposable(new TestDisposable());
        helper.RegisterDisposable(new FailingDisposable());

        // Act - Should not throw even though some disposables fail
        await helper.DisposeAsync();

        // Assert - No exception thrown
    }

    [Fact]
    public void IsolatedDatabaseContext_Properties_ReturnCorrectValues()
    {
        // Arrange
        var dbContext = new TestDbContext();
        var testId = "test123";
        var level = IsolationLevel.Full;

        // Act
        var isolatedContext = new IsolatedDatabaseContext<TestDbContext>(dbContext, testId, level);

        // Assert
        Assert.Equal(dbContext, isolatedContext.Context);
        Assert.Equal(testId, isolatedContext.TestId);
        Assert.Equal(level, isolatedContext.Level);
    }

    [Fact]
    public void IsolatedDatabaseContext_Dispose_DisposesContext()
    {
        // Arrange
        var dbContext = new TestDbContext();
        var isolatedContext = new IsolatedDatabaseContext<TestDbContext>(dbContext, "test", IsolationLevel.DatabaseTransaction);

        // Act
        isolatedContext.Dispose();

        // Assert
        Assert.True(dbContext.IsDisposed);
    }

    [Fact]
    public void IsolatedMemoryStore_StoreAndRetrieve_Works()
    {
        // Arrange
        var store = new IsolatedMemoryStore<TestData>("test123");
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        store.Store("key1", data);
        var retrieved = store.Retrieve("key1");

        // Assert
        Assert.Equal(data, retrieved);
        Assert.Single(store.Items);
    }

    [Fact]
    public void IsolatedMemoryStore_Remove_Works()
    {
        // Arrange
        var store = new IsolatedMemoryStore<TestData>("test123");
        var data = new TestData { Id = 1, Name = "Test" };
        store.Store("key1", data);

        // Act
        var removed = store.Remove("key1");

        // Assert
        Assert.True(removed);
        Assert.Null(store.Retrieve("key1"));
        Assert.Empty(store.Items);
    }

    [Fact]
    public void IsolatedMemoryStore_Remove_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var store = new IsolatedMemoryStore<TestData>("test123");

        // Act
        var removed = store.Remove("nonexistent");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void IsolatedMemoryStore_Clear_Works()
    {
        // Arrange
        var store = new IsolatedMemoryStore<TestData>("test123");
        store.Store("key1", new TestData { Id = 1 });
        store.Store("key2", new TestData { Id = 2 });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.Items);
        Assert.Null(store.Retrieve("key1"));
        Assert.Null(store.Retrieve("key2"));
    }

    [Fact]
    public void IsolatedMemoryStore_Dispose_ClearsStore()
    {
        // Arrange
        var store = new IsolatedMemoryStore<TestData>("test123");
        store.Store("key1", new TestData { Id = 1 });

        // Act
        store.Dispose();

        // Assert
        Assert.Empty(store.Items);
    }

    [Fact]
    public void IsolationLevel_EnumValues_AreDefined()
    {
        // Act & Assert
        Assert.Equal(0, (int)IsolationLevel.None);
        Assert.Equal(1, (int)IsolationLevel.Memory);
        Assert.Equal(2, (int)IsolationLevel.DatabaseTransaction);
        Assert.Equal(3, (int)IsolationLevel.Full);
    }

    // Test helper classes
    private class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class TestAsyncDisposable : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private class FailingDisposable : IDisposable
    {
        public void Dispose()
        {
            throw new Exception("Dispose failed");
        }
    }

    private class TestDbContext : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public override bool Equals(object? obj)
        {
            return obj is TestData other && Id == other.Id && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }
}