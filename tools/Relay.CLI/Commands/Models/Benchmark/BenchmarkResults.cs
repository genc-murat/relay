namespace Relay.CLI.Commands.Models.Benchmark;

public class BenchmarkResults
{
    public TestConfiguration TestConfiguration { get; set; } = new();
    public Dictionary<string, BenchmarkResult> RelayResults { get; set; } = new();
    public Dictionary<string, BenchmarkResult> ComparisonResults { get; set; } = new();
}
