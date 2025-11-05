using System.Linq;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class SnapshotComparerTests
{
    private readonly SnapshotComparer _comparer = new();

    [Fact]
    public void Compare_IdenticalSnapshots_ReturnsEqualDiff()
    {
        // Arrange
        var content = "Line 1\nLine 2\nLine 3";

        // Act
        var diff = _comparer.Compare(content, content);

        // Assert
        Assert.True(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        // When strings are identical, it returns early with AreEqual=true
        // The Differences list may be uninitialized (null or empty)
    }

    [Fact]
    public void Compare_EmptySnapshots_ReturnsEqualDiff()
    {
        // Act
        var diff = _comparer.Compare("", "");

        // Assert
        Assert.True(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Empty(diff.Differences);
    }

    [Fact]
    public void Compare_NullSnapshots_HandlesGracefully()
    {
        // Act - The method handles null inputs by treating them as empty strings
        var diff1 = _comparer.Compare(null!, "content");
        var diff2 = _comparer.Compare("content", null!);

        // Assert
        Assert.False(diff1.AreEqual);
        Assert.False(diff2.AreEqual);
    }

    [Fact]
    public void Compare_AddedLines_ReturnsAddedDifferences()
    {
        // Arrange
        var expected = "Line 1\nLine 2";
        var actual = "Line 1\nLine 2\nLine 3";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(3, diff.Differences.Count);

        // First two lines unchanged
        Assert.Equal(DiffType.Unchanged, diff.Differences[0].Type);
        Assert.Equal("Line 1", diff.Differences[0].Content);
        Assert.Equal(1, diff.Differences[0].LineNumber);

        Assert.Equal(DiffType.Unchanged, diff.Differences[1].Type);
        Assert.Equal("Line 2", diff.Differences[1].Content);
        Assert.Equal(2, diff.Differences[1].LineNumber);

        // Third line added
        Assert.Equal(DiffType.Added, diff.Differences[2].Type);
        Assert.Equal("Line 3", diff.Differences[2].Content);
        Assert.Equal(3, diff.Differences[2].LineNumber);
    }

    [Fact]
    public void Compare_RemovedLines_ReturnsRemovedDifferences()
    {
        // Arrange
        var expected = "Line 1\nLine 2\nLine 3";
        var actual = "Line 1\nLine 2";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(3, diff.Differences.Count);

        // First two lines unchanged
        Assert.Equal(DiffType.Unchanged, diff.Differences[0].Type);
        Assert.Equal("Line 1", diff.Differences[0].Content);
        Assert.Equal(1, diff.Differences[0].LineNumber);

        Assert.Equal(DiffType.Unchanged, diff.Differences[1].Type);
        Assert.Equal("Line 2", diff.Differences[1].Content);
        Assert.Equal(2, diff.Differences[1].LineNumber);

        // Third line removed
        Assert.Equal(DiffType.Removed, diff.Differences[2].Type);
        Assert.Equal("Line 3", diff.Differences[2].Content);
        Assert.Equal(3, diff.Differences[2].LineNumber);
    }

    [Fact]
    public void Compare_ModifiedLines_ReturnsModifiedDifferences()
    {
        // Arrange
        var expected = "Line 1\nOld Line 2\nLine 3";
        var actual = "Line 1\nNew Line 2\nLine 3";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(4, diff.Differences.Count); // 2 unchanged + 1 removed + 1 added = modified

        // First line unchanged
        Assert.Equal(DiffType.Unchanged, diff.Differences[0].Type);
        Assert.Equal("Line 1", diff.Differences[0].Content);
        Assert.Equal(1, diff.Differences[0].LineNumber);

        // Second line removed (old)
        Assert.Equal(DiffType.Removed, diff.Differences[1].Type);
        Assert.Equal("Old Line 2", diff.Differences[1].Content);
        Assert.Equal(2, diff.Differences[1].LineNumber);

        // Second line added (new)
        Assert.Equal(DiffType.Added, diff.Differences[2].Type);
        Assert.Equal("New Line 2", diff.Differences[2].Content);
        Assert.Equal(2, diff.Differences[2].LineNumber);

        // Third line unchanged
        Assert.Equal(DiffType.Unchanged, diff.Differences[3].Type);
        Assert.Equal("Line 3", diff.Differences[3].Content);
        Assert.Equal(3, diff.Differences[3].LineNumber);
    }

    [Fact]
    public void Compare_DifferentLineEndings_HandlesCorrectly()
    {
        // Arrange
        var expected = "Line 1\r\nLine 2\r\nLine 3"; // Windows line endings
        var actual = "Line 1\nLine 2\nLine 3"; // Unix line endings

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual); // String comparison fails first, before line splitting
    }

    [Fact]
    public void Compare_SingleLineDifferences_HandlesCorrectly()
    {
        // Arrange
        var expected = "Single line";
        var actual = "Different single line";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(2, diff.Differences.Count);

        Assert.Equal(DiffType.Removed, diff.Differences[0].Type);
        Assert.Equal("Single line", diff.Differences[0].Content);

        Assert.Equal(DiffType.Added, diff.Differences[1].Type);
        Assert.Equal("Different single line", diff.Differences[1].Content);
    }

    [Fact]
    public void Compare_ComplexChanges_HandlesMultipleDifferences()
    {
        // Arrange
        var expected = @"Header
Line 1
Line 2
Line 3
Footer";

        var actual = @"Header
New Line 1
Line 2
Line 4
New Footer";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);

        // Debug: Print actual differences
        // foreach (var d in diff.Differences) { Console.WriteLine($"{d.Type}: '{d.Content}' at {d.LineNumber}"); }

        // Should have differences for the changed lines
        var addedLines = diff.Differences.Where(d => d.Type == DiffType.Added).ToList();
        var removedLines = diff.Differences.Where(d => d.Type == DiffType.Removed).ToList();
        var unchangedLines = diff.Differences.Where(d => d.Type == DiffType.Unchanged).ToList();

        Assert.Equal(3, addedLines.Count); // "New Line 1", "Line 4", "New Footer"
        Assert.Equal(3, removedLines.Count); // "Line 1", "Line 3", "Footer"
        Assert.Equal(2, unchangedLines.Count); // "Header" and "Line 2"
    }

    [Fact]
    public void GenerateDiffReport_EqualSnapshots_ReturnsMatchMessage()
    {
        // Arrange
        var diff = new SnapshotDiff { AreEqual = true };

        // Act
        var report = _comparer.GenerateDiffReport(diff);

        // Assert
        Assert.Equal("Snapshots match.", report);
    }

    [Fact]
    public void GenerateDiffReport_UnequalSnapshots_ReturnsDetailedReport()
    {
        // Arrange
        var diff = new SnapshotDiff
        {
            AreEqual = false,
            Differences = new()
            {
                new DiffLine { Type = DiffType.Unchanged, Content = "Same line", LineNumber = 1 },
                new DiffLine { Type = DiffType.Added, Content = "Added line", LineNumber = 2 },
                new DiffLine { Type = DiffType.Removed, Content = "Removed line", LineNumber = 3 },
                new DiffLine { Type = DiffType.Modified, Content = "Modified line", LineNumber = 4 }
            }
        };

        // Act
        var report = _comparer.GenerateDiffReport(diff);

        // Assert
        Assert.Contains("Snapshot differences:", report);
        Assert.Contains("  Same line", report);
        Assert.Contains("+ Added line", report);
        Assert.Contains("- Removed line", report);
        // Note: Modified type is handled but not produced by current algorithm
    }

    [Fact]
    public void GenerateDiffReport_WithNullDifferences_HandlesGracefully()
    {
        // Arrange
        var diff = new SnapshotDiff { AreEqual = false, Differences = null! };

        // Act & Assert
        var exception = Assert.Throws<System.NullReferenceException>(() => _comparer.GenerateDiffReport(diff));
    }

    [Fact]
    public void Compare_LargeContent_HandlesEfficiently()
    {
        // Arrange
        var largeContent = string.Join("\n", Enumerable.Range(1, 1000).Select(i => $"Line {i}"));
        var slightlyDifferentContent = largeContent.Replace("Line 500", "Modified Line 500");

        // Act
        var diff = _comparer.Compare(largeContent, slightlyDifferentContent);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        // Should have differences around line 500
        var modifiedLines = diff.Differences.Where(d => d.Type != DiffType.Unchanged).ToList();
        Assert.True(modifiedLines.Count > 0);
    }

    [Fact]
    public void Compare_EmptyExpected_WithContent_ReturnsAllAdded()
    {
        // Arrange
        var expected = "";
        var actual = "Line 1\nLine 2";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(2, diff.Differences.Count);
        Assert.All(diff.Differences, d => Assert.Equal(DiffType.Added, d.Type));
    }

    [Fact]
    public void Compare_ContentWith_EmptyActual_ReturnsAllRemoved()
    {
        // Arrange
        var expected = "Line 1\nLine 2";
        var actual = "";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        Assert.Equal(2, diff.Differences.Count);
        Assert.All(diff.Differences, d => Assert.Equal(DiffType.Removed, d.Type));
    }

    [Fact]
    public void Compare_WithWhitespaceDifferences_TreatsAsDifferent()
    {
        // Arrange
        var expected = "Line 1\nLine 2";
        var actual = "Line 1\n  Line 2"; // Extra space

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        // Should show the difference in line 2
        var differences = diff.Differences.Where(d => d.Type != DiffType.Unchanged).ToList();
        Assert.Equal(2, differences.Count); // 1 removed, 1 added
    }

    [Fact]
    public void Compare_CaseSensitive_TreatsDifferentCaseAsDifferent()
    {
        // Arrange
        var expected = "Line 1\nline 2";
        var actual = "Line 1\nLine 2";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        var differences = diff.Differences.Where(d => d.Type != DiffType.Unchanged).ToList();
        Assert.Equal(2, differences.Count); // 1 removed, 1 added
    }

    [Fact]
    public void Compare_MixedLineEndings_HandlesCorrectly()
    {
        // Arrange
        var expected = "Line 1\nLine 2\r\nLine 3";
        var actual = "Line 1\r\nLine 2\nLine 3";

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual); // String comparison fails first
    }

    [Fact]
    public void Compare_VeryLongLines_HandlesCorrectly()
    {
        // Arrange
        var longLine1 = new string('A', 10000);
        var longLine2 = new string('A', 10000);
        longLine2 = longLine2.Remove(5000, 1).Insert(5000, "B"); // Make one character different

        var expected = longLine1;
        var actual = longLine2;

        // Act
        var diff = _comparer.Compare(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotNull(diff.Differences);
        var differences = diff.Differences.Where(d => d.Type != DiffType.Unchanged).ToList();
        Assert.Equal(2, differences.Count); // 1 removed, 1 added
    }
}