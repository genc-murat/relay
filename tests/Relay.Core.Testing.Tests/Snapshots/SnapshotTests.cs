using System;
using System.IO;
using System.Threading.Tasks;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class SnapshotTests : IDisposable
{
    private readonly string _testDirectory;

    public SnapshotTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "RelaySnapshotTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task MatchSnapshotAsync_CreatesSnapshot_WhenNotExists()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _testDirectory };
        var manager = new SnapshotManager(options);
        var testData = new { Name = "Test", Value = 42 };

        // Act
        var result = await manager.MatchSnapshotAsync(testData, "test_snapshot");

        // Assert
        Assert.True(result.Matched);
        Assert.Contains("test_snapshot.json", result.SnapshotPath);
        Assert.True(File.Exists(result.SnapshotPath));
    }

    [Fact]
    public async Task MatchSnapshotAsync_MatchesSnapshot_WhenExistsAndMatches()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _testDirectory };
        var manager = new SnapshotManager(options);
        var testData = new { Name = "Test", Value = 42 };

        // Create initial snapshot
        await manager.MatchSnapshotAsync(testData, "matching_test");

        // Act - match against the same data
        var result = await manager.MatchSnapshotAsync(testData, "matching_test");

        // Assert
        Assert.True(result.Matched);
        Assert.Contains("matching_test.json", result.SnapshotPath);
    }

    [Fact]
    public async Task MatchSnapshotAsync_Fails_WhenSnapshotDiffers()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _testDirectory };
        var manager = new SnapshotManager(options);
        var originalData = new { Name = "Test", Value = 42 };
        var differentData = new { Name = "Test", Value = 43 };

        // Create initial snapshot
        await manager.MatchSnapshotAsync(originalData, "diff_test");

        // Act - match against different data
        var result = await manager.MatchSnapshotAsync(differentData, "diff_test");

        // Assert
        Assert.False(result.Matched);
        Assert.NotNull(result.Diff);
        Assert.False(result.Diff.AreEqual);
        Assert.NotEmpty(result.Diff.Differences);
    }

    [Fact]
    public async Task MatchSnapshotAsync_UpdatesSnapshot_WhenUpdateModeEnabled()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _testDirectory,
            UpdateSnapshots = true
        };
        var manager = new SnapshotManager(options);
        var originalData = new { Name = "Test", Value = 42 };
        var updatedData = new { Name = "Updated", Value = 100 };

        // Create initial snapshot
        await manager.MatchSnapshotAsync(originalData, "update_test");

        // Act - update with different data
        var result = await manager.MatchSnapshotAsync(updatedData, "update_test");

        // Assert
        Assert.True(result.Matched); // Should match because update mode is enabled
        Assert.Contains("update_test.json", result.SnapshotPath);
    }

    [Fact]
    public void CompareSnapshots_ReturnsDiff_WhenSnapshotsDiffer()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _testDirectory };
        var manager = new SnapshotManager(options);
        var expected = "{\"name\":\"Test\",\"value\":42}";
        var actual = "{\"name\":\"Test\",\"value\":43}";

        // Act
        var diff = manager.CompareSnapshots(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotEmpty(diff.Differences);
    }

    [Fact]
    public void CompareSnapshots_ReturnsEqual_WhenSnapshotsMatch()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _testDirectory };
        var manager = new SnapshotManager(options);
        var content = "{\"name\":\"Test\",\"value\":42}";

        // Act
        var diff = manager.CompareSnapshots(content, content);

        // Assert
        Assert.True(diff.AreEqual);
        Assert.Empty(diff.Differences);
    }
}