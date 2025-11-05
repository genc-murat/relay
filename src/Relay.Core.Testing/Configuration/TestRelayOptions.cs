using System;
using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Configuration options for TestRelay testing framework.
/// </summary>
public class TestRelayOptions
{
    /// <summary>
    /// Gets or sets the default timeout for test operations.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets whether to enable parallel test execution.
    /// </summary>
    public bool EnableParallelExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for test execution.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// Gets or sets whether to enable test isolation.
    /// </summary>
    public bool EnableIsolation { get; set; } = true;

    /// <summary>
    /// Gets or sets the isolation level for tests.
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.DatabaseTransaction;

    /// <summary>
    /// Gets or sets whether to enable automatic cleanup.
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable performance profiling.
    /// </summary>
    public bool EnablePerformanceProfiling { get; set; } = false;

    /// <summary>
    /// Gets or sets the performance profiling options.
    /// </summary>
    public PerformanceProfilingOptions PerformanceProfiling { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable coverage tracking.
    /// </summary>
    public bool EnableCoverageTracking { get; set; } = false;

    /// <summary>
    /// Gets or sets the coverage tracking options.
    /// </summary>
    public CoverageTrackingOptions CoverageTracking { get; set; } = new();

    /// <summary>
    /// Gets or sets whether to enable diagnostic logging.
    /// </summary>
    public bool EnableDiagnosticLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the diagnostic logging options.
    /// </summary>
    public DiagnosticLoggingOptions DiagnosticLogging { get; set; } = new();

    /// <summary>
    /// Gets or sets the test data options.
    /// </summary>
    public TestDataOptions TestData { get; set; } = new();

    /// <summary>
    /// Gets or sets the mock options.
    /// </summary>
    public MockOptions Mock { get; set; } = new();

    /// <summary>
    /// Gets or sets the scenario options.
    /// </summary>
    public ScenarioOptions Scenario { get; set; } = new();

    /// <summary>
    /// Gets or sets environment-specific overrides.
    /// </summary>
    public Dictionary<string, TestRelayOptions> EnvironmentOverrides { get; set; } = new();
}

/// <summary>
/// Options for performance profiling.
/// </summary>
public class PerformanceProfilingOptions
{
    /// <summary>
    /// Gets or sets whether to track memory usage.
    /// </summary>
    public bool TrackMemoryUsage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track execution time.
    /// </summary>
    public bool TrackExecutionTime { get; set; } = true;

    /// <summary>
    /// Gets or sets the memory threshold for warnings (in MB).
    /// </summary>
    public long MemoryWarningThreshold { get; set; } = 100;

    /// <summary>
    /// Gets or sets the execution time threshold for warnings (in milliseconds).
    /// </summary>
    public long ExecutionTimeWarningThreshold { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to enable detailed profiling.
    /// </summary>
    public bool EnableDetailedProfiling { get; set; } = false;
}

/// <summary>
    /// Options for coverage tracking.
/// </summary>
public class CoverageTrackingOptions
{
    /// <summary>
    /// Gets or sets the minimum coverage threshold.
    /// </summary>
    public double MinimumCoverageThreshold { get; set; } = 80.0;

    /// <summary>
    /// Gets or sets whether to track line coverage.
    /// </summary>
    public bool TrackLineCoverage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track branch coverage.
    /// </summary>
    public bool TrackBranchCoverage { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track method coverage.
    /// </summary>
    public bool TrackMethodCoverage { get; set; } = true;

    /// <summary>
    /// Gets or sets the coverage report format.
    /// </summary>
    public CoverageReportFormat ReportFormat { get; set; } = CoverageReportFormat.Json;

    /// <summary>
    /// Gets or sets whether to generate coverage reports.
    /// </summary>
    public bool GenerateReports { get; set; } = true;

    /// <summary>
    /// Gets or sets the output directory for coverage reports.
    /// </summary>
    public string ReportOutputDirectory { get; set; } = "TestCoverageReports";
}

/// <summary>
    /// Options for diagnostic logging.
/// </summary>
public class DiagnosticLoggingOptions
{
    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets whether to log test execution details.
    /// </summary>
    public bool LogTestExecution { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to log mock interactions.
    /// </summary>
    public bool LogMockInteractions { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to log performance metrics.
    /// </summary>
    public bool LogPerformanceMetrics { get; set; } = false;

    /// <summary>
    /// Gets or sets the log output directory.
    /// </summary>
    public string LogOutputDirectory { get; set; } = "TestLogs";

    /// <summary>
    /// Gets or sets whether to enable console logging.
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable file logging.
    /// </summary>
    public bool EnableFileLogging { get; set; } = false;
}

/// <summary>
    /// Options for test data management.
/// </summary>
public class TestDataOptions
{
    /// <summary>
    /// Gets or sets whether to enable automatic test data seeding.
    /// </summary>
    public bool EnableAutoSeeding { get; set; } = false;

    /// <summary>
    /// Gets or sets the test data directory.
    /// </summary>
    public string DataDirectory { get; set; } = "TestData";

    /// <summary>
    /// Gets or sets whether to use shared test data.
    /// </summary>
    public bool UseSharedData { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable data isolation.
    /// </summary>
    public bool EnableDataIsolation { get; set; } = true;

    /// <summary>
    /// Gets or sets the data isolation strategy.
    /// </summary>
    public DataIsolationStrategy IsolationStrategy { get; set; } = DataIsolationStrategy.DatabaseTransaction;
}

/// <summary>
    /// Options for mock configuration.
/// </summary>
public class MockOptions
{
    /// <summary>
    /// Gets or sets whether to enable strict mock verification.
    /// </summary>
    public bool EnableStrictVerification { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable automatic mock registration.
    /// </summary>
    public bool EnableAutoRegistration { get; set; } = true;

    /// <summary>
    /// Gets or sets the default mock behavior.
    /// </summary>
    public MockBehavior DefaultBehavior { get; set; } = MockBehavior.Loose;

    /// <summary>
    /// Gets or sets whether to track mock invocations.
    /// </summary>
    public bool TrackInvocations { get; set; } = true;
}

/// <summary>
    /// Options for scenario configuration.
/// </summary>
public class ScenarioOptions
{
    /// <summary>
    /// Gets or sets the default scenario timeout.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets whether to enable scenario recording.
    /// </summary>
    public bool EnableRecording { get; set; } = false;

    /// <summary>
    /// Gets or sets the scenario recording directory.
    /// </summary>
    public string RecordingDirectory { get; set; } = "ScenarioRecordings";

    /// <summary>
    /// Gets or sets whether to enable scenario replay.
    /// </summary>
    public bool EnableReplay { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable step validation.
    /// </summary>
    public bool EnableStepValidation { get; set; } = true;
}

/// <summary>
/// Defines coverage report formats.
/// </summary>
public enum CoverageReportFormat
{
    /// <summary>
    /// JSON format.
    /// </summary>
    Json,

    /// <summary>
    /// XML format.
    /// </summary>
    Xml,

    /// <summary>
    /// HTML format.
    /// </summary>
    Html,

    /// <summary>
    /// Cobertura format.
    /// </summary>
    Cobertura
}

/// <summary>
/// Defines log levels.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level.
    /// </summary>
    Trace,

    /// <summary>
    /// Debug level.
    /// </summary>
    Debug,

    /// <summary>
    /// Information level.
    /// </summary>
    Information,

    /// <summary>
    /// Warning level.
    /// </summary>
    Warning,

    /// <summary>
    /// Error level.
    /// </summary>
    Error,

    /// <summary>
    /// Critical level.
    /// </summary>
    Critical
}

/// <summary>
/// Defines data isolation strategies.
/// </summary>
public enum DataIsolationStrategy
{
    /// <summary>
    /// No isolation.
    /// </summary>
    None,

    /// <summary>
    /// Memory isolation.
    /// </summary>
    Memory,

    /// <summary>
    /// Database transaction isolation.
    /// </summary>
    DatabaseTransaction,

    /// <summary>
    /// Full isolation including file system.
    /// </summary>
    FullIsolation
}

/// <summary>
/// Defines mock behaviors.
/// </summary>
public enum MockBehavior
{
    /// <summary>
    /// Loose behavior - allows unconfigured calls.
    /// </summary>
    Loose,

    /// <summary>
    /// Strict behavior - throws on unconfigured calls.
    /// </summary>
    Strict
}