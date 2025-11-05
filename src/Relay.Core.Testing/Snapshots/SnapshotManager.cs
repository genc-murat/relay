using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Manages snapshot files and comparison logic.
/// </summary>
public class SnapshotManager
{
    private readonly SnapshotOptions _options;
    private readonly ISnapshotSerializer _serializer;
    private readonly SnapshotComparer _comparer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotManager"/> class.
    /// </summary>
    /// <param name="options">The snapshot options.</param>
    public SnapshotManager(SnapshotOptions? options = null)
    {
        _options = options ?? new SnapshotOptions();
        _serializer = CreateSerializer();
        _comparer = new SnapshotComparer();

        EnsureSnapshotDirectoryExists();
    }

    /// <summary>
    /// Matches a value against an existing snapshot or creates a new one.
    /// </summary>
    /// <typeparam name="T">The type of the value to match.</typeparam>
    /// <param name="value">The value to match against the snapshot.</param>
    /// <param name="snapshotName">The name of the snapshot file.</param>
    /// <returns>The result of the snapshot comparison.</returns>
    public async Task<SnapshotResult> MatchSnapshotAsync<T>(T value, string snapshotName)
    {
        var snapshotPath = GetSnapshotPath(snapshotName);

        if (!File.Exists(snapshotPath))
        {
            // Create new snapshot if it doesn't exist
            await UpdateSnapshotAsync(value, snapshotName);
            return new SnapshotResult
            {
                Matched = true,
                SnapshotPath = snapshotPath
            };
        }

        var expectedContent = await File.ReadAllTextAsync(snapshotPath);
        var actualContent = _serializer.Serialize(value);

        var diff = _comparer.Compare(expectedContent, actualContent);

        if (!diff.AreEqual && _options.UpdateSnapshots)
        {
            await UpdateSnapshotAsync(value, snapshotName);
            return new SnapshotResult
            {
                Matched = true,
                SnapshotPath = snapshotPath
            };
        }

        return new SnapshotResult
        {
            Matched = diff.AreEqual,
            SnapshotPath = snapshotPath,
            Diff = diff.AreEqual ? null : diff
        };
    }

    /// <summary>
    /// Updates or creates a snapshot with the given value.
    /// </summary>
    /// <typeparam name="T">The type of the value to snapshot.</typeparam>
    /// <param name="value">The value to store in the snapshot.</param>
    /// <param name="snapshotName">The name of the snapshot file.</param>
    public async Task UpdateSnapshotAsync<T>(T value, string snapshotName)
    {
        var snapshotPath = GetSnapshotPath(snapshotName);
        EnsureSnapshotDirectoryExists();
        var content = _serializer.Serialize(value);

        await File.WriteAllTextAsync(snapshotPath, content);
    }

    /// <summary>
    /// Compares two snapshots directly.
    /// </summary>
    /// <param name="expected">The expected snapshot content.</param>
    /// <param name="actual">The actual snapshot content.</param>
    /// <returns>The snapshot diff.</returns>
    public SnapshotDiff CompareSnapshots(string expected, string actual)
    {
        return _comparer.Compare(expected, actual);
    }

    private string GetSnapshotPath(string snapshotName)
    {
        var sanitizedName = SanitizeSnapshotName(snapshotName);
        return Path.Combine(_options.SnapshotDirectory, $"{sanitizedName}.json");
    }

    private string SanitizeSnapshotName(string name)
    {
        // Replace invalid file name characters with underscores
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = name;

        foreach (var invalidChar in invalidChars)
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        return sanitized;
    }

    private void EnsureSnapshotDirectoryExists()
    {
        if (!Directory.Exists(_options.SnapshotDirectory))
        {
            Directory.CreateDirectory(_options.SnapshotDirectory);
        }
    }

    private ISnapshotSerializer CreateSerializer()
    {
        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = _options.Serialization.WriteIndented,
            PropertyNamingPolicy = _options.Serialization.PropertyNamingPolicy,
            DefaultIgnoreCondition = _options.Serialization.IgnoreNullValues
                ? System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                : System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };

        return new JsonSnapshotSerializer(jsonOptions);
    }
}