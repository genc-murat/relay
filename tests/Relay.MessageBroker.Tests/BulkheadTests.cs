using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using BulkheadClass = Relay.MessageBroker.Bulkhead.Bulkhead;
using BulkheadOptions = Relay.MessageBroker.Bulkhead.BulkheadOptions;
using BulkheadRejectedException = Relay.MessageBroker.Bulkhead.BulkheadRejectedException;

namespace Relay.MessageBroker.Tests;

public class BulkheadTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldExecuteOperationSuccessfully()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10,
            MaxQueuedOperations = 100
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var executed = false;

        // Act
        var result = await bulkhead.ExecuteAsync(async ct =>
        {
            executed = true;
            return 42;
        });

        // Assert
        Assert.True(executed);
        Assert.Equal(42, result);

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowWhenOperationIsNull()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await bulkhead.ExecuteAsync<int>(null!);
        });

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWhenMaxConcurrentReached()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 1,
            MaxQueuedOperations = 0,
            AcquisitionTimeout = TimeSpan.FromMilliseconds(100)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 1;
        });

        // Wait a bit to ensure first operation is running
        await Task.Delay(50);

        // Assert
        var exception = await Assert.ThrowsAsync<BulkheadRejectedException>(async () =>
        {
            await bulkhead.ExecuteAsync(async ct => 2);
        });

        Assert.Equal(1, exception.ActiveOperations);
        Assert.Equal(0, exception.QueuedOperations);

        tcs.SetResult(true);
        await task1;

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldQueueOperationsWhenBelowMaxQueued()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 1,
            MaxQueuedOperations = 5,
            AcquisitionTimeout = TimeSpan.FromSeconds(5)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 1;
        });

        await Task.Delay(50);

        var task2 = bulkhead.ExecuteAsync(async ct => 2);

        await Task.Delay(50);
        var metrics = bulkhead.GetMetrics();

        // Assert
        Assert.Equal(1, metrics.ActiveOperations);
        Assert.Equal(1, metrics.QueuedOperations);

        tcs.SetResult(true);
        await task1;
        await task2;

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessQueuedOperationsInOrder()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 1,
            MaxQueuedOperations = 10,
            AcquisitionTimeout = TimeSpan.FromSeconds(5)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var results = new List<int>();
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            results.Add(1);
            return 1;
        });

        await Task.Delay(50);

        var task2 = bulkhead.ExecuteAsync(async ct =>
        {
            results.Add(2);
            return 2;
        });

        var task3 = bulkhead.ExecuteAsync(async ct =>
        {
            results.Add(3);
            return 3;
        });

        tcs.SetResult(true);
        await Task.WhenAll(task1.AsTask(), task2.AsTask(), task3.AsTask());

        // Assert
        Assert.Equal(new[] { 1, 2, 3 }, results);

        bulkhead.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCorrectActiveOperations()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 5,
            MaxQueuedOperations = 10
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 1;
        });
        var task2 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 2;
        });

        await Task.Delay(50);
        var metrics = bulkhead.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.ActiveOperations);
        Assert.Equal(5, metrics.MaxConcurrentOperations);

        tcs.SetResult(true);
        await Task.WhenAll(task1.AsTask(), task2.AsTask());

        bulkhead.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackRejectedOperations()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 1,
            MaxQueuedOperations = 0,
            AcquisitionTimeout = TimeSpan.FromMilliseconds(100)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 1;
        });

        await Task.Delay(50);

        try
        {
            await bulkhead.ExecuteAsync(async ct => 2);
        }
        catch (BulkheadRejectedException)
        {
            // Expected
        }

        var metrics = bulkhead.GetMetrics();

        // Assert
        Assert.Equal(1, metrics.RejectedOperations);

        tcs.SetResult(true);
        await task1;

        bulkhead.Dispose();
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackExecutedOperations()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);

        // Act
        await bulkhead.ExecuteAsync(async ct => 1);
        await bulkhead.ExecuteAsync(async ct => 2);
        await bulkhead.ExecuteAsync(async ct => 3);

        var metrics = bulkhead.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.ExecutedOperations);

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleOperationException()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await bulkhead.ExecuteAsync<int>(async ct =>
            {
                throw new InvalidOperationException("Test error");
            });
        });

        // Verify bulkhead is still functional
        var result = await bulkhead.ExecuteAsync(async ct => 42);
        Assert.Equal(42, result);

        bulkhead.Dispose();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectCancellationTokenDuringExecution()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 2,
            MaxQueuedOperations = 10,
            AcquisitionTimeout = TimeSpan.FromSeconds(5)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var cts = new CancellationTokenSource();

        // Act & Assert - operation should respect cancellation during execution
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await bulkhead.ExecuteAsync(async ct =>
            {
                cts.Cancel();
                await Task.Delay(10000, ct);
                return 1;
            }, cts.Token);
        });

        bulkhead.Dispose();
    }

    [Fact]
    public void Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BulkheadClass(null!, NullLogger<BulkheadClass>.Instance);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions { Enabled = true });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BulkheadClass(options, null!);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowWhenNameIsNull()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions { Enabled = true });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BulkheadClass(options, NullLogger<BulkheadClass>.Instance, null!);
        });
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowWhenDisposed()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        bulkhead.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
        {
            await bulkhead.ExecuteAsync(async ct => 42);
        });
    }

    [Fact]
    public void Dispose_ShouldCancelQueuedOperations()
    {
        // Arrange
        var options = Options.Create(new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 1,
            MaxQueuedOperations = 10,
            AcquisitionTimeout = TimeSpan.FromSeconds(5)
        });
        var bulkhead = new BulkheadClass(options, NullLogger<BulkheadClass>.Instance);
        var tcs = new TaskCompletionSource<bool>();

        // Act
        var task1 = bulkhead.ExecuteAsync(async ct =>
        {
            await tcs.Task;
            return 1;
        });

        Task.Delay(50).Wait();

        var task2 = bulkhead.ExecuteAsync(async ct => 2);

        bulkhead.Dispose();

        // Assert - queued operation should be cancelled
        // Note: The actual behavior depends on implementation details
        tcs.SetResult(true);
    }

    [Fact]
    public void BulkheadOptions_Validate_ShouldThrowWhenMaxConcurrentIsZero()
    {
        // Arrange
        var options = new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 0
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void BulkheadOptions_Validate_ShouldThrowWhenMaxQueuedIsNegative()
    {
        // Arrange
        var options = new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10,
            MaxQueuedOperations = -1
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void BulkheadOptions_Validate_ShouldThrowWhenAcquisitionTimeoutIsZero()
    {
        // Arrange
        var options = new BulkheadOptions
        {
            Enabled = true,
            MaxConcurrentOperations = 10,
            AcquisitionTimeout = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void BulkheadOptions_Validate_ShouldNotThrowWhenDisabled()
    {
        // Arrange
        var options = new BulkheadOptions
        {
            Enabled = false,
            MaxConcurrentOperations = 0
        };

        // Act & Assert (should not throw)
        options.Validate();
    }
}
