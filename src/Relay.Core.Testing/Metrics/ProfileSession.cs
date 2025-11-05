using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a profiling session that tracks multiple operations.
/// </summary>
public class ProfileSession
{
    private readonly List<OperationMetrics> _operations = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the name of the profiling session.
    /// </summary>
    public string SessionName { get; }

    /// <summary>
    /// Gets the start time of the session.
    /// </summary>
    public DateTime StartTime { get; private set; }

    /// <summary>
    /// Gets the end time of the session, or null if still running.
    /// </summary>
    public DateTime? EndTime { get; private set; }

    /// <summary>
    /// Gets whether the session is currently running.
    /// </summary>
    public bool IsRunning => EndTime == null;

    /// <summary>
    /// Gets the total duration of the session.
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;

    /// <summary>
    /// Gets the collection of operation metrics recorded in this session.
    /// </summary>
    public IReadOnlyList<OperationMetrics> Operations => _operations;

    /// <summary>
    /// Gets the total memory used across all operations.
    /// </summary>
    public long TotalMemoryUsed => _operations.Sum(o => o.MemoryUsed);

    /// <summary>
    /// Gets the total allocations across all operations.
    /// </summary>
    public long TotalAllocations => _operations.Sum(o => o.Allocations);

    /// <summary>
    /// Gets the average operation duration.
    /// </summary>
    public TimeSpan AverageOperationDuration => _operations.Count > 0
        ? TimeSpan.FromTicks((long)_operations.Average(o => o.Duration.Ticks))
        : TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileSession"/> class.
    /// </summary>
    /// <param name="sessionName">The name of the session.</param>
    public ProfileSession(string sessionName)
    {
        SessionName = sessionName ?? throw new ArgumentNullException(nameof(sessionName));
        EndTime = DateTime.MinValue; // Indicate not started
    }

    /// <summary>
    /// Starts the profiling session.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (IsRunning)
                throw new InvalidOperationException("Session is already running.");

            StartTime = DateTime.UtcNow;
            EndTime = null;
        }
    }

    /// <summary>
    /// Stops the profiling session.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Session is not running.");

            EndTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Adds an operation metric to the session.
    /// </summary>
    /// <param name="metrics">The operation metrics to add.</param>
    public void AddOperation(OperationMetrics metrics)
    {
        if (metrics == null)
            throw new ArgumentNullException(nameof(metrics));

        lock (_lock)
        {
            _operations.Add(metrics);
        }
    }

    /// <summary>
    /// Clears all operation metrics from the session.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _operations.Clear();
        }
    }
}