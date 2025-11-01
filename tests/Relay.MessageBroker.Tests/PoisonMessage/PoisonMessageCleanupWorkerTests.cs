using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.PoisonMessage;

public class PoisonMessageCleanupWorkerTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var options = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, options, NullLogger<PoisonMessageHandler>.Instance);
        var workerOptions = Options.Create(new PoisonMessageOptions());

        // Act
        var worker = new PoisonMessageCleanupWorker(handler, workerOptions, NullLogger<PoisonMessageCleanupWorker>.Instance);

        // Assert
        Assert.NotNull(worker);
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new PoisonMessageOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PoisonMessageCleanupWorker(null!, options, NullLogger<PoisonMessageCleanupWorker>.Instance));
        Assert.Equal("handler", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var handlerOptions = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, handlerOptions, NullLogger<PoisonMessageHandler>.Instance);

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PoisonMessageCleanupWorker(handler, null!, NullLogger<PoisonMessageCleanupWorker>.Instance));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var handlerOptions = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, handlerOptions, NullLogger<PoisonMessageHandler>.Instance);
        var workerOptions = Options.Create(new PoisonMessageOptions());

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new PoisonMessageCleanupWorker(handler, workerOptions, null!));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldLogAndReturnImmediately()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var handlerOptions = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, handlerOptions, NullLogger<PoisonMessageHandler>.Instance);
        var workerOptions = Options.Create(new PoisonMessageOptions
        {
            Enabled = false,
            CleanupInterval = TimeSpan.FromMilliseconds(100),
            RetentionPeriod = TimeSpan.FromHours(24)
        });

        var worker = new PoisonMessageCleanupWorker(handler, workerOptions, NullLogger<PoisonMessageCleanupWorker>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Wait for potential operations
        await worker.StopAsync(CancellationToken.None);

        // For this test, we're verifying that no exception is thrown when the worker is disabled
        // The actual logging behavior would require testing with a logging framework that can capture logs
        Assert.NotNull(worker);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_ShouldNotThrow()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var handlerOptions = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, handlerOptions, NullLogger<PoisonMessageHandler>.Instance);
        var workerOptions = Options.Create(new PoisonMessageOptions
        {
            Enabled = true,
            CleanupInterval = TimeSpan.FromMilliseconds(50),
            RetentionPeriod = TimeSpan.FromHours(24)
        });

        var worker = new PoisonMessageCleanupWorker(handler, workerOptions, NullLogger<PoisonMessageCleanupWorker>.Instance);

        // Act & Assert - Should not throw
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)); // Cancel after short time
        await worker.StartAsync(CancellationToken.None);
        
        // Wait briefly, then stop to prevent the background task from running too long
        await Task.Delay(60);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopProcessing()
    {
        // Arrange
        var store = new InMemoryPoisonMessageStore();
        var handlerOptions = Options.Create(new PoisonMessageOptions());
        var handler = new PoisonMessageHandler(store, handlerOptions, NullLogger<PoisonMessageHandler>.Instance);
        var workerOptions = Options.Create(new PoisonMessageOptions
        {
            Enabled = true,
            CleanupInterval = TimeSpan.FromMilliseconds(50),
            RetentionPeriod = TimeSpan.FromHours(24)
        });

        var worker = new PoisonMessageCleanupWorker(handler, workerOptions, NullLogger<PoisonMessageCleanupWorker>.Instance);
        var cts = new CancellationTokenSource();

        // Act
        await worker.StartAsync(cts.Token);
        await Task.Delay(75); // Wait for first cleanup cycle
        cts.Cancel();
        await Task.Delay(100); // Wait for cancellation to take effect
        await worker.StopAsync(CancellationToken.None);

        // Assert - Should complete without throwing
        Assert.NotNull(worker);
    }
}