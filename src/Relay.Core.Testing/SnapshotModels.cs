using System.Collections.Generic;

namespace Relay.Core.Testing;

/// <summary>
/// Represents the result of a snapshot comparison operation.
/// </summary>
public class SnapshotResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the snapshot matched.
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// Gets or sets the path to the snapshot file.
    /// </summary>
    public string SnapshotPath { get; set; }

    /// <summary>
    /// Gets or sets the diff information if the snapshot didn't match.
    /// </summary>
    public SnapshotDiff Diff { get; set; }
}

/// <summary>
/// Represents the differences between two snapshots.
/// </summary>
public class SnapshotDiff
{
    /// <summary>
    /// Gets or sets a value indicating whether the snapshots are equal.
    /// </summary>
    public bool AreEqual { get; set; }

    /// <summary>
    /// Gets or sets the list of differences.
    /// </summary>
    public List<DiffLine> Differences { get; set; } = new();
}

/// <summary>
/// Represents a single line in a diff.
/// </summary>
public class DiffLine
{
    /// <summary>
    /// Gets or sets the type of difference.
    /// </summary>
    public DiffType Type { get; set; }

    /// <summary>
    /// Gets or sets the content of the line.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Gets or sets the line number in the original content.
    /// </summary>
    public int LineNumber { get; set; }
}

/// <summary>
/// Defines the types of differences that can occur in a diff.
/// </summary>
public enum DiffType
{
    /// <summary>
    /// The line is unchanged.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The line was added.
    /// </summary>
    Added,

    /// <summary>
    /// The line was removed.
    /// </summary>
    Removed,

    /// <summary>
    /// The line was modified.
    /// </summary>
    Modified
}