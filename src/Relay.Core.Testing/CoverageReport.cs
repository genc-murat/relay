using System;
using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a test coverage report.
/// </summary>
public class CoverageReport
{
    /// <summary>
    /// Gets or sets the name of the report.
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the overall coverage metrics.
    /// </summary>
    public CoverageMetrics OverallMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the coverage metrics by assembly.
    /// </summary>
    public Dictionary<string, CoverageMetrics> AssemblyMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the coverage metrics by class.
    /// </summary>
    public Dictionary<string, CoverageMetrics> ClassMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the coverage metrics by method.
    /// </summary>
    public Dictionary<string, CoverageMetrics> MethodMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of test scenarios that were executed.
    /// </summary>
    public List<string> TestScenarios { get; set; } = new();

    /// <summary>
    /// Gets or sets the minimum acceptable coverage percentage.
    /// </summary>
    public double MinimumCoverageThreshold { get; set; } = 80.0;

    /// <summary>
    /// Gets a value indicating whether the coverage meets the minimum threshold.
    /// </summary>
    public bool MeetsThreshold => OverallMetrics.LineCoveragePercentage >= MinimumCoverageThreshold;

    /// <summary>
    /// Gets or sets additional metadata for the report.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Adds coverage metrics for a specific assembly.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <param name="metrics">The coverage metrics.</param>
    public void AddAssemblyMetrics(string assemblyName, CoverageMetrics metrics)
    {
        if (string.IsNullOrWhiteSpace(assemblyName)) throw new ArgumentException("Assembly name cannot be null or empty", nameof(assemblyName));
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));

        AssemblyMetrics[assemblyName] = metrics;
        OverallMetrics.Merge(metrics);
    }

    /// <summary>
    /// Adds coverage metrics for a specific class.
    /// </summary>
    /// <param name="className">The name of the class.</param>
    /// <param name="metrics">The coverage metrics.</param>
    public void AddClassMetrics(string className, CoverageMetrics metrics)
    {
        if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException("Class name cannot be null or empty", nameof(className));
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));

        ClassMetrics[className] = metrics;
    }

    /// <summary>
    /// Adds coverage metrics for a specific method.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <param name="metrics">The coverage metrics.</param>
    public void AddMethodMetrics(string methodName, CoverageMetrics metrics)
    {
        if (string.IsNullOrWhiteSpace(methodName)) throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        if (metrics == null) throw new ArgumentNullException(nameof(metrics));

        MethodMetrics[methodName] = metrics;
    }

    /// <summary>
    /// Adds a test scenario to the report.
    /// </summary>
    /// <param name="scenarioName">The name of the test scenario.</param>
    public void AddTestScenario(string scenarioName)
    {
        if (string.IsNullOrWhiteSpace(scenarioName)) throw new ArgumentException("Scenario name cannot be null or empty", nameof(scenarioName));

        if (!TestScenarios.Contains(scenarioName))
        {
            TestScenarios.Add(scenarioName);
        }
    }

    /// <summary>
    /// Gets the coverage metrics for a specific assembly.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly.</param>
    /// <returns>The coverage metrics, or null if not found.</returns>
    public CoverageMetrics? GetAssemblyMetrics(string assemblyName)
    {
        return AssemblyMetrics.TryGetValue(assemblyName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets the coverage metrics for a specific class.
    /// </summary>
    /// <param name="className">The name of the class.</param>
    /// <returns>The coverage metrics, or null if not found.</returns>
    public CoverageMetrics? GetClassMetrics(string className)
    {
        return ClassMetrics.TryGetValue(className, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets the coverage metrics for a specific method.
    /// </summary>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>The coverage metrics, or null if not found.</returns>
    public CoverageMetrics? GetMethodMetrics(string methodName)
    {
        return MethodMetrics.TryGetValue(methodName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Generates a summary string of the coverage report.
    /// </summary>
    /// <returns>A summary string.</returns>
    public string GenerateSummary()
    {
        var summary = $"Coverage Report: {ReportName}\n";
        summary += $"Generated: {GeneratedAt}\n";
        summary += $"Test Scenarios: {TestScenarios.Count}\n";
        summary += $"Overall Coverage:\n";
        summary += $"  Line Coverage: {OverallMetrics.LineCoveragePercentage:F2}%\n";
        summary += $"  Branch Coverage: {OverallMetrics.BranchCoveragePercentage:F2}%\n";
        summary += $"  Method Coverage: {OverallMetrics.MethodCoveragePercentage:F2}%\n";
        summary += $"Meets Threshold ({MinimumCoverageThreshold}%): {MeetsThreshold}\n";

        return summary;
    }
}