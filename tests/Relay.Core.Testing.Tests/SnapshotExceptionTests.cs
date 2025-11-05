using System;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class SnapshotExceptionTests
{
    [Fact]
    public void SnapshotNotFoundException_ConstructsWithSnapshotPath()
    {
        // Arrange
        var snapshotPath = "/path/to/snapshot.txt";

        // Act
        var exception = new SnapshotNotFoundException(snapshotPath);

        // Assert
        Assert.Contains(snapshotPath, exception.Message);
        Assert.Equal(snapshotPath, exception.SnapshotPath);
    }

    [Fact]
    public void SnapshotSerializationException_ConstructsWithMessage()
    {
        // Arrange
        var message = "Serialization failed";

        // Act
        var exception = new SnapshotSerializationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void SnapshotSerializationException_ConstructsWithMessageAndInnerException()
    {
        // Arrange
        var message = "Serialization failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SnapshotSerializationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void SnapshotMismatchException_ConstructsWithDiff()
    {
        // Arrange
        var diff = new SnapshotDiff
        {
            AreEqual = false,
            Differences = new System.Collections.Generic.List<DiffLine>
            {
                new DiffLine { Type = DiffType.Added, Content = "+ new line" }
            }
        };

        // Act
        var exception = new SnapshotMismatchException(diff);

        // Assert
        Assert.Equal("Snapshot mismatch detected", exception.Message);
        Assert.Equal(diff, exception.Diff);
    }

    [Fact]
    public void ProfilerNotStartedException_ConstructsWithDefaultMessage()
    {
        // Act
        var exception = new ProfilerNotStartedException();

        // Assert
        Assert.Equal("Performance profiler session has not been started.", exception.Message);
    }

    [Fact]
    public void ProfilerNotStartedException_ConstructsWithCustomMessage()
    {
        // Arrange
        var message = "Custom profiler error";

        // Act
        var exception = new ProfilerNotStartedException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void PerformanceThresholdExceededException_ConstructsWithValues()
    {
        // Arrange
        var message = "Threshold exceeded";
        var threshold = 100;
        var actual = 150;

        // Act
        var exception = new PerformanceThresholdExceededException(message, threshold, actual);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(threshold, exception.Threshold);
        Assert.Equal(actual, exception.Actual);
    }
}