using System;
using Relay.Core;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Test-specific handle attribute that allows test-only handler registration
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestHandleAttribute : Attribute
{
    /// <summary>
    /// Optional name for the handler
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Priority for handler execution order
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this handler should be used in integration tests
    /// </summary>
    public bool IntegrationTest { get; set; } = false;

    /// <summary>
    /// Expected exception type for negative testing
    /// </summary>
    public Type? ExpectedException { get; set; }
}

/// <summary>
/// Test-specific notification attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestNotificationAttribute : Attribute
{
    /// <summary>
    /// Dispatch mode for the notification
    /// </summary>
    public NotificationDispatchMode DispatchMode { get; set; } = NotificationDispatchMode.Parallel;

    /// <summary>
    /// Priority for handler execution order
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Whether this handler should simulate failure
    /// </summary>
    public bool SimulateFailure { get; set; } = false;
}

/// <summary>
/// Test-specific pipeline attribute
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestPipelineAttribute : Attribute
{
    /// <summary>
    /// Order of pipeline execution
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// Scope of the pipeline
    /// </summary>
    public PipelineScope Scope { get; set; } = PipelineScope.All;

    /// <summary>
    /// Whether this pipeline should modify requests/responses
    /// </summary>
    public bool ModifyData { get; set; } = false;

    /// <summary>
    /// Whether this pipeline should simulate failure
    /// </summary>
    public bool SimulateFailure { get; set; } = false;
}

/// <summary>
/// Attribute to mark test methods that should be skipped in certain environments
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class SkipInEnvironmentAttribute : Attribute
{
    public string Environment { get; }
    public string Reason { get; }

    public SkipInEnvironmentAttribute(string environment, string reason)
    {
        Environment = environment;
        Reason = reason;
    }
}

/// <summary>
/// Attribute to mark performance-sensitive tests
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PerformanceTestAttribute : Attribute
{
    /// <summary>
    /// Maximum allowed execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; set; } = 1000;

    /// <summary>
    /// Maximum allowed memory allocation in bytes
    /// </summary>
    public long MaxAllocationBytes { get; set; } = 1024 * 1024; // 1MB default

    /// <summary>
    /// Whether to run this test in release mode only
    /// </summary>
    public bool ReleaseModeOnly { get; set; } = false;
}

/// <summary>
/// Attribute to mark integration tests
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IntegrationTestAttribute : Attribute
{
    /// <summary>
    /// Category of integration test
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Whether this test requires external dependencies
    /// </summary>
    public bool RequiresExternalDependencies { get; set; } = false;
}