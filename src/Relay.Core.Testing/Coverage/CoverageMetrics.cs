using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Represents coverage metrics for a test run or code element.
/// </summary>
public class CoverageMetrics
{
    /// <summary>
    /// Gets or sets the total number of lines in the code.
    /// </summary>
    public int TotalLines { get; set; }

    /// <summary>
    /// Gets or sets the number of lines that were executed during testing.
    /// </summary>
    public int CoveredLines { get; set; }

    /// <summary>
    /// Gets or sets the total number of branches in the code.
    /// </summary>
    public int TotalBranches { get; set; }

    /// <summary>
    /// Gets or sets the number of branches that were executed during testing.
    /// </summary>
    public int CoveredBranches { get; set; }

    /// <summary>
    /// Gets or sets the total number of methods in the code.
    /// </summary>
    public int TotalMethods { get; set; }

    /// <summary>
    /// Gets or sets the number of methods that were executed during testing.
    /// </summary>
    public int CoveredMethods { get; set; }

    /// <summary>
    /// Gets the line coverage percentage (0-100).
    /// </summary>
    public double LineCoveragePercentage => TotalLines > 0 ? (double)CoveredLines / TotalLines * 100 : 0;

    /// <summary>
    /// Gets the branch coverage percentage (0-100).
    /// </summary>
    public double BranchCoveragePercentage => TotalBranches > 0 ? (double)CoveredBranches / TotalBranches * 100 : 0;

    /// <summary>
    /// Gets the method coverage percentage (0-100).
    /// </summary>
    public double MethodCoveragePercentage => TotalMethods > 0 ? (double)CoveredMethods / TotalMethods * 100 : 0;

    /// <summary>
    /// Gets or sets the list of uncovered lines.
    /// </summary>
    public List<int> UncoveredLines { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of uncovered branches.
    /// </summary>
    public List<int> UncoveredBranches { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of uncovered methods.
    /// </summary>
    public List<string> UncoveredMethods { get; set; } = new();

    /// <summary>
    /// Merges another CoverageMetrics instance into this one.
    /// </summary>
    /// <param name="other">The other coverage metrics to merge.</param>
    public void Merge(CoverageMetrics other)
    {
        if (other == null) return;

        TotalLines += other.TotalLines;
        CoveredLines += other.CoveredLines;
        TotalBranches += other.TotalBranches;
        CoveredBranches += other.CoveredBranches;
        TotalMethods += other.TotalMethods;
        CoveredMethods += other.CoveredMethods;

        UncoveredLines.AddRange(other.UncoveredLines);
        UncoveredBranches.AddRange(other.UncoveredBranches);
        UncoveredMethods.AddRange(other.UncoveredMethods);
    }

    /// <summary>
    /// Creates a copy of this CoverageMetrics instance.
    /// </summary>
    /// <returns>A copy of this instance.</returns>
    public CoverageMetrics Clone()
    {
        return new CoverageMetrics
        {
            TotalLines = TotalLines,
            CoveredLines = CoveredLines,
            TotalBranches = TotalBranches,
            CoveredBranches = CoveredBranches,
            TotalMethods = TotalMethods,
            CoveredMethods = CoveredMethods,
            UncoveredLines = new List<int>(UncoveredLines),
            UncoveredBranches = new List<int>(UncoveredBranches),
            UncoveredMethods = new List<string>(UncoveredMethods)
        };
    }
}