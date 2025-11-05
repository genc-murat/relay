using System;

namespace Relay.Core.Testing;

/// <summary>
/// Exception thrown when a snapshot file is not found.
/// </summary>
public class SnapshotNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotNotFoundException"/> class.
    /// </summary>
    /// <param name="snapshotPath">The path to the missing snapshot file.</param>
    public SnapshotNotFoundException(string snapshotPath)
        : base($"Snapshot file not found: {snapshotPath}")
    {
        SnapshotPath = snapshotPath;
    }

    /// <summary>
    /// Gets the path to the missing snapshot file.
    /// </summary>
    public string SnapshotPath { get; }
}

/// <summary>
/// Exception thrown when snapshot serialization fails.
/// </summary>
public class SnapshotSerializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotSerializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SnapshotSerializationException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when snapshot comparison fails in strict mode.
/// </summary>
public class SnapshotMismatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotMismatchException"/> class.
    /// </summary>
    /// <param name="diff">The snapshot diff containing the differences.</param>
    public SnapshotMismatchException(SnapshotDiff diff)
        : base("Snapshot mismatch detected")
    {
        Diff = diff;
    }

    /// <summary>
    /// Gets the snapshot diff containing the differences.
    /// </summary>
    public SnapshotDiff Diff { get; }
}

/// <summary>
/// Exception thrown when attempting to profile without an active session.
/// </summary>
public class ProfilerNotStartedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilerNotStartedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ProfilerNotStartedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfilerNotStartedException"/> class.
    /// </summary>
    public ProfilerNotStartedException()
        : base("Performance profiler session has not been started.")
    {
    }
}

/// <summary>
/// Exception thrown when performance thresholds are exceeded.
/// </summary>
public class PerformanceThresholdExceededException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceThresholdExceededException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="threshold">The exceeded threshold value.</param>
    /// <param name="actual">The actual measured value.</param>
    public PerformanceThresholdExceededException(string message, object threshold, object actual)
        : base(message)
    {
        Threshold = threshold;
        Actual = actual;
    }

    /// <summary>
    /// Gets the threshold value that was exceeded.
    /// </summary>
    public object Threshold { get; }

    /// <summary>
    /// Gets the actual measured value.
    /// </summary>
    public object Actual { get; }
}