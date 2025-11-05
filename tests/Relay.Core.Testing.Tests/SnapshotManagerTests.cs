using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class SnapshotManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _snapshotDirectory;

    public SnapshotManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _snapshotDirectory = Path.Combine(_testDirectory, "__snapshots__");
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
    public void Constructor_WithDefaultOptions_CreatesManager()
    {
        // Act
        var manager = new SnapshotManager();

        // Assert
        Assert.NotNull(manager);
        Assert.True(Directory.Exists("__snapshots__"));
    }

    [Fact]
    public void Constructor_WithCustomOptions_CreatesManager()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _snapshotDirectory,
            UpdateSnapshots = true
        };

        // Act
        var manager = new SnapshotManager(options);

        // Assert
        Assert.NotNull(manager);
        Assert.True(Directory.Exists(_snapshotDirectory));
    }

    [Fact]
    public async Task MatchSnapshotAsync_NewSnapshot_CreatesSnapshotAndReturnsMatched()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var testObject = new { Name = "Test", Value = 42 };
        var snapshotName = "new_snapshot";

        // Act
        var result = await manager.MatchSnapshotAsync(testObject, snapshotName);

        // Assert
        Assert.True(result.Matched);
        Assert.Contains(snapshotName, result.SnapshotPath);
        Assert.EndsWith(".json", result.SnapshotPath);
        Assert.True(File.Exists(result.SnapshotPath));
    }

    [Fact]
    public async Task MatchSnapshotAsync_ExistingMatchingSnapshot_ReturnsMatched()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var testObject = new { Name = "Test", Value = 42 };
        var snapshotName = "existing_snapshot";

        // Create initial snapshot
        await manager.MatchSnapshotAsync(testObject, snapshotName);

        // Act - match against the same object
        var result = await manager.MatchSnapshotAsync(testObject, snapshotName);

        // Assert
        Assert.True(result.Matched);
        Assert.Contains(snapshotName, result.SnapshotPath);
        Assert.Null(result.Diff);
    }

    [Fact]
    public async Task MatchSnapshotAsync_ExistingNonMatchingSnapshot_UpdateSnapshotsFalse_ReturnsNotMatched()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _snapshotDirectory,
            UpdateSnapshots = false
        };
        var manager = new SnapshotManager(options);
        var snapshotName = "non_matching_snapshot";

        // Create initial snapshot
        var originalObject = new { Name = "Original", Value = 1 };
        await manager.MatchSnapshotAsync(originalObject, snapshotName);

        // Act - match against different object
        var differentObject = new { Name = "Different", Value = 2 };
        var result = await manager.MatchSnapshotAsync(differentObject, snapshotName);

        // Assert
        Assert.False(result.Matched);
        Assert.Contains(snapshotName, result.SnapshotPath);
        Assert.NotNull(result.Diff);
        Assert.False(result.Diff.AreEqual);
    }

    [Fact]
    public async Task MatchSnapshotAsync_ExistingNonMatchingSnapshot_UpdateSnapshotsTrue_UpdatesAndReturnsMatched()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _snapshotDirectory,
            UpdateSnapshots = true
        };
        var manager = new SnapshotManager(options);
        var snapshotName = "update_snapshot";

        // Create initial snapshot
        var originalObject = new { Name = "Original", Value = 1 };
        await manager.MatchSnapshotAsync(originalObject, snapshotName);

        // Act - match against different object (should update)
        var differentObject = new { Name = "Updated", Value = 2 };
        var result = await manager.MatchSnapshotAsync(differentObject, snapshotName);

        // Assert
        Assert.True(result.Matched);
        Assert.Contains(snapshotName, result.SnapshotPath);
        Assert.Null(result.Diff);
    }

    [Fact]
    public async Task UpdateSnapshotAsync_UpdatesSnapshotFile()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var testObject = new { Message = "Updated content" };
        var snapshotName = "update_test";

        // Act
        await manager.UpdateSnapshotAsync(testObject, snapshotName);

        // Assert
        var snapshotPath = Path.Combine(_snapshotDirectory, $"{snapshotName}.json");
        Assert.True(File.Exists(snapshotPath));

        var content = await File.ReadAllTextAsync(snapshotPath);
        Assert.Contains("Updated content", content);
    }

    [Fact]
    public void CompareSnapshots_IdenticalContent_ReturnsEqual()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var content = "{\"name\":\"test\",\"value\":123}";

        // Act
        var diff = manager.CompareSnapshots(content, content);

        // Assert
        Assert.True(diff.AreEqual);
        Assert.Empty(diff.Differences);
    }

    [Fact]
    public void CompareSnapshots_DifferentContent_ReturnsNotEqual()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var expected = "{\"name\":\"test\",\"value\":123}";
        var actual = "{\"name\":\"test\",\"value\":456}";

        // Act
        var diff = manager.CompareSnapshots(expected, actual);

        // Assert
        Assert.False(diff.AreEqual);
        Assert.NotEmpty(diff.Differences);
    }

    [Fact]
    public async Task MatchSnapshotAsync_SanitizesSnapshotName()
    {
        // Arrange
        var options = new SnapshotOptions { SnapshotDirectory = _snapshotDirectory };
        var manager = new SnapshotManager(options);
        var testObject = new { Data = "test" };
        var invalidName = "invalid<>|name:with*special?chars";

        // Act
        var result = await manager.MatchSnapshotAsync(testObject, invalidName);

        // Assert
        Assert.True(result.Matched);
        Assert.Contains("invalid___name_with_special_chars", result.SnapshotPath);
        Assert.EndsWith(".json", result.SnapshotPath);
    }

    [Fact]
    public async Task MatchSnapshotAsync_CreatesDirectoryIfNotExists()
    {
        // Arrange
        var customSnapshotDir = Path.Combine(_testDirectory, "custom_snapshots");
        var options = new SnapshotOptions { SnapshotDirectory = customSnapshotDir };
        var testObject = new { Test = true };

        // Ensure directory doesn't exist initially
        if (Directory.Exists(customSnapshotDir))
        {
            Directory.Delete(customSnapshotDir, true);
        }

        var manager = new SnapshotManager(options);

        // Act
        await manager.MatchSnapshotAsync(testObject, "test");

        // Assert
        Assert.True(Directory.Exists(customSnapshotDir));
    }

    [Fact]
    public async Task MatchSnapshotAsync_WithComplexObject_SerializesCorrectly()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _snapshotDirectory,
            Serialization = new SerializationOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true
            }
        };
        var manager = new SnapshotManager(options);
        var complexObject = new
        {
            Id = 123,
            Name = "Complex Test",
            Items = new[] { "item1", "item2", "item3" },
            Metadata = new { Created = DateTime.UtcNow.Date, Version = "1.0" },
            NullValue = (string)null
        };

        // Act
        var result = await manager.MatchSnapshotAsync(complexObject, "complex");

        // Assert
        Assert.True(result.Matched);
        Assert.True(File.Exists(result.SnapshotPath));

        var content = await File.ReadAllTextAsync(result.SnapshotPath);
        Assert.Contains("\"name\":", content); // camelCase conversion
        Assert.Contains("item1", content);
        Assert.DoesNotContain("nullValue", content); // null values ignored
    }

    [Fact]
    public async Task MatchSnapshotAsync_WithMinimalSerializationOptions_Works()
    {
        // Arrange
        var options = new SnapshotOptions
        {
            SnapshotDirectory = _snapshotDirectory,
            Serialization = new SerializationOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = null, // Use default (no transformation)
                IgnoreNullValues = false
            }
        };
        var manager = new SnapshotManager(options);
        var testObject = new { Name = "Test", Value = 42, NullField = (string)null };

        // Act
        var result = await manager.MatchSnapshotAsync(testObject, "minimal");

        // Assert
        Assert.True(result.Matched);
        var content = await File.ReadAllTextAsync(result.SnapshotPath);
        Assert.Contains("Name", content); // No camelCase conversion
        Assert.Contains("NullField", content); // Null values included
        Assert.DoesNotContain("\n", content); // Not indented
    }
}