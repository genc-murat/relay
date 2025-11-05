using System;

namespace Relay.Core.AI;

/// <summary>
/// Represents training progress information
/// </summary>
public class TrainingProgress
{
    /// <summary>
    /// Current training phase
    /// </summary>
    public TrainingPhase Phase { get; set; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Current status message
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Number of samples processed
    /// </summary>
    public int SamplesProcessed { get; set; }

    /// <summary>
    /// Total number of samples
    /// </summary>
    public int TotalSamples { get; set; }

    /// <summary>
    /// Elapsed time since training started
    /// </summary>
    public TimeSpan ElapsedTime { get; set; }

    /// <summary>
    /// Current model metrics (if available)
    /// </summary>
    public ModelMetrics? CurrentMetrics { get; set; }
}

/// <summary>
/// Training progress callback delegate
/// </summary>
/// <param name="progress">Current training progress</param>
public delegate void TrainingProgressCallback(TrainingProgress progress);
