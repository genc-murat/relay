using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Relay.Core.Testing;

/// <summary>
/// Manages environment-specific test configurations.
/// </summary>
public class TestEnvironmentConfiguration
{
    private readonly Dictionary<string, TestRelayOptions> _environmentConfigs = new();
    private readonly TestRelayOptions _defaultOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestEnvironmentConfiguration"/> class.
    /// </summary>
    /// <param name="defaultOptions">The default test options.</param>
    public TestEnvironmentConfiguration(TestRelayOptions defaultOptions)
    {
        _defaultOptions = defaultOptions ?? throw new ArgumentNullException(nameof(defaultOptions));
    }

    /// <summary>
    /// Gets the current environment name.
    /// </summary>
    public string CurrentEnvironment => GetCurrentEnvironmentName();

    /// <summary>
    /// Adds configuration for a specific environment.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="options">The environment-specific options.</param>
    public void AddEnvironmentConfig(string environment, TestRelayOptions options)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment name cannot be null or empty", nameof(environment));

        _environmentConfigs[environment] = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Adds configuration for a specific environment using a builder.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="configure">Action to configure the environment options.</param>
    public void AddEnvironmentConfig(string environment, Action<TestRelayOptionsBuilder> configure)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment name cannot be null or empty", nameof(environment));

        var builder = new TestRelayOptionsBuilder();
        configure(builder);
        _environmentConfigs[environment] = builder.Build();
    }

    /// <summary>
    /// Gets the effective options for the current environment.
    /// </summary>
    /// <returns>The merged options for the current environment.</returns>
    public TestRelayOptions GetEffectiveOptions()
    {
        return GetEffectiveOptions(CurrentEnvironment);
    }

    /// <summary>
    /// Gets the effective options for a specific environment.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <returns>The merged options for the specified environment.</returns>
    public TestRelayOptions GetEffectiveOptions(string environment)
    {
        var options = CloneOptions(_defaultOptions);

        if (_environmentConfigs.TryGetValue(environment, out var envOptions))
        {
            MergeOptions(options, envOptions);
        }

        return options;
    }

    /// <summary>
    /// Loads environment configurations from a directory.
    /// </summary>
    /// <param name="configDirectory">The directory containing configuration files.</param>
    public void LoadFromDirectory(string configDirectory)
    {
        if (!Directory.Exists(configDirectory))
            return;

        var configFiles = Directory.GetFiles(configDirectory, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var configFile in configFiles)
        {
            var environment = Path.GetFileNameWithoutExtension(configFile);
            try
            {
                // In a real implementation, you would deserialize the JSON
                // For now, we'll skip loading
                // var options = JsonSerializer.Deserialize<TestRelayOptions>(File.ReadAllText(configFile));
                // AddEnvironmentConfig(environment, options);
            }
            catch
            {
                // Continue loading other configurations
            }
        }
    }

    /// <summary>
    /// Saves the current configuration to a file.
    /// </summary>
    /// <param name="filePath">The file path to save to.</param>
    /// <param name="environment">The environment to save (optional).</param>
    public void SaveToFile(string filePath, string environment = null)
    {
        var options = string.IsNullOrEmpty(environment) ? _defaultOptions : GetEffectiveOptions(environment);

        // In a real implementation, you would serialize to JSON
        // File.WriteAllText(filePath, JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>
    /// Gets all configured environments.
    /// </summary>
    /// <returns>A collection of environment names.</returns>
    public IEnumerable<string> GetEnvironments()
    {
        return _environmentConfigs.Keys.OrderBy(e => e);
    }

    /// <summary>
    /// Validates the configuration for all environments.
    /// </summary>
    /// <returns>A collection of validation errors.</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        // Validate default options
        errors.AddRange(ValidateOptions(_defaultOptions, "Default"));

        // Validate environment-specific options
        foreach (var env in _environmentConfigs)
        {
            errors.AddRange(ValidateOptions(env.Value, env.Key));
        }

        return errors;
    }

    private IEnumerable<string> ValidateOptions(TestRelayOptions options, string context)
    {
        var errors = new List<string>();

        if (options.DefaultTimeout <= TimeSpan.Zero)
            errors.Add($"{context}: DefaultTimeout must be greater than zero");

        if (options.MaxDegreeOfParallelism < 1)
            errors.Add($"{context}: MaxDegreeOfParallelism must be at least 1");

        if (options.EnableCoverageTracking && options.CoverageTracking.MinimumCoverageThreshold < 0)
            errors.Add($"{context}: Coverage MinimumCoverageThreshold cannot be negative");

        if (options.EnablePerformanceProfiling)
        {
            var perf = options.PerformanceProfiling;
            if (perf.MemoryWarningThreshold < 0)
                errors.Add($"{context}: Performance MemoryWarningThreshold cannot be negative");

            if (perf.ExecutionTimeWarningThreshold < 0)
                errors.Add($"{context}: Performance ExecutionTimeWarningThreshold cannot be negative");
        }

        return errors;
    }

    private string GetCurrentEnvironmentName()
    {
        // Check various environment variables for the current environment
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                         Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ??
                         "Development";

        return environment;
    }

    private TestRelayOptions CloneOptions(TestRelayOptions source)
    {
        // Simple cloning - in a real implementation, you might use serialization or a deep clone library
        return new TestRelayOptions
        {
            DefaultTimeout = source.DefaultTimeout,
            EnableParallelExecution = source.EnableParallelExecution,
            MaxDegreeOfParallelism = source.MaxDegreeOfParallelism,
            EnableIsolation = source.EnableIsolation,
            IsolationLevel = source.IsolationLevel,
            EnableAutoCleanup = source.EnableAutoCleanup,
            EnablePerformanceProfiling = source.EnablePerformanceProfiling,
            PerformanceProfiling = ClonePerformanceProfilingOptions(source.PerformanceProfiling),
            EnableCoverageTracking = source.EnableCoverageTracking,
            CoverageTracking = CloneCoverageTrackingOptions(source.CoverageTracking),
            EnableDiagnosticLogging = source.EnableDiagnosticLogging,
            DiagnosticLogging = CloneDiagnosticLoggingOptions(source.DiagnosticLogging),
            TestData = CloneTestDataOptions(source.TestData),
            Mock = CloneMockOptions(source.Mock),
            Scenario = CloneScenarioOptions(source.Scenario),
            EnvironmentOverrides = new Dictionary<string, TestRelayOptions>(source.EnvironmentOverrides)
        };
    }

    private PerformanceProfilingOptions ClonePerformanceProfilingOptions(PerformanceProfilingOptions source)
    {
        return new PerformanceProfilingOptions
        {
            TrackMemoryUsage = source.TrackMemoryUsage,
            TrackExecutionTime = source.TrackExecutionTime,
            MemoryWarningThreshold = source.MemoryWarningThreshold,
            ExecutionTimeWarningThreshold = source.ExecutionTimeWarningThreshold,
            EnableDetailedProfiling = source.EnableDetailedProfiling
        };
    }

    private CoverageTrackingOptions CloneCoverageTrackingOptions(CoverageTrackingOptions source)
    {
        return new CoverageTrackingOptions
        {
            MinimumCoverageThreshold = source.MinimumCoverageThreshold,
            TrackLineCoverage = source.TrackLineCoverage,
            TrackBranchCoverage = source.TrackBranchCoverage,
            TrackMethodCoverage = source.TrackMethodCoverage,
            ReportFormat = source.ReportFormat,
            GenerateReports = source.GenerateReports,
            ReportOutputDirectory = source.ReportOutputDirectory
        };
    }

    private DiagnosticLoggingOptions CloneDiagnosticLoggingOptions(DiagnosticLoggingOptions source)
    {
        return new DiagnosticLoggingOptions
        {
            LogLevel = source.LogLevel,
            LogTestExecution = source.LogTestExecution,
            LogMockInteractions = source.LogMockInteractions,
            LogPerformanceMetrics = source.LogPerformanceMetrics,
            LogOutputDirectory = source.LogOutputDirectory,
            EnableConsoleLogging = source.EnableConsoleLogging,
            EnableFileLogging = source.EnableFileLogging
        };
    }

    private TestDataOptions CloneTestDataOptions(TestDataOptions source)
    {
        return new TestDataOptions
        {
            EnableAutoSeeding = source.EnableAutoSeeding,
            DataDirectory = source.DataDirectory,
            UseSharedData = source.UseSharedData,
            EnableDataIsolation = source.EnableDataIsolation,
            IsolationStrategy = source.IsolationStrategy
        };
    }

    private MockOptions CloneMockOptions(MockOptions source)
    {
        return new MockOptions
        {
            EnableStrictVerification = source.EnableStrictVerification,
            EnableAutoRegistration = source.EnableAutoRegistration,
            DefaultBehavior = source.DefaultBehavior,
            TrackInvocations = source.TrackInvocations
        };
    }

    private ScenarioOptions CloneScenarioOptions(ScenarioOptions source)
    {
        return new ScenarioOptions
        {
            DefaultTimeout = source.DefaultTimeout,
            EnableRecording = source.EnableRecording,
            RecordingDirectory = source.RecordingDirectory,
            EnableReplay = source.EnableReplay,
            EnableStepValidation = source.EnableStepValidation
        };
    }

    private void MergeOptions(TestRelayOptions target, TestRelayOptions source)
    {
        // Reuse the merging logic from TestRelayOptionsBuilder
        var builder = new TestRelayOptionsBuilder();
        builder.MergeOptions(target, source);
    }
}

/// <summary>
/// Factory for creating test environment configurations.
/// </summary>
public static class TestEnvironmentConfigurationFactory
{
    /// <summary>
    /// Creates a default test environment configuration.
    /// </summary>
    /// <returns>A new test environment configuration with default settings.</returns>
    public static TestEnvironmentConfiguration CreateDefault()
    {
        var defaultOptions = new TestRelayOptionsBuilder()
            .WithDefaultTimeout(TimeSpan.FromSeconds(30))
            .WithParallelExecution(true)
            .WithIsolation(true, IsolationLevel.DatabaseTransaction)
            .WithAutoCleanup(true)
            .Build();

        var config = new TestEnvironmentConfiguration(defaultOptions);

        // Add common environment configurations
        config.AddEnvironmentConfig("Development", builder => builder.ForDevelopment());
        config.AddEnvironmentConfig("CI", builder => builder.ForCI());
        config.AddEnvironmentConfig("Performance", builder => builder.ForPerformanceTesting());

        return config;
    }

    /// <summary>
    /// Creates a test environment configuration from a configuration file.
    /// </summary>
    /// <param name="configFilePath">The path to the configuration file.</param>
    /// <returns>A test environment configuration loaded from the file.</returns>
    public static TestEnvironmentConfiguration FromFile(string configFilePath)
    {
        var defaultOptions = new TestRelayOptionsBuilder().Build();
        var config = new TestEnvironmentConfiguration(defaultOptions);

        if (File.Exists(configFilePath))
        {
            // In a real implementation, load from file
            // config = JsonSerializer.Deserialize<TestEnvironmentConfiguration>(File.ReadAllText(configFilePath));
        }

        return config;
    }

    /// <summary>
    /// Creates a test environment configuration from environment variables.
    /// </summary>
    /// <param name="prefix">The environment variable prefix.</param>
    /// <returns>A test environment configuration configured from environment variables.</returns>
    public static TestEnvironmentConfiguration FromEnvironment(string prefix = "TESTRELAY_")
    {
        var builder = new TestRelayOptionsBuilder().LoadFromEnvironment(prefix);
        var defaultOptions = builder.Build();

        return new TestEnvironmentConfiguration(defaultOptions);
    }
}