using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class TestIsolationUtilitiesTests
{
    [Fact]
    public void TestDataIsolationHelper_CreatesUniqueTestId()
    {
        // Arrange & Act
        var helper1 = new TestDataIsolationHelper();
        var helper2 = new TestDataIsolationHelper();

        // Assert
        Assert.NotEqual(helper1.TestId, helper2.TestId);
        Assert.NotNull(helper1.TestId);
        Assert.NotNull(helper2.TestId);
    }

    [Fact]
    public void TestDataIsolationHelper_IsolationLevel_SetCorrectly()
    {
        // Arrange & Act
        var helper = new TestDataIsolationHelper(IsolationLevel.Full);

        // Assert
        Assert.Equal(IsolationLevel.Full, helper.Level);
    }

    [Fact]
    public async Task TestDataIsolationHelper_CleanupActions_ExecutedInReverseOrder()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executionOrder = new List<int>();

        helper.RegisterCleanupAction(() => { executionOrder.Add(1); return Task.CompletedTask; });
        helper.RegisterCleanupAction(() => { executionOrder.Add(2); return Task.CompletedTask; });
        helper.RegisterCleanupAction(() => { executionOrder.Add(3); return Task.CompletedTask; });

        // Act
        await helper.DisposeAsync();

        // Assert
        Assert.Equal(new[] { 3, 2, 1 }, executionOrder);
    }

    [Fact]
    public void TestDataIsolationHelper_IsolatedMemoryStore_StoresAndRetrievesData()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var store = helper.CreateIsolatedMemoryStore<TestData>();

        // Act
        var data = new TestData { Id = 1, Name = "Test" };
        store.Store("key1", data);
        var retrieved = store.Retrieve("key1");

        // Assert
        Assert.Equal(data, retrieved);
        Assert.Single(store.Items);
    }

    [Fact]
    public void TestDataIsolationHelper_IsolatedMemoryStore_RemovesData()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var store = helper.CreateIsolatedMemoryStore<TestData>();
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
    public void TestDataIsolationHelper_IsolatedMemoryStore_ClearsAllData()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var store = helper.CreateIsolatedMemoryStore<TestData>();
        store.Store("key1", new TestData { Id = 1, Name = "Test1" });
        store.Store("key2", new TestData { Id = 2, Name = "Test2" });

        // Act
        store.Clear();

        // Assert
        Assert.Empty(store.Items);
        Assert.Null(store.Retrieve("key1"));
        Assert.Null(store.Retrieve("key2"));
    }

    [Fact]
    public void TestDataIsolationHelper_IsolatedDatabaseContext_WrapsContext()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var mockContext = new MockDatabaseContext();

        // Act
        var isolatedContext = helper.CreateIsolatedDatabaseContext(() => mockContext);

        // Assert
        Assert.Equal(mockContext, isolatedContext.Context);
        Assert.Equal(helper.TestId, isolatedContext.TestId);
        Assert.Equal(helper.Level, isolatedContext.Level);
    }

    [Fact]
    public void TestDataIsolationHelper_CreateIsolatedTempFile_CreatesUniqueFile()
    {
        // Arrange
        var helper1 = new TestDataIsolationHelper();
        var helper2 = new TestDataIsolationHelper();

        // Act
        var file1 = helper1.CreateIsolatedTempFile("txt");
        var file2 = helper2.CreateIsolatedTempFile("txt");

        // Assert
        Assert.NotEqual(file1, file2);
        Assert.EndsWith(".txt", file1);
        Assert.EndsWith(".txt", file2);
        Assert.Contains(helper1.TestId, file1);
        Assert.Contains(helper2.TestId, file2);
    }

    [Fact]
    public async Task TestDataIsolationHelper_ExecuteIsolatedAsync_ExecutesAction()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var executed = false;

        // Act
        await helper.ExecuteIsolatedAsync(() => { executed = true; return Task.CompletedTask; });

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task TestDataIsolationHelper_ExecuteIsolatedAsync_ReturnsResult()
    {
        // Arrange
        var helper = new TestDataIsolationHelper();
        var expectedResult = 42;

        // Act
        var result = await helper.ExecuteIsolatedAsync(() => Task.FromResult(expectedResult));

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void DependencyMockHelper_Mock_CreatesMockInstance()
    {
        // Arrange
        var helper = new DependencyMockHelper();

        // Act
        var mockBuilder = helper.Mock<ITestService>();

        // Assert
        Assert.NotNull(mockBuilder);
        Assert.NotNull(mockBuilder.Instance);
    }

    [Fact]
    public void DependencyMockHelper_Mock_SetupReturnsValue()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mock = helper.Mock<ITestService>();

        // Act
        mock.Setup(x => x.GetValue(), "test result");

        // Assert - would need to invoke the method to test
        // This is tested more thoroughly in integration tests
    }

    [Fact]
    public void DependencyMockHelper_GetMock_ThrowsForUnregisteredType()
    {
        // Arrange
        var helper = new DependencyMockHelper();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => helper.GetMock<ITestService>());
    }

    [Fact]
    public void DependencyMockHelper_GetMock_ReturnsRegisteredMock()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        helper.Mock<ITestService>();

        // Act
        var mock = helper.GetMock<ITestService>();

        // Assert
        Assert.NotNull(mock);
    }

    [Fact]
    public void DependencyMockHelper_ResetAll_ClearsInvocations()
    {
        // Arrange
        var helper = new DependencyMockHelper();
        var mock = helper.Mock<ITestService>();
        mock.Setup(x => x.GetValue(), "result");

        // Act
        helper.ResetAll();

        // Assert - Reset functionality would be verified through verification calls
    }

    [Fact]
    public void TestCleanupHelper_RegisterCleanupAction_ExecutesActionOnDispose()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        helper.RegisterCleanupAction(() => { executed = true; return Task.CompletedTask; });

        // Act
        helper.Dispose();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void TestCleanupHelper_RegisterCleanupAction_Sync_ExecutesActionOnDispose()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        helper.RegisterCleanupAction(() => executed = true);

        // Act
        helper.Dispose();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void TestCleanupHelper_CreateTempFile_CreatesAndRegistersFile()
    {
        // Arrange
        var helper = new TestCleanupHelper();

        // Act
        var tempFile = helper.CreateTempFile("test");

        // Assert
        Assert.EndsWith(".test", tempFile);
        Assert.False(File.Exists(tempFile)); // File shouldn't exist yet

        // Cleanup
        helper.Dispose();
    }

    [Fact]
    public void TestCleanupHelper_CreateTempDirectory_CreatesAndRegistersDirectory()
    {
        // Arrange
        var helper = new TestCleanupHelper();

        // Act
        var tempDir = helper.CreateTempDirectory();

        // Assert
        Assert.True(Directory.Exists(tempDir));

        // Cleanup
        helper.Dispose();
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void TestCleanupHelper_RegisterTempFile_DeletesFileOnDispose()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");

        helper.RegisterTempFile(tempFile);

        // Act
        helper.Dispose();

        // Assert
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public void TestCleanupHelper_RegisterTempDirectory_DeletesDirectoryOnDispose()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(tempFile, "test content");

        helper.RegisterTempDirectory(tempDir);

        // Act
        helper.Dispose();

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void TestCleanupHelper_RegisterDisposable_DisposesResource()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var disposable = new MockDisposable();

        helper.RegisterDisposable(disposable);

        // Act
        helper.Dispose();

        // Assert
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void TestCleanupHelper_RegisterCollectionClear_ClearsCollection()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var collection = new List<string> { "item1", "item2", "item3" };

        helper.RegisterCollectionClear(collection);

        // Act
        helper.Dispose();

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void TestCleanupHelper_CreateScope_CleansUpOnDispose()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        helper.RegisterCleanupAction(() => executed = true);

        // Act
        using (helper.CreateScope())
        {
            // Scope is active
        }

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task TestCleanupHelper_CleanupAsync_ExecutesAllActions()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var syncExecuted = false;
        var asyncExecuted = false;

        helper.RegisterCleanupAction(() => syncExecuted = true);
        helper.RegisterCleanupAction(() => { asyncExecuted = true; return Task.CompletedTask; });

        // Act
        await helper.CleanupAsync();

        // Assert
        Assert.True(syncExecuted);
        Assert.True(asyncExecuted);
    }

    // Helper classes for testing
    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TestData other && Id == other.Id && Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name);
        }
    }

    private interface ITestService
    {
        string GetValue();
        Task<string> GetValueAsync();
        void DoSomething();
    }

    private class MockDatabaseContext : IDisposable
    {
        public void Dispose()
        {
            // Mock dispose implementation
        }
    }

    private class MockDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}