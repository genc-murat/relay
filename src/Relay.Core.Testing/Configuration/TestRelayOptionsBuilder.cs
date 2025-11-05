using System;
using System.Collections.Generic;
using System.IO;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for configuring TestRelay options.
/// </summary>
public class TestRelayOptionsBuilder
{
    private readonly TestRelayOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestRelayOptionsBuilder"/> class.
    /// </summary>
    public TestRelayOptionsBuilder()
    {
        _options = new TestRelayOptions();
    }

    /// <summary>
    /// Sets the default timeout for test operations.
    /// </summary>
    /// <param name="timeout">The default timeout.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithDefaultTimeout(TimeSpan timeout)
    {
        _options.DefaultTimeout = timeout;
        return this;
    }

    /// <summary>
    /// Enables or disables parallel test execution.
    /// </summary>
    /// <param name="enable">Whether to enable parallel execution.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithParallelExecution(bool enable = true)
    {
        _options.EnableParallelExecution = enable;
        return this;
    }

    /// <summary>
    /// Sets the maximum degree of parallelism.
    /// </summary>
    /// <param name="maxDegree">The maximum degree of parallelism.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithMaxDegreeOfParallelism(int maxDegree)
    {
        _options.MaxDegreeOfParallelism = maxDegree;
        return this;
    }

    /// <summary>
    /// Enables or disables test isolation.
    /// </summary>
    /// <param name="enable">Whether to enable isolation.</param>
    /// <param name="level">The isolation level.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithIsolation(bool enable = true, IsolationLevel level = IsolationLevel.DatabaseTransaction)
    {
        _options.EnableIsolation = enable;
        _options.IsolationLevel = level;
        return this;
    }

    /// <summary>
    /// Enables or disables automatic cleanup.
    /// </summary>
    /// <param name="enable">Whether to enable auto cleanup.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithAutoCleanup(bool enable = true)
    {
        _options.EnableAutoCleanup = enable;
        return this;
    }

    /// <summary>
    /// Enables performance profiling with specified options.
    /// </summary>
    /// <param name="configure">Action to configure performance profiling options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithPerformanceProfiling(Action<PerformanceProfilingOptions> configure = null)
    {
        _options.EnablePerformanceProfiling = true;
        configure?.Invoke(_options.PerformanceProfiling);
        return this;
    }

    /// <summary>
    /// Enables coverage tracking with specified options.
    /// </summary>
    /// <param name="configure">Action to configure coverage tracking options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithCoverageTracking(Action<CoverageTrackingOptions> configure = null)
    {
        _options.EnableCoverageTracking = true;
        configure?.Invoke(_options.CoverageTracking);
        return this;
    }

    /// <summary>
    /// Enables diagnostic logging with specified options.
    /// </summary>
    /// <param name="configure">Action to configure diagnostic logging options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithDiagnosticLogging(Action<DiagnosticLoggingOptions> configure = null)
    {
        _options.EnableDiagnosticLogging = true;
        configure?.Invoke(_options.DiagnosticLogging);
        return this;
    }

    /// <summary>
    /// Configures test data options.
    /// </summary>
    /// <param name="configure">Action to configure test data options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithTestData(Action<TestDataOptions> configure)
    {
        configure?.Invoke(_options.TestData);
        return this;
    }

    /// <summary>
    /// Configures mock options.
    /// </summary>
    /// <param name="configure">Action to configure mock options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithMock(Action<MockOptions> configure)
    {
        configure?.Invoke(_options.Mock);
        return this;
    }

    /// <summary>
    /// Configures scenario options.
    /// </summary>
    /// <param name="configure">Action to configure scenario options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithScenario(Action<ScenarioOptions> configure)
    {
        configure?.Invoke(_options.Scenario);
        return this;
    }

    /// <summary>
    /// Adds environment-specific overrides.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="configure">Action to configure environment-specific options.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder WithEnvironmentOverride(string environment, Action<TestRelayOptionsBuilder> configure)
    {
        var envBuilder = new TestRelayOptionsBuilder();
        configure(envBuilder);
        _options.EnvironmentOverrides[environment] = envBuilder.Build();
        return this;
    }

    /// <summary>
    /// Loads configuration from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to the configuration file.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        // In a real implementation, you would deserialize from JSON
        // For now, we'll just return the builder as-is
        return this;
    }

    /// <summary>
    /// Loads configuration from environment variables.
    /// </summary>
    /// <param name="prefix">The environment variable prefix.</param>
    /// <returns>The builder instance for chaining.</returns>
    public TestRelayOptionsBuilder LoadFromEnvironment(string prefix = "TESTRELAY_")
    {
        // Load from environment variables with the specified prefix
        var envVars = Environment.GetEnvironmentVariables();

        foreach (var key in envVars.Keys)
        {
            var keyStr = key.ToString();
            if (keyStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var optionName = keyStr.Substring(prefix.Length);
                var value = envVars[key].ToString();

                // Map environment variables to options
                ApplyEnvironmentVariable(optionName, value);
            }
        }

        return this;
    }

    /// <summary>
    /// Builds the TestRelay options.
    /// </summary>
    /// <returns>The configured TestRelay options.</returns>
    public TestRelayOptions Build()
    {
        // Apply environment-specific overrides if applicable
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                         "Development";

        if (_options.EnvironmentOverrides.TryGetValue(environment, out var envOptions))
        {
            // Merge environment-specific options
            MergeOptions(_options, envOptions);
        }

        return _options;
    }

    private void ApplyEnvironmentVariable(string optionName, string value)
    {
        // Simple mapping of environment variables to options
        switch (optionName.ToUpperInvariant())
        {
            case "DEFAULTTIMEOUT":
                if (TimeSpan.TryParse(value, out var timeout))
                    _options.DefaultTimeout = timeout;
                break;

            case "ENABLEPARALLEL":
                if (bool.TryParse(value, out var enableParallel))
                    _options.EnableParallelExecution = enableParallel;
                break;

            case "MAXPARALLELISM":
                if (int.TryParse(value, out var maxParallelism))
                    _options.MaxDegreeOfParallelism = maxParallelism;
                break;

            case "ENABLEISOLATION":
                if (bool.TryParse(value, out var enableIsolation))
                    _options.EnableIsolation = enableIsolation;
                break;

            case "ENABLEPROFILING":
                if (bool.TryParse(value, out var enableProfiling))
                    _options.EnablePerformanceProfiling = enableProfiling;
                break;

            case "ENABLECOVERAGE":
                if (bool.TryParse(value, out var enableCoverage))
                    _options.EnableCoverageTracking = enableCoverage;
                break;

            case "ENABLELOGGING":
                if (bool.TryParse(value, out var enableLogging))
                    _options.EnableDiagnosticLogging = enableLogging;
                break;
        }
    }

    internal void MergeOptions(TestRelayOptions target, TestRelayOptions source)
    {
        // Simple property merging - in a real implementation, you might use reflection or a merging library
        target.DefaultTimeout = source.DefaultTimeout != TimeSpan.Zero ? source.DefaultTimeout : target.DefaultTimeout;
        target.EnableParallelExecution = source.EnableParallelExecution;
        target.MaxDegreeOfParallelism = source.MaxDegreeOfParallelism > 0 ? source.MaxDegreeOfParallelism : target.MaxDegreeOfParallelism;
        target.EnableIsolation = source.EnableIsolation;
        target.IsolationLevel = source.IsolationLevel;
        target.EnableAutoCleanup = source.EnableAutoCleanup;
        target.EnablePerformanceProfiling = source.EnablePerformanceProfiling;
        target.EnableCoverageTracking = source.EnableCoverageTracking;
        target.EnableDiagnosticLogging = source.EnableDiagnosticLogging;

        // Merge complex objects
        MergePerformanceProfilingOptions(target.PerformanceProfiling, source.PerformanceProfiling);
        MergeCoverageTrackingOptions(target.CoverageTracking, source.CoverageTracking);
        MergeDiagnosticLoggingOptions(target.DiagnosticLogging, source.DiagnosticLogging);
        MergeTestDataOptions(target.TestData, source.TestData);
        MergeMockOptions(target.Mock, source.Mock);
        MergeScenarioOptions(target.Scenario, source.Scenario);
    }

    private void MergePerformanceProfilingOptions(PerformanceProfilingOptions target, PerformanceProfilingOptions source)
    {
        target.TrackMemoryUsage = source.TrackMemoryUsage;
        target.TrackExecutionTime = source.TrackExecutionTime;
        target.MemoryWarningThreshold = source.MemoryWarningThreshold > 0 ? source.MemoryWarningThreshold : target.MemoryWarningThreshold;
        target.ExecutionTimeWarningThreshold = source.ExecutionTimeWarningThreshold > 0 ? source.ExecutionTimeWarningThreshold : target.ExecutionTimeWarningThreshold;
        target.EnableDetailedProfiling = source.EnableDetailedProfiling;
    }

    private void MergeCoverageTrackingOptions(CoverageTrackingOptions target, CoverageTrackingOptions source)
    {
        target.MinimumCoverageThreshold = source.MinimumCoverageThreshold > 0 ? source.MinimumCoverageThreshold : target.MinimumCoverageThreshold;
        target.TrackLineCoverage = source.TrackLineCoverage;
        target.TrackBranchCoverage = source.TrackBranchCoverage;
        target.TrackMethodCoverage = source.TrackMethodCoverage;
        target.ReportFormat = source.ReportFormat;
        target.GenerateReports = source.GenerateReports;
        target.ReportOutputDirectory = !string.IsNullOrEmpty(source.ReportOutputDirectory) ? source.ReportOutputDirectory : target.ReportOutputDirectory;
    }

    private void MergeDiagnosticLoggingOptions(DiagnosticLoggingOptions target, DiagnosticLoggingOptions source)
    {
        target.LogLevel = source.LogLevel;
        target.LogTestExecution = source.LogTestExecution;
        target.LogMockInteractions = source.LogMockInteractions;
        target.LogPerformanceMetrics = source.LogPerformanceMetrics;
        target.LogOutputDirectory = !string.IsNullOrEmpty(source.LogOutputDirectory) ? source.LogOutputDirectory : target.LogOutputDirectory;
        target.EnableConsoleLogging = source.EnableConsoleLogging;
        target.EnableFileLogging = source.EnableFileLogging;
    }

    private void MergeTestDataOptions(TestDataOptions target, TestDataOptions source)
    {
        target.EnableAutoSeeding = source.EnableAutoSeeding;
        target.DataDirectory = !string.IsNullOrEmpty(source.DataDirectory) ? source.DataDirectory : target.DataDirectory;
        target.UseSharedData = source.UseSharedData;
        target.EnableDataIsolation = source.EnableDataIsolation;
        target.IsolationStrategy = source.IsolationStrategy;
    }

    private void MergeMockOptions(MockOptions target, MockOptions source)
    {
        target.EnableStrictVerification = source.EnableStrictVerification;
        target.EnableAutoRegistration = source.EnableAutoRegistration;
        target.DefaultBehavior = source.DefaultBehavior;
        target.TrackInvocations = source.TrackInvocations;
    }

    private void MergeScenarioOptions(ScenarioOptions target, ScenarioOptions source)
    {
        target.DefaultTimeout = source.DefaultTimeout != TimeSpan.Zero ? source.DefaultTimeout : target.DefaultTimeout;
        target.EnableRecording = source.EnableRecording;
        target.RecordingDirectory = !string.IsNullOrEmpty(source.RecordingDirectory) ? source.RecordingDirectory : target.RecordingDirectory;
        target.EnableReplay = source.EnableReplay;
        target.EnableStepValidation = source.EnableStepValidation;
    }
}

/// <summary>
/// Extension methods for TestRelay options.
/// </summary>
public static class TestRelayOptionsExtensions
{
    /// <summary>
    /// Creates a new TestRelay options builder.
    /// </summary>
    /// <returns>A new TestRelay options builder.</returns>
    public static TestRelayOptionsBuilder CreateOptions()
    {
        return new TestRelayOptionsBuilder();
    }

    /// <summary>
    /// Configures TestRelay for development environment.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <returns>The configured builder.</returns>
    public static TestRelayOptionsBuilder ForDevelopment(this TestRelayOptionsBuilder builder)
    {
        return builder
            .WithDiagnosticLogging(options =>
            {
                options.LogLevel = LogLevel.Debug;
                options.EnableConsoleLogging = true;
                options.EnableFileLogging = false;
            })
            .WithPerformanceProfiling(options =>
            {
                options.EnableDetailedProfiling = true;
            })
            .WithCoverageTracking(options =>
            {
                options.GenerateReports = true;
            });
    }

    /// <summary>
    /// Configures TestRelay for CI/CD environment.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <returns>The configured builder.</returns>
    public static TestRelayOptionsBuilder ForCI(this TestRelayOptionsBuilder builder)
    {
        return builder
            .WithParallelExecution(true)
            .WithMaxDegreeOfParallelism(Environment.ProcessorCount * 2)
            .WithCoverageTracking(options =>
            {
                options.GenerateReports = true;
                options.MinimumCoverageThreshold = 85.0;
            })
            .WithDiagnosticLogging(options =>
            {
                options.LogLevel = LogLevel.Information;
                options.EnableFileLogging = true;
            });
    }

    /// <summary>
    /// Configures TestRelay for performance testing.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <returns>The configured builder.</returns>
    public static TestRelayOptionsBuilder ForPerformanceTesting(this TestRelayOptionsBuilder builder)
    {
        return builder
            .WithPerformanceProfiling(options =>
            {
                options.TrackMemoryUsage = true;
                options.TrackExecutionTime = true;
                options.EnableDetailedProfiling = true;
                options.MemoryWarningThreshold = 500; // 500MB
                options.ExecutionTimeWarningThreshold = 5000; // 5 seconds
            })
            .WithParallelExecution(false) // Sequential for accurate measurements
            .WithDiagnosticLogging(options =>
            {
                options.LogPerformanceMetrics = true;
            });
    }
}