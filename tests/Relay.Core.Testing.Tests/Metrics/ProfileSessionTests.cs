using System;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class ProfileSessionTests
{
    [Fact]
    public void Constructor_WithNullSessionName_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new ProfileSession(null!));
        Assert.Equal("sessionName", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidSessionName_SetsProperties()
    {
        // Arrange
        var sessionName = "TestSession";

        // Act
        var session = new ProfileSession(sessionName);

        // Assert
        Assert.Equal(sessionName, session.SessionName);
        Assert.False(session.IsRunning);
        Assert.Equal(DateTime.MinValue, session.EndTime);
        Assert.Equal(TimeSpan.Zero, session.Duration);
        Assert.Empty(session.Operations);
        Assert.Equal(0, session.TotalMemoryUsed);
        Assert.Equal(0, session.TotalAllocations);
        Assert.Equal(TimeSpan.Zero, session.AverageOperationDuration);
    }

    [Fact]
    public void Start_SetsStartTimeAndRunningState()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        var beforeStart = DateTime.UtcNow;

        // Act
        session.Start();

        // Assert
        Assert.True(session.IsRunning);
        Assert.Null(session.EndTime);
        Assert.True(session.StartTime >= beforeStart);
        Assert.True(session.StartTime <= DateTime.UtcNow);
    }

    [Fact]
    public void Start_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.Start();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => session.Start());
        Assert.Contains("already running", exception.Message);
    }

    [Fact]
    public void Stop_SetsEndTimeAndStopsRunning()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.Start();
        var beforeStop = DateTime.UtcNow;

        // Act
        session.Stop();

        // Assert
        Assert.False(session.IsRunning);
        Assert.NotNull(session.EndTime);
        Assert.True(session.EndTime >= beforeStop);
        Assert.True(session.EndTime <= DateTime.UtcNow);
        Assert.True(session.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void Stop_WhenNotRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new ProfileSession("TestSession");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => session.Stop());
        Assert.Contains("not running", exception.Message);
    }

    [Fact]
    public void Duration_WhenRunning_ReturnsTimeSinceStart()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.Start();

        // Act
        var duration1 = session.Duration;
        Task.Delay(10).Wait(); // Small delay
        var duration2 = session.Duration;

        // Assert
        Assert.True(duration2 > duration1);
        Assert.True(duration2 > TimeSpan.Zero);
    }

    [Fact]
    public void Duration_WhenStopped_ReturnsTotalSessionTime()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.Start();
        Task.Delay(10).Wait(); // Small delay
        session.Stop();

        // Act
        var stoppedDuration1 = session.Duration;
        Task.Delay(5).Wait(); // Additional delay after stopping
        var stoppedDuration2 = session.Duration;

        // Assert - Duration should remain the same after stopping
        Assert.Equal(stoppedDuration1, stoppedDuration2);
    }

    [Fact]
    public void AddOperation_WithNullMetrics_ThrowsArgumentNullException()
    {
        // Arrange
        var session = new ProfileSession("TestSession");

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => session.AddOperation(null!));
        Assert.Equal("metrics", exception.ParamName);
    }

    [Fact]
    public void AddOperation_WithValidMetrics_AddsToOperations()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        var metrics = new OperationMetrics
        {
            OperationName = "TestOp",
            Duration = TimeSpan.FromMilliseconds(100),
            MemoryUsed = 1024,
            Allocations = 10
        };

        // Act
        session.AddOperation(metrics);

        // Assert
        Assert.Single(session.Operations);
        Assert.Equal(metrics, session.Operations[0]);
        Assert.Equal(1024, session.TotalMemoryUsed);
        Assert.Equal(10, session.TotalAllocations);
    }

    [Fact]
    public void AddOperation_MultipleOperations_CalculatesAggregatesCorrectly()
    {
        // Arrange
        var session = new ProfileSession("TestSession");

        var metrics1 = new OperationMetrics
        {
            OperationName = "Op1",
            Duration = TimeSpan.FromMilliseconds(100),
            MemoryUsed = 1024,
            Allocations = 10
        };

        var metrics2 = new OperationMetrics
        {
            OperationName = "Op2",
            Duration = TimeSpan.FromMilliseconds(200),
            MemoryUsed = 2048,
            Allocations = 20
        };

        // Act
        session.AddOperation(metrics1);
        session.AddOperation(metrics2);

        // Assert
        Assert.Equal(2, session.Operations.Count);
        Assert.Equal(3072, session.TotalMemoryUsed); // 1024 + 2048
        Assert.Equal(30, session.TotalAllocations); // 10 + 20
        Assert.Equal(TimeSpan.FromMilliseconds(150), session.AverageOperationDuration); // (100 + 200) / 2
    }

    [Fact]
    public void AverageOperationDuration_WithNoOperations_ReturnsZero()
    {
        // Arrange
        var session = new ProfileSession("TestSession");

        // Act & Assert
        Assert.Equal(TimeSpan.Zero, session.AverageOperationDuration);
    }

    [Fact]
    public void Clear_RemovesAllOperations()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.AddOperation(new OperationMetrics { OperationName = "Op1" });
        session.AddOperation(new OperationMetrics { OperationName = "Op2" });

        // Act
        session.Clear();

        // Assert
        Assert.Empty(session.Operations);
        Assert.Equal(0, session.TotalMemoryUsed);
        Assert.Equal(0, session.TotalAllocations);
        Assert.Equal(TimeSpan.Zero, session.AverageOperationDuration);
    }

    [Fact]
    public void Operations_IsReadOnly_ReturnsSameInstance()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        var operations = session.Operations;

        // Act
        session.AddOperation(new OperationMetrics { OperationName = "Op1" });

        // Assert
        Assert.Same(operations, session.Operations);
        Assert.Single(operations);
    }

    [Fact]
    public void ThreadSafety_StartStopOperations_WorkConcurrently()
    {
        // Arrange
        var session = new ProfileSession("TestSession");

        // Act - Run multiple operations concurrently
        var tasks = new[]
        {
            Task.Run(() => {
                session.Start();
                Task.Delay(10).Wait();
                session.Stop();
            }),
            Task.Run(() => {
                Task.Delay(5).Wait();
                session.AddOperation(new OperationMetrics { OperationName = "ConcurrentOp" });
            })
        };

        Task.WaitAll(tasks);

        // Assert - Should not throw and should have completed successfully
        Assert.False(session.IsRunning);
        Assert.NotNull(session.EndTime);
        Assert.Single(session.Operations);
    }

    [Fact]
    public void Properties_AfterStop_RemainConsistent()
    {
        // Arrange
        var session = new ProfileSession("TestSession");
        session.Start();
        session.AddOperation(new OperationMetrics
        {
            OperationName = "TestOp",
            Duration = TimeSpan.FromMilliseconds(50),
            MemoryUsed = 512,
            Allocations = 5
        });
        session.Stop();

        var durationAfterStop = session.Duration;
        var memoryAfterStop = session.TotalMemoryUsed;

        // Act - Wait a bit more
        Task.Delay(10).Wait();

        // Assert - Values should remain the same
        Assert.Equal(durationAfterStop, session.Duration);
        Assert.Equal(memoryAfterStop, session.TotalMemoryUsed);
    }
}