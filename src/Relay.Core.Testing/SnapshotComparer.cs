using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Testing;

/// <summary>
/// Compares snapshots and generates detailed diffs.
/// </summary>
public class SnapshotComparer
{
    /// <summary>
    /// Compares two snapshots and returns the differences.
    /// </summary>
    /// <param name="expected">The expected snapshot content.</param>
    /// <param name="actual">The actual snapshot content.</param>
    /// <returns>The snapshot diff containing the differences.</returns>
    public SnapshotDiff Compare(string expected, string actual)
    {
        if (expected == actual)
        {
            return new SnapshotDiff { AreEqual = true };
        }

        var expectedLines = SplitIntoLines(expected);
        var actualLines = SplitIntoLines(actual);

        var differences = ComputeDiff(expectedLines, actualLines);

        return new SnapshotDiff
        {
            AreEqual = false,
            Differences = differences
        };
    }

    /// <summary>
    /// Generates a human-readable diff report.
    /// </summary>
    /// <param name="diff">The snapshot diff to generate the report for.</param>
    /// <returns>The diff report as a string.</returns>
    public string GenerateDiffReport(SnapshotDiff diff)
    {
        if (diff.AreEqual)
        {
            return "Snapshots match.";
        }

        var report = new System.Text.StringBuilder();
        report.AppendLine("Snapshot differences:");
        report.AppendLine();

        foreach (var difference in diff.Differences)
        {
            var prefix = difference.Type switch
            {
                DiffType.Added => "+ ",
                DiffType.Removed => "- ",
                DiffType.Modified => "~ ",
                DiffType.Unchanged => "  ",
                _ => "? "
            };

            report.AppendLine($"{prefix}{difference.Content}");
        }

        return report.ToString();
    }

    private static List<string> SplitIntoLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return new List<string>();
        }

        return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
                     .ToList();
    }

    private static List<DiffLine> ComputeDiff(List<string> expected, List<string> actual)
    {
        var differences = new List<DiffLine>();

        // Simple line-by-line comparison for now
        // A full Myers diff implementation would be more complex
        var maxLines = Math.Max(expected.Count, actual.Count);
        var lineNumber = 1;

        for (int i = 0; i < maxLines; i++)
        {
            var expectedLine = i < expected.Count ? expected[i] : null;
            var actualLine = i < actual.Count ? actual[i] : null;

            if (expectedLine == null && actualLine != null)
            {
                // Line added
                differences.Add(new DiffLine
                {
                    Type = DiffType.Added,
                    Content = actualLine,
                    LineNumber = lineNumber
                });
            }
            else if (expectedLine != null && actualLine == null)
            {
                // Line removed
                differences.Add(new DiffLine
                {
                    Type = DiffType.Removed,
                    Content = expectedLine,
                    LineNumber = lineNumber
                });
                lineNumber++;
            }
            else if (expectedLine != actualLine)
            {
                // Line modified
                differences.Add(new DiffLine
                {
                    Type = DiffType.Removed,
                    Content = expectedLine,
                    LineNumber = lineNumber
                });
                differences.Add(new DiffLine
                {
                    Type = DiffType.Added,
                    Content = actualLine,
                    LineNumber = lineNumber
                });
                lineNumber++;
            }
            else
            {
                // Line unchanged
                differences.Add(new DiffLine
                {
                    Type = DiffType.Unchanged,
                    Content = expectedLine,
                    LineNumber = lineNumber
                });
                lineNumber++;
            }
        }

        return differences;
    }
}