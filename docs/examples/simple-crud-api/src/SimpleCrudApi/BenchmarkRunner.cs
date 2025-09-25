using BenchmarkDotNet.Running;
using SimpleCrudApi.Benchmarks;

namespace SimpleCrudApi;

public class BenchmarkRunner
{
    public static void RunBenchmarks(string[] args)
    {
        // Check if quick benchmark is requested
        var benchmarkType = typeof(QuickBenchmark);

        // Run the benchmark
        var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run(benchmarkType);

        // Print summary
        Console.WriteLine("Benchmark Results Summary:");
        Console.WriteLine("========================");
        Console.WriteLine($"Total benchmarks run: {summary.Reports.Count()}");
        Console.WriteLine($"Failed benchmarks: {summary.Reports.Count(r => !r.Success)}");

        Console.WriteLine("\nTop performing operations:");
        var orderedReports = summary.Reports
            .Where(r => r.Success)
            .OrderBy(r => r.ResultStatistics?.Mean ?? double.MaxValue)
            .Take(5);

        foreach (var report in orderedReports)
        {
            Console.WriteLine($"- {report.BenchmarkCase.Descriptor.DisplayInfo}: {report.ResultStatistics?.Mean:F2} ns");
        }
    }
}