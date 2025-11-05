using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class PerformanceProfilerTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var profiler = new PerformanceProfiler();

        // Assert
        Assert.NotNull(profiler);
        Assert.Null(profiler.ActiveSession);
        Assert.NotNull(profiler.Sessions);
        Assert.Empty(profiler.Sessions);
    }

    [Fact]
    public void StartSession_CreatesAndStartsSession()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var sessionName = "test-session";

        // Act
        var session = profiler.StartSession(sessionName);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(sessionName, session.SessionName);
        Assert.True(session.IsRunning);
        Assert.Equal(session, profiler.ActiveSession);
        Assert.Contains(sessionName, profiler.Sessions.Keys);
        Assert.Equal(session, profiler.Sessions[sessionName]);
    }

    [Fact]
    public void StartSession_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => profiler.StartSession(null!));
    }

    [Fact]
    public void StartSession_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => profiler.StartSession(""));
    }

    [Fact]
    public void StartSession_WithDuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("duplicate");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profiler.StartSession("duplicate"));
    }

    [Fact]
    public void StopSession_StopsRunningSession()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("test");

        // Act
        profiler.StopSession("test");

        // Assert
        Assert.False(session.IsRunning);
        Assert.Null(profiler.ActiveSession);
    }

    [Fact]
    public void StopSession_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profiler.StopSession("nonexistent"));
    }

    [Fact]
    public void StopActiveSession_StopsActiveSession()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("active");

        // Act
        profiler.StopActiveSession();

        // Assert
        Assert.False(session.IsRunning);
        Assert.Null(profiler.ActiveSession);
    }

    [Fact]
    public void StopActiveSession_WithNoActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profiler.StopActiveSession());
    }

    [Fact]
    public void Profile_WithActiveSession_RecordsOperation()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("test");
        var executed = false;

        // Act
        profiler.Profile("test-op", () =>
        {
            executed = true;
            // Simulate some work
            System.Threading.Thread.Sleep(1);
        });

        // Assert
        Assert.True(executed);
        var session = profiler.ActiveSession!;
        Assert.Single(session.Operations);
        var operation = session.Operations[0];
        Assert.Equal("test-op", operation.OperationName);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void Profile_WithoutActiveSession_ThrowsProfilerNotStartedException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<ProfilerNotStartedException>(() => profiler.Profile("test", () => { }));
    }

    [Fact]
    public async Task ProfileAsync_WithActiveSession_RecordsOperation()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("test");
        var executed = false;

        // Act
        await profiler.ProfileAsync("async-test", async () =>
        {
            executed = true;
            await Task.Delay(1);
        });

        // Assert
        Assert.True(executed);
        var session = profiler.ActiveSession!;
        Assert.Single(session.Operations);
        var operation = session.Operations[0];
        Assert.Equal("async-test", operation.OperationName);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task ProfileAsync_WithoutActiveSession_ThrowsProfilerNotStartedException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        await Assert.ThrowsAsync<ProfilerNotStartedException>(() => profiler.ProfileAsync("test", () => Task.CompletedTask));
    }

    [Fact]
    public void Profile_WithReturnValue_ReturnsValueAndRecordsOperation()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("test");

        // Act
        var result = profiler.Profile("returning-op", () =>
        {
            System.Threading.Thread.Sleep(1);
            return 42;
        });

        // Assert
        Assert.Equal(42, result);
        var session = profiler.ActiveSession!;
        Assert.Single(session.Operations);
        var operation = session.Operations[0];
        Assert.Equal("returning-op", operation.OperationName);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public void Profile_WithReturnValue_WithoutActiveSession_ThrowsProfilerNotStartedException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<ProfilerNotStartedException>(() => profiler.Profile("test", () => 42));
    }

    [Fact]
    public async Task ProfileAsync_WithReturnValue_ReturnsValueAndRecordsOperation()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("test");

        // Act
        var result = await profiler.ProfileAsync("async-returning", async () =>
        {
            await Task.Delay(1);
            return "async-result";
        });

        // Assert
        Assert.Equal("async-result", result);
        var session = profiler.ActiveSession!;
        Assert.Single(session.Operations);
        var operation = session.Operations[0];
        Assert.Equal("async-returning", operation.OperationName);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task ProfileAsync_WithReturnValue_WithoutActiveSession_ThrowsProfilerNotStartedException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        await Assert.ThrowsAsync<ProfilerNotStartedException>(() => profiler.ProfileAsync("test", () => Task.FromResult(42)));
    }

    [Fact]
    public void GetSession_ReturnsExistingSession()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("existing");

        // Act
        var retrieved = profiler.GetSession("existing");

        // Assert
        Assert.Equal(session, retrieved);
    }

    [Fact]
    public void GetSession_WithNonExistentSession_ReturnsNull()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act
        var retrieved = profiler.GetSession("nonexistent");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void RemoveSession_RemovesExistingSession()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("to-remove");

        // Act
        var removed = profiler.RemoveSession("to-remove");

        // Assert
        Assert.True(removed);
        Assert.DoesNotContain("to-remove", profiler.Sessions.Keys);
    }

    [Fact]
    public void RemoveSession_WithNonExistentSession_ReturnsFalse()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act
        var removed = profiler.RemoveSession("nonexistent");

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void RemoveSession_RemovesActiveSession_SetsActiveToNull()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("active");

        // Act
        profiler.RemoveSession("active");

        // Assert
        Assert.Null(profiler.ActiveSession);
    }

    [Fact]
    public void Clear_RemovesAllSessions()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("session1");
        profiler.StartSession("session2");

        // Act
        profiler.Clear();

        // Assert
        Assert.Empty(profiler.Sessions);
        Assert.Null(profiler.ActiveSession);
    }

    [Fact]
    public void GenerateReport_WithExistingSession_ReturnsReport()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("report-test");
        profiler.Profile("op1", () => System.Threading.Thread.Sleep(1));
        profiler.StopSession("report-test");

        // Act
        var report = profiler.GenerateReport("report-test");

        // Assert
        Assert.NotNull(report);
        Assert.Equal(session, report.Session);
        Assert.NotNull(report.Thresholds);
    }

    [Fact]
    public void GenerateReport_WithNonExistentSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profiler.GenerateReport("nonexistent"));
    }

    [Fact]
    public void GenerateActiveReport_WithActiveSession_ReturnsReport()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("active-report");

        // Act
        var report = profiler.GenerateActiveReport();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(profiler.ActiveSession, report.Session);
    }

    [Fact]
    public void GenerateActiveReport_WithoutActiveSession_ThrowsInvalidOperationException()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => profiler.GenerateActiveReport());
    }

    [Fact]
    public void Sessions_Property_ReturnsCopy()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("session1");

        // Act
        var sessions = profiler.Sessions;
        profiler.StartSession("session2");

        // Assert - Original dictionary should not be affected by new additions
        Assert.Single(sessions);
        Assert.Contains("session1", sessions.Keys);
        Assert.Equal(2, profiler.Sessions.Count);
    }

    [Fact]
    public void MultipleSessions_CanBeManagedIndependently()
    {
        // Arrange
        var profiler = new PerformanceProfiler();

        // Act
        var session1 = profiler.StartSession("session1");
        profiler.Profile("op1", () => { });

        var session2 = profiler.StartSession("session2");
        profiler.Profile("op2", () => { });

        // Assert
        Assert.Equal(session2, profiler.ActiveSession);
        Assert.Equal(2, profiler.Sessions.Count);
        Assert.Single(session1.Operations);
        Assert.Single(session2.Operations);
    }

    [Fact]
    public void SessionTiming_IsAccurate()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("timing");

        // Act
        System.Threading.Thread.Sleep(10);
        profiler.StopSession("timing");

        // Assert
        Assert.True(session.Duration >= TimeSpan.FromMilliseconds(10));
        Assert.True(session.EndTime > session.StartTime);
    }

    [Fact]
    public void OperationMetrics_ContainExpectedData()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        profiler.StartSession("metrics");

        // Act
        profiler.Profile("detailed-op", () =>
        {
            // Allocate some memory
            var data = new byte[1024];
            System.Threading.Thread.Sleep(5);
        });

        // Assert
        var operation = profiler.ActiveSession!.Operations[0];
        Assert.Equal("detailed-op", operation.OperationName);
        Assert.True(operation.Duration >= TimeSpan.FromMilliseconds(5));
        Assert.True(operation.StartTime <= operation.EndTime);
        // Memory usage may vary, but should be non-negative
        Assert.True(operation.MemoryUsed >= 0);
    }

    [Fact]
    public void ProfilerNotStartedException_IncludesMessage()
    {
        // Act
        var exception = new ProfilerNotStartedException("Test message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void ProfileReport_ToConsole_IncludesSessionInfo()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("console-report");
        profiler.Profile("op1", () => System.Threading.Thread.Sleep(1));
        profiler.StopSession("console-report");

        var report = profiler.GenerateReport("console-report");

        // Act
        var consoleOutput = report.ToConsole();

        // Assert
        Assert.Contains("Performance Profile Report: console-report", consoleOutput);
        Assert.Contains("Session Duration:", consoleOutput);
        Assert.Contains("Total Memory Used:", consoleOutput);
        Assert.Contains("Operations Count: 1", consoleOutput);
        Assert.Contains("op1", consoleOutput);
    }

    [Fact]
    public void ProfileReport_ToJson_IncludesSessionData()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("json-report");
        profiler.Profile("op1", () => { });
        profiler.StopSession("json-report");

        var report = profiler.GenerateReport("json-report");

        // Act
        var jsonOutput = report.ToJson();

        // Assert
        Assert.Contains("json-report", jsonOutput);
        Assert.Contains("op1", jsonOutput);
        Assert.Contains("Operations", jsonOutput);
        Assert.Contains("DurationMs", jsonOutput);
    }

    [Fact]
    public void ProfileReport_ToCsv_IncludesSessionData()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("csv-report");
        profiler.Profile("op1", () => { });
        profiler.StopSession("csv-report");

        var report = profiler.GenerateReport("csv-report");

        // Act
        var csvOutput = report.ToCsv();

        // Assert
        Assert.Contains("Session Summary", csvOutput);
        Assert.Contains("csv-report", csvOutput);
        Assert.Contains("Operations", csvOutput);
        Assert.Contains("op1", csvOutput);
    }

    [Fact]
    public void ProfileReport_WithThresholds_GeneratesWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("warnings");
        profiler.Profile("slow-op", () => System.Threading.Thread.Sleep(10));
        profiler.StopSession("warnings");

        var thresholds = new PerformanceThresholds
        {
            MaxOperationDuration = TimeSpan.FromMilliseconds(1) // Very low threshold
        };

        var report = profiler.GenerateReport("warnings", thresholds);

        // Act & Assert
        Assert.NotEmpty(report.Warnings);
        Assert.Contains("slow-op", report.Warnings[0]);
        Assert.Contains("exceeds threshold", report.Warnings[0]);
    }

    [Fact]
    public void ProfileReport_WithoutThresholds_HasEmptyWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("no-warnings");
        profiler.Profile("fast-op", () => { });
        profiler.StopSession("no-warnings");

        var report = profiler.GenerateReport("no-warnings");

        // Act & Assert
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void ProfileReport_ToConsole_IncludesWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("console-warnings");
        profiler.Profile("slow-op", () => System.Threading.Thread.Sleep(10));
        profiler.StopSession("console-warnings");

        var thresholds = new PerformanceThresholds
        {
            MaxOperationDuration = TimeSpan.FromMilliseconds(1) // Very low threshold
        };

        var report = profiler.GenerateReport("console-warnings", thresholds);

        // Act
        var consoleOutput = report.ToConsole();

        // Assert
        Assert.Contains("Warnings:", consoleOutput);
        Assert.Contains("slow-op", consoleOutput);
        Assert.Contains("exceeds threshold", consoleOutput);
    }

    [Fact]
    public void ProfileReport_ToCsv_IncludesWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("csv-warnings");
        profiler.Profile("slow-op", () => System.Threading.Thread.Sleep(10));
        profiler.StopSession("csv-warnings");

        var thresholds = new PerformanceThresholds
        {
            MaxOperationDuration = TimeSpan.FromMilliseconds(1) // Very low threshold
        };

        var report = profiler.GenerateReport("csv-warnings", thresholds);

        // Act
        var csvOutput = report.ToCsv();

        // Assert
        Assert.Contains("Warnings", csvOutput);
        Assert.Contains("slow-op", csvOutput);
        Assert.Contains("exceeds threshold", csvOutput);
    }

    [Fact]
    public void ProfileReport_WithSessionThresholds_GeneratesWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("session-thresholds");
        profiler.Profile("op1", () => System.Threading.Thread.Sleep(10));
        profiler.StopSession("session-thresholds");

        var thresholds = new PerformanceThresholds
        {
            MaxDuration = TimeSpan.FromMilliseconds(1), // Very low threshold - should trigger
            MaxMemory = -1, // Negative threshold - should trigger since memory usage is always >= 0
            MaxAllocations = -1 // Negative threshold - should trigger since allocations are always >= 0
        };

        var report = profiler.GenerateReport("session-thresholds", thresholds);

        // Act & Assert
        Assert.NotEmpty(report.Warnings);
        var allWarnings = string.Join(" ", report.Warnings);
        Assert.Contains("Session duration", allWarnings);
        Assert.Contains("Total memory usage", allWarnings);
        Assert.Contains("Total allocations", allWarnings);
    }

    [Fact]
    public void ProfileReport_WithOperationMemoryThreshold_GeneratesWarnings()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("operation-memory");
        profiler.Profile("memory-op", () => {
            var _ = new byte[100000]; // Allocate more memory
            GC.Collect(); // Force GC to update memory stats
        });
        profiler.StopSession("operation-memory");

        var thresholds = new PerformanceThresholds
        {
            MaxOperationMemory = -1 // Negative threshold - should trigger since memory usage is always >= 0
        };

        var report = profiler.GenerateReport("operation-memory", thresholds);

        // Act & Assert
        Assert.NotEmpty(report.Warnings);
        Assert.Contains("memory usage", string.Join(" ", report.Warnings));
        Assert.Contains("exceeds threshold", string.Join(" ", report.Warnings));
    }

    [Fact]
    public void ProfileReport_FormatBytes_HandlesLargeValues()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("large-bytes");
        profiler.Profile("op1", () => { });
        profiler.StopSession("large-bytes");

        var report = profiler.GenerateReport("large-bytes");

        // Act - Test FormatBytes indirectly through ToConsole with large memory values
        // We need to create a session with large memory values
        var mockSession = new ProfileSession("large-test");
        mockSession.Start();
        // Manually add an operation with large memory
        var operation = new OperationMetrics
        {
            OperationName = "large-op",
            Duration = TimeSpan.FromMilliseconds(100),
            MemoryUsed = 1024L * 1024L * 1024L * 2L, // 2GB
            Allocations = 100
        };
        mockSession.AddOperation(operation);
        mockSession.Stop();

        var largeReport = new ProfileReport(mockSession);

        // Act
        var consoleOutput = largeReport.ToConsole();

        // Assert
        Assert.Contains("GB", consoleOutput); // Should format as GB
    }

    [Fact]
    public void ProfileReport_EscapeCsv_HandlesSpecialCharacters()
    {
        // Arrange
        var profiler = new PerformanceProfiler();
        var session = profiler.StartSession("csv-special");
        profiler.Profile("op,with\"quotes", () => { });
        profiler.StopSession("csv-special");

        var report = profiler.GenerateReport("csv-special");

        // Act
        var csvOutput = report.ToCsv();

        // Assert
        Assert.Contains("\"op,with\"\"quotes\"", csvOutput); // Should be properly escaped
    }

    [Fact]
    public void PerformanceThresholds_DefaultValues_AreNull()
    {
        // Act
        var thresholds = new PerformanceThresholds();

        // Assert
        Assert.Null(thresholds.MaxDuration);
        Assert.Null(thresholds.MaxMemory);
        Assert.Null(thresholds.MaxAllocations);
        Assert.Null(thresholds.MaxOperationDuration);
        Assert.Null(thresholds.MaxOperationMemory);
    }

    [Fact]
    public void OperationMetrics_CalculatedProperties_Work()
    {
        // Arrange
        var metrics = new OperationMetrics
        {
            OperationName = "test",
            Duration = TimeSpan.FromMilliseconds(100),
            MemoryUsed = 1000,
            Allocations = 50,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddMilliseconds(100)
        };

        // Act & Assert
        Assert.Equal(10.0, metrics.MemoryPerMs);
        Assert.Equal(0.5, metrics.AllocationsPerMs);
    }

    [Fact]
    public void OperationMetrics_ZeroDuration_HandlesDivisionByZero()
    {
        // Arrange
        var metrics = new OperationMetrics
        {
            OperationName = "test",
            Duration = TimeSpan.Zero,
            MemoryUsed = 1000,
            Allocations = 50
        };

        // Act & Assert
        Assert.Equal(0.0, metrics.MemoryPerMs);
        Assert.Equal(0.0, metrics.AllocationsPerMs);
    }

    [Fact]
    public void ProfileSession_AggregatedProperties_Work()
    {
        // Arrange
        var session = new ProfileSession("aggregate-test");
        session.Start();

        var metrics1 = new OperationMetrics { Duration = TimeSpan.FromMilliseconds(10), MemoryUsed = 100, Allocations = 5 };
        var metrics2 = new OperationMetrics { Duration = TimeSpan.FromMilliseconds(20), MemoryUsed = 200, Allocations = 10 };

        session.AddOperation(metrics1);
        session.AddOperation(metrics2);

        // Act & Assert
        Assert.Equal(300, session.TotalMemoryUsed);
        Assert.Equal(15, session.TotalAllocations);
        Assert.Equal(TimeSpan.FromMilliseconds(15), session.AverageOperationDuration); // (10+20)/2 = 15
    }

    [Fact]
    public void ProfileSession_EmptyOperations_ReturnsZeroAverages()
    {
        // Arrange
        var session = new ProfileSession("empty-test");

        // Act & Assert
        Assert.Equal(0, session.TotalMemoryUsed);
        Assert.Equal(0, session.TotalAllocations);
        Assert.Equal(TimeSpan.Zero, session.AverageOperationDuration);
    }

    [Fact]
    public void ProfileSession_StartStop_UpdatesState()
    {
        // Arrange
        var session = new ProfileSession("start-stop");

        // Initially not started
        Assert.False(session.IsRunning);
        Assert.Equal(DateTime.MinValue, session.EndTime);

        // Act - Start
        session.Start();

        // Assert - Running
        Assert.True(session.IsRunning);
        Assert.NotEqual(DateTime.MinValue, session.StartTime);
        Assert.Null(session.EndTime);

        // Act - Stop
        session.Stop();

        // Assert - Stopped
        Assert.False(session.IsRunning);
        Assert.NotNull(session.EndTime);
        Assert.True(session.EndTime > session.StartTime);
    }

    [Fact]
    public void ProfileSession_StartTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new ProfileSession("double-start");
        session.Start();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Start());
    }

    [Fact]
    public void ProfileSession_StopWithoutStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new ProfileSession("stop-without-start");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Stop());
    }

    [Fact]
    public void ProfileSession_StopTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new ProfileSession("double-stop");
        session.Start();
        session.Stop();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => session.Stop());
    }

    [Fact]
    public void ProfileSession_AddOperation_ValidatesInput()
    {
        // Arrange
        var session = new ProfileSession("validation-test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => session.AddOperation(null!));
    }

    [Fact]
    public void ProfileSession_Clear_RemovesAllOperations()
    {
        // Arrange
        var session = new ProfileSession("clear-test");
        session.AddOperation(new OperationMetrics { OperationName = "op1" });
        session.AddOperation(new OperationMetrics { OperationName = "op2" });

        // Act
        session.Clear();

        // Assert
        Assert.Empty(session.Operations);
    }

    [Fact]
    public void MetricsCollector_Collect_RecordsMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();
        var executed = false;

        // Act
        var metrics = collector.Collect("test-collect", () =>
        {
            executed = true;
            System.Threading.Thread.Sleep(2);
        });

        // Assert
        Assert.True(executed);
        Assert.Equal("test-collect", metrics.OperationName);
        Assert.True(metrics.Duration >= TimeSpan.FromMilliseconds(2));
        Assert.True(metrics.StartTime <= metrics.EndTime);
        Assert.True(metrics.MemoryUsed >= 0); // Memory usage should be non-negative
    }

    [Fact]
    public async Task MetricsCollector_CollectAsync_RecordsMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();
        var executed = false;

        // Act
        var metrics = await collector.CollectAsync("async-test", async () =>
        {
            executed = true;
            await Task.Delay(2);
        });

        // Assert
        Assert.True(executed);
        Assert.Equal("async-test", metrics.OperationName);
        Assert.True(metrics.Duration >= TimeSpan.FromMilliseconds(2));
        Assert.True(metrics.StartTime <= metrics.EndTime);
    }

    [Fact]
    public void MetricsCollector_Collect_WithReturnValue_ReturnsValueAndMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        var (result, metrics) = collector.Collect("returning-test", () =>
        {
            System.Threading.Thread.Sleep(1);
            return "test-result";
        });

        // Assert
        Assert.Equal("test-result", result);
        Assert.Equal("returning-test", metrics.OperationName);
        Assert.True(metrics.Duration >= TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public async Task MetricsCollector_CollectAsync_WithReturnValue_ReturnsValueAndMetrics()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act
        var (result, metrics) = await collector.CollectAsync("async-returning", async () =>
        {
            await Task.Delay(1);
            return 12345;
        });

        // Assert
        Assert.Equal(12345, result);
        Assert.Equal("async-returning", metrics.OperationName);
        Assert.True(metrics.Duration >= TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void MetricsCollector_Collect_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => collector.Collect("test", null!));
    }

    [Fact]
    public async Task MetricsCollector_CollectAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var collector = new MetricsCollector();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => collector.CollectAsync("test", null!));
    }
}