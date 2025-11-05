using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TestCleanupHelperTests : IDisposable
{
    private readonly string _testDirectory;

    public TestCleanupHelperTests()
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
    public void Constructor_CreatesInstance()
    {
        // Act
        var helper = new TestCleanupHelper();

        // Assert
        Assert.NotNull(helper);
    }

    [Fact]
    public void RegisterCleanupAction_Async_AddsAction()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        // Act
        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert - Action should be registered but not executed yet
        Assert.False(executed);
    }

    [Fact]
    public void RegisterCleanupAction_Sync_AddsAction()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        // Act
        helper.RegisterCleanupAction(() => executed = true);

        // Assert - Action should be registered but not executed yet
        Assert.False(executed);
    }

    [Fact]
    public async Task CleanupAsync_ExecutesAllRegisteredActions()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var asyncExecuted = false;
        var syncExecuted = false;

        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            asyncExecuted = true;
        });

        helper.RegisterCleanupAction(() => syncExecuted = true);

        // Act
        await helper.CleanupAsync();

        // Assert
        Assert.True(asyncExecuted);
        Assert.True(syncExecuted);
    }

    [Fact]
    public void Cleanup_ExecutesAllRegisteredActions()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        helper.RegisterCleanupAction(() => executed = true);

        // Act
        helper.Cleanup();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task RegisterTempFile_AddsFileForCleanup()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var tempFile = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(tempFile, "test content");

        // Act
        helper.RegisterTempFile(tempFile);
        await helper.CleanupAsync();

        // Assert
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public async Task RegisterTempDirectory_AddsDirectoryForCleanup()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var tempDir = Path.Combine(_testDirectory, "testdir");
        Directory.CreateDirectory(tempDir);
        var tempFile = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(tempFile, "content");

        // Act
        helper.RegisterTempDirectory(tempDir);
        await helper.CleanupAsync();

        // Assert
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public void CreateTempFile_CreatesFileAndRegistersForCleanup()
    {
        // Arrange
        var helper = new TestCleanupHelper();

        // Act
        var tempFile = helper.CreateTempFile("txt");

        // Assert
        Assert.False(string.IsNullOrEmpty(tempFile));
        Assert.EndsWith(".txt", tempFile);

        // Cleanup should remove the file
        helper.Cleanup();
        Assert.False(File.Exists(tempFile));
    }

    [Fact]
    public void CreateTempDirectory_CreatesDirectoryAndRegistersForCleanup()
    {
        // Arrange
        var helper = new TestCleanupHelper();

        // Act
        var tempDir = helper.CreateTempDirectory();

        // Assert
        Assert.False(string.IsNullOrEmpty(tempDir));
        Assert.True(Directory.Exists(tempDir));

        // Cleanup should remove the directory
        helper.Cleanup();
        Assert.False(Directory.Exists(tempDir));
    }

    [Fact]
    public async Task RegisterDisposable_DisposesResource()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var disposable = new TestDisposable();

        // Act
        helper.RegisterDisposable(disposable);
        await helper.CleanupAsync();

        // Assert
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public async Task RegisterAsyncDisposable_DisposesResource()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var asyncDisposable = new TestAsyncDisposable();

        // Act
        helper.RegisterAsyncDisposable(asyncDisposable);
        await helper.CleanupAsync();

        // Assert
        Assert.True(asyncDisposable.IsDisposed);
    }

    [Fact]
    public void RegisterStaticReset_AddsResetAction()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var resetExecuted = false;

        // Act
        helper.RegisterStaticReset<TestCleanupHelper>(() => resetExecuted = true);
        helper.Cleanup();

        // Assert
        Assert.True(resetExecuted);
    }

    [Fact]
    public void RegisterCollectionClear_ClearsCollection()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var collection = new List<string> { "item1", "item2", "item3" };

        // Act
        helper.RegisterCollectionClear(collection);
        helper.Cleanup();

        // Assert
        Assert.Empty(collection);
    }

    [Fact]
    public void Dispose_ExecutesCleanup()
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
    public async Task DisposeAsync_ExecutesCleanup()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executed = false;

        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Act
        await helper.DisposeAsync();

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void Dispose_Idempotent_CanCallMultipleTimes()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executionCount = 0;

        helper.RegisterCleanupAction(() => executionCount++);

        // Act
        helper.Dispose();
        helper.Dispose();

        // Assert
        Assert.Equal(1, executionCount); // Should only execute once
    }

    [Fact]
    public async Task DisposeAsync_Idempotent_CanCallMultipleTimes()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var executionCount = 0;

        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            executionCount++;
        });

        // Act
        await helper.DisposeAsync();
        await helper.DisposeAsync();

        // Assert
        Assert.Equal(1, executionCount); // Should only execute once
    }

    [Fact]
    public void RegisterCleanupAction_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterCleanupAction(() => { }));
    }

    [Fact]
    public void RegisterTempFile_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterTempFile("test.txt"));
    }

    [Fact]
    public void RegisterTempDirectory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.RegisterTempDirectory("testdir"));
    }

    [Fact]
    public void CreateTempFile_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.CreateTempFile());
    }

    [Fact]
    public void CreateTempDirectory_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        helper.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() =>
            helper.CreateTempDirectory());
    }

    [Fact]
    public async Task CleanupAsync_HandlesExceptionsInCleanupActions()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var successfulExecutionCount = 0;

        helper.RegisterCleanupAction(() => throw new Exception("Cleanup failed"));
        helper.RegisterCleanupAction(() => successfulExecutionCount++);
        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            throw new Exception("Async cleanup failed");
        });
        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            successfulExecutionCount++;
        });

        // Act
        await helper.CleanupAsync();

        // Assert - Should continue executing other actions even if some fail
        Assert.Equal(2, successfulExecutionCount);
    }

    [Fact]
    public async Task CleanupAsync_HandlesFileDeletionFailures()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act - Should not throw even if file doesn't exist
        helper.RegisterTempFile(nonExistentFile);
        await helper.CleanupAsync();

        // Assert - No exception thrown
    }

    [Fact]
    public async Task CleanupAsync_HandlesDirectoryDeletionFailures()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act - Should not throw even if directory doesn't exist
        helper.RegisterTempDirectory(nonExistentDir);
        await helper.CleanupAsync();

        // Assert - No exception thrown
    }

    [Fact]
    public void CreateScope_ReturnsDisposableScope()
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
        // Scope disposed, cleanup should execute

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public void RegisterDatabaseContext_RegistersDisposable()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var context = new TestDisposable();

        // Act
        helper.RegisterDatabaseContext(context);
        helper.Cleanup();

        // Assert
        Assert.True(context.IsDisposed);
    }

    [Fact]
    public void RegisterHttpClient_RegistersDisposable()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var client = new TestHttpClient();

        // Act
        helper.RegisterHttpClient(client);
        helper.Cleanup();

        // Assert
        Assert.True(client.IsDisposed);
    }

    [Fact]
    public void RegisterStream_RegistersDisposable()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var stream = new TestStream();

        // Act
        helper.RegisterStream(stream);
        helper.Cleanup();

        // Assert
        Assert.True(stream.IsDisposed);
    }

    [Fact]
    public async Task IntegrationTest_ComplexCleanupScenario()
    {
        // Arrange
        var helper = new TestCleanupHelper();
        var tempFile = helper.CreateTempFile("txt");
        var tempDir = helper.CreateTempDirectory();
        var tempFileInDir = Path.Combine(tempDir, "file.txt");
        File.WriteAllText(tempFileInDir, "content");

        var disposableExecuted = false;
        var asyncDisposableExecuted = false;
        var syncActionExecuted = false;
        var asyncActionExecuted = false;

        helper.RegisterDisposable(new TestDisposable(() => disposableExecuted = true));
        helper.RegisterAsyncDisposable(new TestAsyncDisposable(() => asyncDisposableExecuted = true));
        helper.RegisterCleanupAction(() => syncActionExecuted = true);
        helper.RegisterCleanupAction(async () =>
        {
            await Task.Delay(1);
            asyncActionExecuted = true;
        });

        // Act
        await helper.CleanupAsync();

        // Assert
        Assert.True(disposableExecuted);
        Assert.True(asyncDisposableExecuted);
        Assert.True(syncActionExecuted);
        Assert.True(asyncActionExecuted);
        Assert.False(File.Exists(tempFile));
        Assert.False(Directory.Exists(tempDir));
    }

    // Test helper classes
    private class TestDisposable : IDisposable
    {
        private readonly Action? _onDispose;

        public TestDisposable(Action? onDispose = null)
        {
            _onDispose = onDispose;
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            _onDispose?.Invoke();
        }
    }

    private class TestAsyncDisposable : IAsyncDisposable
    {
        private readonly Action? _onDispose;

        public TestAsyncDisposable(Action? onDispose = null)
        {
            _onDispose = onDispose;
        }

        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            _onDispose?.Invoke();
            return ValueTask.CompletedTask;
        }
    }

    private class TestHttpClient : System.Net.Http.HttpClient
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private class TestStream : Stream
    {
        public bool IsDisposed { get; private set; }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => 0;
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}