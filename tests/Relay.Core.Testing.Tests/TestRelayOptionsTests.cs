using System;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class TestRelayOptionsTests
{
    [Fact]
    public void TestRelayOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new TestRelayOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultTimeout);
        Assert.True(options.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableIsolation);
        Assert.Equal(IsolationLevel.DatabaseTransaction, options.IsolationLevel);
        Assert.True(options.EnableAutoCleanup);
        Assert.False(options.EnablePerformanceProfiling);
        Assert.NotNull(options.PerformanceProfiling);
        Assert.False(options.EnableCoverageTracking);
        Assert.NotNull(options.CoverageTracking);
        Assert.False(options.EnableDiagnosticLogging);
        Assert.NotNull(options.DiagnosticLogging);
        Assert.NotNull(options.TestData);
        Assert.NotNull(options.Mock);
        Assert.NotNull(options.Scenario);
        Assert.NotNull(options.EnvironmentOverrides);
        Assert.Empty(options.EnvironmentOverrides);
    }

    [Fact]
    public void PerformanceProfilingOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new PerformanceProfilingOptions();

        // Assert
        Assert.True(options.TrackMemoryUsage);
        Assert.True(options.TrackExecutionTime);
        Assert.Equal(100L, options.MemoryWarningThreshold);
        Assert.Equal(1000L, options.ExecutionTimeWarningThreshold);
        Assert.False(options.EnableDetailedProfiling);
    }

    [Fact]
    public void CoverageTrackingOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new CoverageTrackingOptions();

        // Assert
        Assert.Equal(80.0, options.MinimumCoverageThreshold);
        Assert.True(options.TrackLineCoverage);
        Assert.True(options.TrackBranchCoverage);
        Assert.True(options.TrackMethodCoverage);
        Assert.Equal(CoverageReportFormat.Json, options.ReportFormat);
        Assert.True(options.GenerateReports);
        Assert.Equal("TestCoverageReports", options.ReportOutputDirectory);
    }

    [Fact]
    public void DiagnosticLoggingOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new DiagnosticLoggingOptions();

        // Assert
        Assert.Equal(LogLevel.Information, options.LogLevel);
        Assert.True(options.LogTestExecution);
        Assert.False(options.LogMockInteractions);
        Assert.False(options.LogPerformanceMetrics);
        Assert.Equal("TestLogs", options.LogOutputDirectory);
        Assert.True(options.EnableConsoleLogging);
        Assert.False(options.EnableFileLogging);
    }

    [Fact]
    public void TestDataOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new TestDataOptions();

        // Assert
        Assert.False(options.EnableAutoSeeding);
        Assert.Equal("TestData", options.DataDirectory);
        Assert.False(options.UseSharedData);
        Assert.True(options.EnableDataIsolation);
        Assert.Equal(DataIsolationStrategy.DatabaseTransaction, options.IsolationStrategy);
    }

    [Fact]
    public void MockOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new MockOptions();

        // Assert
        Assert.False(options.EnableStrictVerification);
        Assert.True(options.EnableAutoRegistration);
        Assert.Equal(MockBehavior.Loose, options.DefaultBehavior);
        Assert.True(options.TrackInvocations);
    }

    [Fact]
    public void ScenarioOptions_DefaultValues_AreCorrect()
    {
        // Act
        var options = new ScenarioOptions();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout);
        Assert.False(options.EnableRecording);
        Assert.Equal("ScenarioRecordings", options.RecordingDirectory);
        Assert.False(options.EnableReplay);
        Assert.True(options.EnableStepValidation);
    }

    [Fact]
    public void CoverageReportFormat_EnumValues_AreDefined()
    {
        // Act & Assert
        Assert.Equal(0, (int)CoverageReportFormat.Json);
        Assert.Equal(1, (int)CoverageReportFormat.Xml);
        Assert.Equal(2, (int)CoverageReportFormat.Html);
        Assert.Equal(3, (int)CoverageReportFormat.Cobertura);
    }

    [Fact]
    public void LogLevel_EnumValues_AreDefined()
    {
        // Act & Assert
        Assert.Equal(0, (int)LogLevel.Trace);
        Assert.Equal(1, (int)LogLevel.Debug);
        Assert.Equal(2, (int)LogLevel.Information);
        Assert.Equal(3, (int)LogLevel.Warning);
        Assert.Equal(4, (int)LogLevel.Error);
        Assert.Equal(5, (int)LogLevel.Critical);
    }

    [Fact]
    public void DataIsolationStrategy_EnumValues_AreDefined()
    {
        // Act & Assert
        Assert.Equal(0, (int)DataIsolationStrategy.None);
        Assert.Equal(1, (int)DataIsolationStrategy.Memory);
        Assert.Equal(2, (int)DataIsolationStrategy.DatabaseTransaction);
        Assert.Equal(3, (int)DataIsolationStrategy.FullIsolation);
    }

    [Fact]
    public void MockBehavior_EnumValues_AreDefined()
    {
        // Act & Assert
        Assert.Equal(0, (int)MockBehavior.Loose);
        Assert.Equal(1, (int)MockBehavior.Strict);
    }

    [Fact]
    public void TestRelayOptions_CanSetCustomValues()
    {
        // Act
        var options = new TestRelayOptions
        {
            DefaultTimeout = TimeSpan.FromMinutes(1),
            EnableParallelExecution = false,
            MaxDegreeOfParallelism = 2,
            EnableIsolation = false,
            IsolationLevel = IsolationLevel.Full,
            EnableAutoCleanup = false,
            EnablePerformanceProfiling = true,
            EnableCoverageTracking = true,
            EnableDiagnosticLogging = true
        };

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), options.DefaultTimeout);
        Assert.False(options.EnableParallelExecution);
        Assert.Equal(2, options.MaxDegreeOfParallelism);
        Assert.False(options.EnableIsolation);
        Assert.Equal(IsolationLevel.Full, options.IsolationLevel);
        Assert.False(options.EnableAutoCleanup);
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.EnableCoverageTracking);
        Assert.True(options.EnableDiagnosticLogging);
    }

    [Fact]
    public void TestRelayOptions_EnvironmentOverrides_CanBeModified()
    {
        // Arrange
        var options = new TestRelayOptions();
        var envOptions = new TestRelayOptions { DefaultTimeout = TimeSpan.FromMinutes(2) };

        // Act
        options.EnvironmentOverrides["test"] = envOptions;

        // Assert
        Assert.Single(options.EnvironmentOverrides);
        Assert.Equal(TimeSpan.FromMinutes(2), options.EnvironmentOverrides["test"].DefaultTimeout);
    }
}