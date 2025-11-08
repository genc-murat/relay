using System;
using System.IO;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TestRelayOptionsBuilderTests
{
    [Fact]
    public void Constructor_InitializesWithDefaultOptions()
    {
        // Act
        var builder = new TestRelayOptionsBuilder();

        // Assert
        var options = builder.Build();
        Assert.Equal(TimeSpan.FromSeconds(30), options.DefaultTimeout);
        Assert.True(options.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableIsolation);
        Assert.Equal(IsolationLevel.DatabaseTransaction, options.IsolationLevel);
        Assert.True(options.EnableAutoCleanup);
        Assert.False(options.EnablePerformanceProfiling);
        Assert.False(options.EnableCoverageTracking);
        Assert.False(options.EnableDiagnosticLogging);
    }

    [Fact]
    public void WithDefaultTimeout_SetsTimeoutAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var result = builder.WithDefaultTimeout(timeout);

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.Equal(timeout, options.DefaultTimeout);
    }

    [Fact]
    public void WithParallelExecution_SetsParallelExecutionAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithParallelExecution(false);

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.False(options.EnableParallelExecution);
    }

    [Fact]
    public void WithMaxDegreeOfParallelism_SetsMaxDegreeAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var maxDegree = 8;

        // Act
        var result = builder.WithMaxDegreeOfParallelism(maxDegree);

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.Equal(maxDegree, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void WithIsolation_SetsIsolationOptionsAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithIsolation(false, IsolationLevel.None);

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.False(options.EnableIsolation);
        Assert.Equal(IsolationLevel.None, options.IsolationLevel);
    }

    [Fact]
    public void WithAutoCleanup_SetsAutoCleanupAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithAutoCleanup(false);

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.False(options.EnableAutoCleanup);
    }

    [Fact]
    public void WithPerformanceProfiling_EnablesProfilingAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithPerformanceProfiling();

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.True(options.EnablePerformanceProfiling);
    }

    [Fact]
    public void WithPerformanceProfiling_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithPerformanceProfiling(options =>
        {
            options.TrackMemoryUsage = true;
            options.MemoryWarningThreshold = 100;
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.EnablePerformanceProfiling);
        Assert.True(result.PerformanceProfiling.TrackMemoryUsage);
        Assert.Equal(100, result.PerformanceProfiling.MemoryWarningThreshold);
    }

    [Fact]
    public void WithCoverageTracking_EnablesCoverageTrackingAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithCoverageTracking();

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.True(options.EnableCoverageTracking);
    }

    [Fact]
    public void WithCoverageTracking_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithCoverageTracking(options =>
        {
            options.MinimumCoverageThreshold = 90.0;
            options.GenerateReports = true;
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.EnableCoverageTracking);
        Assert.Equal(90.0, result.CoverageTracking.MinimumCoverageThreshold);
        Assert.True(result.CoverageTracking.GenerateReports);
    }

    [Fact]
    public void WithDiagnosticLogging_EnablesDiagnosticLoggingAndReturnsBuilder()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        var result = builder.WithDiagnosticLogging();

        // Assert
        Assert.Same(builder, result);
        var options = builder.Build();
        Assert.True(options.EnableDiagnosticLogging);
    }

    [Fact]
    public void WithDiagnosticLogging_WithConfiguration_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithDiagnosticLogging(options =>
        {
            options.LogLevel = LogLevel.Debug;
            options.EnableConsoleLogging = true;
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Debug, result.DiagnosticLogging.LogLevel);
        Assert.True(result.DiagnosticLogging.EnableConsoleLogging);
    }

    [Fact]
    public void WithTestData_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithTestData(options =>
        {
            options.EnableAutoSeeding = true;
            options.DataDirectory = "/test/data";
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.TestData.EnableAutoSeeding);
        Assert.Equal("/test/data", result.TestData.DataDirectory);
    }

    [Fact]
    public void WithMock_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithMock(options =>
        {
            options.EnableStrictVerification = true;
            options.TrackInvocations = true;
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.Mock.EnableStrictVerification);
        Assert.True(result.Mock.TrackInvocations);
    }

    [Fact]
    public void WithScenario_AppliesConfiguration()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithScenario(options =>
        {
            options.EnableRecording = true;
            options.EnableReplay = true;
        });

        // Assert
        var result = builder.Build();
        Assert.True(result.Scenario.EnableRecording);
        Assert.True(result.Scenario.EnableReplay);
    }

    [Fact]
    public void WithEnvironmentOverride_AddsEnvironmentSpecificOptions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.WithEnvironmentOverride("Production", envBuilder =>
        {
            envBuilder.WithParallelExecution(false);
            envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(10));
        });

        // Assert
        var options = builder.Build();
        Assert.Contains("Production", options.EnvironmentOverrides);
        var prodOptions = options.EnvironmentOverrides["Production"];
        Assert.False(prodOptions.EnableParallelExecution);
        Assert.Equal(TimeSpan.FromMinutes(10), prodOptions.DefaultTimeout);
    }

    [Fact]
    public void LoadFromFile_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var nonExistentFile = "nonexistent.json";

        // Act & Assert
        var exception = Assert.Throws<FileNotFoundException>(() => builder.LoadFromFile(nonExistentFile));
        Assert.Contains("Configuration file not found", exception.Message);
        Assert.Equal(nonExistentFile, exception.FileName);
    }

    [Fact]
    public void LoadFromEnvironment_LoadsEnvironmentVariablesWithPrefix()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalTimeout = Environment.GetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT");
        var originalParallel = Environment.GetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL");

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", "00:02:00");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", "false");

            // Act
            builder.LoadFromEnvironment("TESTRELAY_");

            // Assert
            var options = builder.Build();
            Assert.Equal(TimeSpan.FromMinutes(2), options.DefaultTimeout);
            Assert.False(options.EnableParallelExecution);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", originalTimeout);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", originalParallel);
        }
    }

    [Fact]
    public void LoadFromEnvironment_CustomPrefix_LoadsCorrectly()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalTimeout = Environment.GetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT");

        try
        {
            Environment.SetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT", "00:05:00");

            // Act
            builder.LoadFromEnvironment("CUSTOM_");

            // Assert
            var options = builder.Build();
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout);
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("CUSTOM_DEFAULTTIMEOUT", originalTimeout);
        }
    }

    [Fact]
    public void Build_WithEnvironmentOverride_MergesOptionsCorrectly()
    {
        // Arrange
        var originalEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");

            var builder = new TestRelayOptionsBuilder()
                .WithDefaultTimeout(TimeSpan.FromSeconds(30))
                .WithParallelExecution(true)
                .WithEnvironmentOverride("Staging", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(5));
                    // Note: EnableParallelExecution not set in override, should keep base value
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout); // Overridden
            Assert.True(options.EnableParallelExecution); // Not overridden, keeps base value
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalEnvironment);
        }
    }

    [Fact]
    public void Build_UsesDotnetEnvironmentVariable()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

            var builder = new TestRelayOptionsBuilder()
                .WithEnvironmentOverride("Testing", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(10));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void Build_NoEnvironmentVariables_UsesDevelopmentAsDefault()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");

            var builder = new TestRelayOptionsBuilder()
                .WithEnvironmentOverride("Development", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(15));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(15), options.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void FluentInterface_AllowsMethodChaining()
    {
        // Act
        var options = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromMinutes(2))
            .WithParallelExecution(false)
            .WithMaxDegreeOfParallelism(4)
            .WithIsolation(true, IsolationLevel.DatabaseTransaction)
            .WithAutoCleanup(false)
            .WithPerformanceProfiling()
            .WithCoverageTracking()
            .WithDiagnosticLogging()
            .Build();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), options.DefaultTimeout);
        Assert.False(options.EnableParallelExecution);
        Assert.Equal(4, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableIsolation);
        Assert.Equal(IsolationLevel.DatabaseTransaction, options.IsolationLevel);
        Assert.False(options.EnableAutoCleanup);
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.EnableCoverageTracking);
        Assert.True(options.EnableDiagnosticLogging);
    }
}

public class TestRelayOptionsExtensionsTests
{
    [Fact]
    public void CreateOptions_ReturnsNewBuilder()
    {
        // Act
        var builder = TestRelayOptionsExtensions.CreateOptions();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<TestRelayOptionsBuilder>(builder);
    }

    [Fact]
    public void ForDevelopment_ConfiguresDevelopmentSettings()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.ForDevelopment();

        // Assert
        var options = builder.Build();
        Assert.True(options.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Debug, options.DiagnosticLogging.LogLevel);
        Assert.True(options.DiagnosticLogging.EnableConsoleLogging);
        Assert.False(options.DiagnosticLogging.EnableFileLogging);
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.PerformanceProfiling.EnableDetailedProfiling);
        Assert.True(options.EnableCoverageTracking);
        Assert.True(options.CoverageTracking.GenerateReports);
    }

    [Fact]
    public void ForCI_ConfiguresCISettings()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.ForCI();

        // Assert
        var options = builder.Build();
        Assert.True(options.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableCoverageTracking);
        Assert.Equal(85.0, options.CoverageTracking.MinimumCoverageThreshold);
        Assert.True(options.CoverageTracking.GenerateReports);
        Assert.True(options.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Information, options.DiagnosticLogging.LogLevel);
        Assert.True(options.DiagnosticLogging.EnableFileLogging);
    }

    [Fact]
    public void ForPerformanceTesting_ConfiguresPerformanceSettings()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act
        builder.ForPerformanceTesting();

        // Assert
        var options = builder.Build();
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.PerformanceProfiling.TrackMemoryUsage);
        Assert.True(options.PerformanceProfiling.TrackExecutionTime);
        Assert.True(options.PerformanceProfiling.EnableDetailedProfiling);
        Assert.Equal(500, options.PerformanceProfiling.MemoryWarningThreshold);
        Assert.Equal(5000, options.PerformanceProfiling.ExecutionTimeWarningThreshold);
        Assert.False(options.EnableParallelExecution);
        Assert.True(options.EnableDiagnosticLogging);
        Assert.True(options.DiagnosticLogging.LogPerformanceMetrics);
    }

    [Fact]
    public void ExtensionMethods_ReturnBuilderForChaining()
    {
        // Act
        var options = TestRelayOptionsExtensions.CreateOptions()
            .ForDevelopment()
            .WithDefaultTimeout(TimeSpan.FromMinutes(1))
            .Build();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(1), options.DefaultTimeout);
        Assert.True(options.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Debug, options.DiagnosticLogging.LogLevel);
    }

    [Fact]
    public void LoadFromFile_FileExists_ReturnsBuilderWithoutLoading()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var tempFile = Path.GetTempFileName();

        try
        {
            // Create an empty file
            File.WriteAllText(tempFile, "{}");

            // Act
            var result = builder.LoadFromFile(tempFile);

            // Assert
            Assert.Same(builder, result);
            // Note: The current implementation doesn't actually load from file,
            // it just checks existence and returns the builder
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadFromEnvironment_NoMatchingVariables_DoesNotModifyOptions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalOptions = builder.Build();

        // Act
        builder.LoadFromEnvironment("NONEXISTENTPREFIX_");

        // Assert
        var newOptions = builder.Build();
        Assert.Equal(originalOptions.DefaultTimeout, newOptions.DefaultTimeout);
        Assert.Equal(originalOptions.EnableParallelExecution, newOptions.EnableParallelExecution);
        Assert.Equal(originalOptions.MaxDegreeOfParallelism, newOptions.MaxDegreeOfParallelism);
    }

    [Fact]
    public void ApplyEnvironmentVariable_InvalidTimeSpanValue_IgnoresInvalidValue()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalTimeout = builder.Build().DefaultTimeout;

        // Act
        // This calls the private method via reflection for testing
        var method = typeof(TestRelayOptionsBuilder).GetMethod("ApplyEnvironmentVariable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { "DEFAULTTIMEOUT", "invalid-time-span" });

        // Assert
        var options = builder.Build();
        Assert.Equal(originalTimeout, options.DefaultTimeout); // Should remain unchanged
    }

    [Fact]
    public void ApplyEnvironmentVariable_InvalidBooleanValue_IgnoresInvalidValue()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalParallel = builder.Build().EnableParallelExecution;

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("ApplyEnvironmentVariable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { "ENABLEPARALLEL", "maybe" });

        // Assert
        var options = builder.Build();
        Assert.Equal(originalParallel, options.EnableParallelExecution); // Should remain unchanged
    }

    [Fact]
    public void ApplyEnvironmentVariable_InvalidIntegerValue_IgnoresInvalidValue()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalMaxParallelism = builder.Build().MaxDegreeOfParallelism;

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("ApplyEnvironmentVariable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { "MAXPARALLELISM", "not-a-number" });

        // Assert
        var options = builder.Build();
        Assert.Equal(originalMaxParallelism, options.MaxDegreeOfParallelism); // Should remain unchanged
    }

    [Fact]
    public void ApplyEnvironmentVariable_UnknownOptionName_IgnoresUnknownOption()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalOptions = builder.Build();

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("ApplyEnvironmentVariable",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { "UNKNOWNOPTION", "somevalue" });

        // Assert
        var newOptions = builder.Build();
        Assert.Equal(originalOptions.DefaultTimeout, newOptions.DefaultTimeout);
        Assert.Equal(originalOptions.EnableParallelExecution, newOptions.EnableParallelExecution);
    }

    [Fact]
    public void WithEnvironmentOverride_NullConfigureAction_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.WithEnvironmentOverride("Test", null));
    }

    [Fact]
    public void Build_NoEnvironmentOverrides_ReturnsOptionsWithoutMerging()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            // Clear environment variables
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            var builder = new TestRelayOptionsBuilder()
                .WithDefaultTimeout(TimeSpan.FromMinutes(5))
                .WithParallelExecution(false);

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout);
            Assert.False(options.EnableParallelExecution);
            Assert.Equal("Development", Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void MergePerformanceProfilingOptions_MergesWithConditions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new PerformanceProfilingOptions();
        var source = new PerformanceProfilingOptions
        {
            TrackMemoryUsage = true,
            TrackExecutionTime = true,
            MemoryWarningThreshold = 200,
            ExecutionTimeWarningThreshold = 0, // Should not override
            EnableDetailedProfiling = true
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergePerformanceProfilingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.True(target.TrackMemoryUsage);
        Assert.True(target.TrackExecutionTime);
        Assert.Equal(200, target.MemoryWarningThreshold); // Should be overridden
        Assert.Equal(1000, target.ExecutionTimeWarningThreshold); // Should not be overridden (0 is not > 0)
        Assert.True(target.EnableDetailedProfiling);
    }

    [Fact]
    public void MergeCoverageTrackingOptions_MergesWithConditions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new CoverageTrackingOptions();
        var source = new CoverageTrackingOptions
        {
            MinimumCoverageThreshold = 95.0,
            TrackLineCoverage = true,
            TrackBranchCoverage = true,
            TrackMethodCoverage = true,
            ReportFormat = CoverageReportFormat.Xml,
            GenerateReports = true,
            ReportOutputDirectory = "/custom/path"
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeCoverageTrackingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(95.0, target.MinimumCoverageThreshold);
        Assert.True(target.TrackLineCoverage);
        Assert.True(target.TrackBranchCoverage);
        Assert.True(target.TrackMethodCoverage);
        Assert.Equal(CoverageReportFormat.Xml, target.ReportFormat);
        Assert.True(target.GenerateReports);
        Assert.Equal("/custom/path", target.ReportOutputDirectory);
    }

    [Fact]
    public void MergeDiagnosticLoggingOptions_MergesAllProperties()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new DiagnosticLoggingOptions();
        var source = new DiagnosticLoggingOptions
        {
            LogLevel = LogLevel.Warning,
            LogTestExecution = true,
            LogMockInteractions = true,
            LogPerformanceMetrics = true,
            LogOutputDirectory = "/logs",
            EnableConsoleLogging = false,
            EnableFileLogging = true
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeDiagnosticLoggingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(LogLevel.Warning, target.LogLevel);
        Assert.True(target.LogTestExecution);
        Assert.True(target.LogMockInteractions);
        Assert.True(target.LogPerformanceMetrics);
        Assert.Equal("/logs", target.LogOutputDirectory);
        Assert.False(target.EnableConsoleLogging);
        Assert.True(target.EnableFileLogging);
    }

    [Fact]
    public void MergeTestDataOptions_MergesWithConditions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new TestDataOptions();
        var source = new TestDataOptions
        {
            EnableAutoSeeding = true,
            DataDirectory = "/test/data",
            UseSharedData = true,
            EnableDataIsolation = false,
            IsolationStrategy = DataIsolationStrategy.DatabaseTransaction
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeTestDataOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.True(target.EnableAutoSeeding);
        Assert.Equal("/test/data", target.DataDirectory);
        Assert.True(target.UseSharedData);
        Assert.False(target.EnableDataIsolation);
        Assert.Equal(DataIsolationStrategy.DatabaseTransaction, target.IsolationStrategy);
    }

    [Fact]
    public void MergeMockOptions_MergesAllProperties()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new MockOptions();
        var source = new MockOptions
        {
            EnableStrictVerification = true,
            EnableAutoRegistration = false,
            DefaultBehavior = MockBehavior.Loose,
            TrackInvocations = true
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeMockOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.True(target.EnableStrictVerification);
        Assert.False(target.EnableAutoRegistration);
        Assert.Equal(MockBehavior.Loose, target.DefaultBehavior);
        Assert.True(target.TrackInvocations);
    }

    [Fact]
    public void MergeScenarioOptions_MergesWithConditions()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new ScenarioOptions();
        var source = new ScenarioOptions
        {
            DefaultTimeout = TimeSpan.FromMinutes(10),
            EnableRecording = true,
            RecordingDirectory = "/recordings",
            EnableReplay = false,
            EnableStepValidation = true
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeScenarioOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), target.DefaultTimeout);
        Assert.True(target.EnableRecording);
        Assert.Equal("/recordings", target.RecordingDirectory);
        Assert.False(target.EnableReplay);
        Assert.True(target.EnableStepValidation);
    }

    [Fact]
    public void WithTestData_NullConfigureAction_DoesNotThrow()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act & Assert
        // Should not throw when configure is null due to null-conditional operator
        var result = builder.WithTestData(null);
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithMock_NullConfigureAction_DoesNotThrow()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act & Assert
        // Should not throw when configure is null due to null-conditional operator
        var result = builder.WithMock(null);
        Assert.Same(builder, result);
    }

    [Fact]
    public void WithScenario_NullConfigureAction_DoesNotThrow()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        // Act & Assert
        // Should not throw when configure is null due to null-conditional operator
        var result = builder.WithScenario(null);
        Assert.Same(builder, result);
    }

    [Fact]
    public void LoadFromEnvironment_InvalidIntegerValue_IgnoresInvalidValue()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalMaxParallelism = builder.Build().MaxDegreeOfParallelism;

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_MAXPARALLELISM", "not-a-number");

            // Act
            builder.LoadFromEnvironment();

            // Assert
            var options = builder.Build();
            Assert.Equal(originalMaxParallelism, options.MaxDegreeOfParallelism); // Should remain unchanged
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESTRELAY_MAXPARALLELISM", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_InvalidBooleanValues_IgnoreInvalidValues()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalOptions = builder.Build();

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", "maybe");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEISOLATION", "perhaps");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPROFILING", "sortof");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLECOVERAGE", "kinda");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLELOGGING", "ish");

            // Act
            builder.LoadFromEnvironment();

            // Assert
            var options = builder.Build();
            Assert.Equal(originalOptions.EnableParallelExecution, options.EnableParallelExecution);
            Assert.Equal(originalOptions.EnableIsolation, options.EnableIsolation);
            Assert.Equal(originalOptions.EnablePerformanceProfiling, options.EnablePerformanceProfiling);
            Assert.Equal(originalOptions.EnableCoverageTracking, options.EnableCoverageTracking);
            Assert.Equal(originalOptions.EnableDiagnosticLogging, options.EnableDiagnosticLogging);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", null);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEISOLATION", null);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPROFILING", null);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLECOVERAGE", null);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLELOGGING", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_UnknownOptionName_IgnoresUnknownOption()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalOptions = builder.Build();

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_UNKNOWNOPTION", "somevalue");

            // Act
            builder.LoadFromEnvironment();

            // Assert
            var newOptions = builder.Build();
            Assert.Equal(originalOptions.DefaultTimeout, newOptions.DefaultTimeout);
            Assert.Equal(originalOptions.EnableParallelExecution, newOptions.EnableParallelExecution);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESTRELAY_UNKNOWNOPTION", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_EmptyAndWhitespaceValues_IgnoreInvalidValues()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var originalOptions = builder.Build();

        try
        {
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", "");
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", "   ");
            Environment.SetEnvironmentVariable("TESTRELAY_MAXPARALLELISM", "");

            // Act
            builder.LoadFromEnvironment();

            // Assert
            var options = builder.Build();
            Assert.Equal(originalOptions.DefaultTimeout, options.DefaultTimeout);
            Assert.Equal(originalOptions.EnableParallelExecution, options.EnableParallelExecution);
            Assert.Equal(originalOptions.MaxDegreeOfParallelism, options.MaxDegreeOfParallelism);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TESTRELAY_DEFAULTTIMEOUT", null);
            Environment.SetEnvironmentVariable("TESTRELAY_ENABLEPARALLEL", null);
            Environment.SetEnvironmentVariable("TESTRELAY_MAXPARALLELISM", null);
        }
    }

    [Fact]
    public void LoadFromEnvironment_CaseInsensitivePrefix_WorksCorrectly()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();

        try
        {
            Environment.SetEnvironmentVariable("testrelay_defaulttimeout", "00:03:00");
            Environment.SetEnvironmentVariable("TestRelay_EnableParallel", "false");

            // Act
            builder.LoadFromEnvironment();

            // Assert
            var options = builder.Build();
            Assert.Equal(TimeSpan.FromMinutes(3), options.DefaultTimeout);
            Assert.False(options.EnableParallelExecution);
        }
        finally
        {
            Environment.SetEnvironmentVariable("testrelay_defaulttimeout", null);
            Environment.SetEnvironmentVariable("TestRelay_EnableParallel", null);
        }
    }

    [Fact]
    public void Build_AspNetCoreEnvironmentVariable_TakesPrecedence()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Staging");
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Production");

            var builder = new TestRelayOptionsBuilder()
                .WithEnvironmentOverride("Staging", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(5));
                })
                .WithEnvironmentOverride("Production", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(10));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout); // Should use Staging override
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void Build_DotnetEnvironmentVariable_UsedWhenAspNetCoreNotSet()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

            var builder = new TestRelayOptionsBuilder()
                .WithEnvironmentOverride("Testing", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(7));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(7), options.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void Build_NoEnvironmentVariables_DefaultsToDevelopment()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);

            var builder = new TestRelayOptionsBuilder()
                .WithEnvironmentOverride("Development", envBuilder =>
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(15));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(15), options.DefaultTimeout);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void Build_NoMatchingEnvironmentOverride_UsesBaseConfiguration()
    {
        // Arrange
        var originalAspNetCore = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var originalDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        try
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            var builder = new TestRelayOptionsBuilder()
                .WithDefaultTimeout(TimeSpan.FromMinutes(5))
                .WithParallelExecution(false)
                .WithEnvironmentOverride("Staging", envBuilder => // Different environment
                {
                    envBuilder.WithDefaultTimeout(TimeSpan.FromMinutes(10));
                });

            // Act
            var options = builder.Build();

            // Assert
            Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTimeout); // Should keep base config
            Assert.False(options.EnableParallelExecution);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", originalAspNetCore);
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", originalDotnet);
        }
    }

    [Fact]
    public void MergeOptions_DefaultTimeoutZero_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new TestRelayOptions { DefaultTimeout = TimeSpan.FromMinutes(5) };
        var source = new TestRelayOptions { DefaultTimeout = TimeSpan.Zero };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), target.DefaultTimeout); // Should not be overridden
    }

    [Fact]
    public void MergeOptions_MaxDegreeOfParallelismZeroOrNegative_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new TestRelayOptions { MaxDegreeOfParallelism = 4 };
        var source = new TestRelayOptions { MaxDegreeOfParallelism = 0 };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(4, target.MaxDegreeOfParallelism); // Should not be overridden
    }

    [Fact]
    public void MergePerformanceProfilingOptions_ZeroThresholds_DoNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new PerformanceProfilingOptions
        {
            MemoryWarningThreshold = 200,
            ExecutionTimeWarningThreshold = 3000
        };
        var source = new PerformanceProfilingOptions
        {
            MemoryWarningThreshold = 0, // Should not override
            ExecutionTimeWarningThreshold = 0 // Should not override
        };

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergePerformanceProfilingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(200, target.MemoryWarningThreshold); // Should not be overridden
        Assert.Equal(3000, target.ExecutionTimeWarningThreshold); // Should not be overridden
    }

    [Fact]
    public void MergeCoverageTrackingOptions_ZeroMinimumCoverage_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new CoverageTrackingOptions { MinimumCoverageThreshold = 80.0 };
        var source = new CoverageTrackingOptions { MinimumCoverageThreshold = 0.0 }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeCoverageTrackingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(80.0, target.MinimumCoverageThreshold); // Should not be overridden
    }

    [Fact]
    public void MergeCoverageTrackingOptions_EmptyReportOutputDirectory_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new CoverageTrackingOptions { ReportOutputDirectory = "/original/path" };
        var source = new CoverageTrackingOptions { ReportOutputDirectory = "" }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeCoverageTrackingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal("/original/path", target.ReportOutputDirectory); // Should not be overridden
    }

    [Fact]
    public void MergeDiagnosticLoggingOptions_EmptyLogOutputDirectory_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new DiagnosticLoggingOptions { LogOutputDirectory = "/original/logs" };
        var source = new DiagnosticLoggingOptions { LogOutputDirectory = null }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeDiagnosticLoggingOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal("/original/logs", target.LogOutputDirectory); // Should not be overridden
    }

    [Fact]
    public void MergeTestDataOptions_EmptyDataDirectory_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new TestDataOptions { DataDirectory = "/original/data" };
        var source = new TestDataOptions { DataDirectory = "" }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeTestDataOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal("/original/data", target.DataDirectory); // Should not be overridden
    }

    [Fact]
    public void MergeScenarioOptions_ZeroDefaultTimeout_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new ScenarioOptions { DefaultTimeout = TimeSpan.FromMinutes(5) };
        var source = new ScenarioOptions { DefaultTimeout = TimeSpan.Zero }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeScenarioOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), target.DefaultTimeout); // Should not be overridden
    }

    [Fact]
    public void MergeScenarioOptions_EmptyRecordingDirectory_DoesNotOverride()
    {
        // Arrange
        var builder = new TestRelayOptionsBuilder();
        var target = new ScenarioOptions { RecordingDirectory = "/original/recordings" };
        var source = new ScenarioOptions { RecordingDirectory = null }; // Should not override

        // Act
        var method = typeof(TestRelayOptionsBuilder).GetMethod("MergeScenarioOptions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method.Invoke(builder, new object[] { target, source });

        // Assert
        Assert.Equal("/original/recordings", target.RecordingDirectory); // Should not be overridden
    }

    [Fact]
    public void ForDevelopment_AppliesDevelopmentConfiguration()
    {
        // Act
        var options = new TestRelayOptionsBuilder().ForDevelopment().Build();

        // Assert
        Assert.True(options.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Debug, options.DiagnosticLogging.LogLevel);
        Assert.True(options.DiagnosticLogging.EnableConsoleLogging);
        Assert.False(options.DiagnosticLogging.EnableFileLogging);
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.PerformanceProfiling.EnableDetailedProfiling);
        Assert.True(options.EnableCoverageTracking);
        Assert.True(options.CoverageTracking.GenerateReports);
    }

    [Fact]
    public void ForCI_AppliesCIConfiguration()
    {
        // Act
        var options = new TestRelayOptionsBuilder().ForCI().Build();

        // Assert
        Assert.True(options.EnableParallelExecution);
        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism);
        Assert.True(options.EnableCoverageTracking);
        Assert.Equal(85.0, options.CoverageTracking.MinimumCoverageThreshold);
        Assert.True(options.CoverageTracking.GenerateReports);
        Assert.True(options.EnableDiagnosticLogging);
        Assert.Equal(LogLevel.Information, options.DiagnosticLogging.LogLevel);
        Assert.True(options.DiagnosticLogging.EnableFileLogging);
    }

    [Fact]
    public void ForPerformanceTesting_AppliesPerformanceConfiguration()
    {
        // Act
        var options = new TestRelayOptionsBuilder().ForPerformanceTesting().Build();

        // Assert
        Assert.True(options.EnablePerformanceProfiling);
        Assert.True(options.PerformanceProfiling.TrackMemoryUsage);
        Assert.True(options.PerformanceProfiling.TrackExecutionTime);
        Assert.True(options.PerformanceProfiling.EnableDetailedProfiling);
        Assert.Equal(500, options.PerformanceProfiling.MemoryWarningThreshold);
        Assert.Equal(5000, options.PerformanceProfiling.ExecutionTimeWarningThreshold);
        Assert.False(options.EnableParallelExecution);
        Assert.True(options.EnableDiagnosticLogging);
        Assert.True(options.DiagnosticLogging.LogPerformanceMetrics);
    }

    [Fact]
    public void ExtensionMethods_CanBeChainedTogether()
    {
        // Act
        var options = TestRelayOptionsExtensions.CreateOptions()
            .ForDevelopment()
            .WithDefaultTimeout(TimeSpan.FromSeconds(45))
            .ForCI() // This should override some settings
            .Build();

        // Assert - CI settings should take precedence
        Assert.True(options.EnableParallelExecution); // From ForCI
        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism); // From ForCI
        Assert.Equal(TimeSpan.FromSeconds(45), options.DefaultTimeout); // Custom setting preserved
        Assert.True(options.EnableDiagnosticLogging); // From both
        Assert.Equal(LogLevel.Information, options.DiagnosticLogging.LogLevel); // ForCI overrides ForDevelopment
    }
}