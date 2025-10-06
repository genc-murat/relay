namespace Relay.CLI.Commands.Models.Benchmark;

public class BenchmarkResult
{
    public string Name { get; set; } = "";
    public TimeSpan TotalTime { get; set; }
    public int Iterations { get; set; }
    public double AverageTime { get; set; }
    public double RequestsPerSecond { get; set; }
    public long MemoryAllocated { get; set; }
    public int Threads { get; set; }
}