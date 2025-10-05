using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for parallel processing operations.
    /// </summary>
    public sealed class ParallelProcessingStatistics
    {
        public int TasksStarted { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksExecuted { get; set; }
        public int TasksFailed { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan AverageTaskDuration { get; set; }
        public double Speedup { get; set; }
        public double Efficiency { get; set; }
        public double ActualParallelism { get; set; }
    }
}
