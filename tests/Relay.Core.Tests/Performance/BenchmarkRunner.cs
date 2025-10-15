using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Console application to run performance benchmarks
/// </summary>
public class RelayBenchmarkRunner
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Relay Framework Performance Benchmarks");
        Console.WriteLine("=====================================");

        if (args.Length > 0 && args[0] == "--help")
        {
            ShowHelp();
            return;
        }

        var config = CreateBenchmarkConfig();

        try
        {
            if (args.Length == 0 || args[0] == "all")
            {
                await RunAllBenchmarks(config);
            }
            else
            {
                await RunSpecificBenchmark(args[0], config);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running benchmarks: {ex.Message}");
            Environment.Exit(1);
        }

        Console.WriteLine("Benchmarks completed successfully!");
    }

    private static async Task RunAllBenchmarks(IConfig config)
    {
        Console.WriteLine("Running all performance benchmarks...");

        Console.WriteLine("\n1. Running Relay Performance Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayPerformanceBenchmarks>(config);

        Console.WriteLine("\n2. Running Allocation Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayAllocationBenchmarks>(config);

        Console.WriteLine("\n3. Running Throughput Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayThroughputBenchmarks>(config);

        Console.WriteLine("\n4. Running Validation Benchmarks...");
        BenchmarkDotNet.Running.BenchmarkRunner.Run<ValidationBenchmarks>(config);

        await Task.CompletedTask;
    }

    private static async Task RunSpecificBenchmark(string benchmarkName, IConfig config)
    {
        Console.WriteLine($"Running specific benchmark: {benchmarkName}");

        switch (benchmarkName.ToLowerInvariant())
        {
            case "performance":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayPerformanceBenchmarks>(config);
                break;
            case "allocation":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayAllocationBenchmarks>(config);
                break;
            case "throughput":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<RelayThroughputBenchmarks>(config);
                break;
            case "validation":
                BenchmarkDotNet.Running.BenchmarkRunner.Run<ValidationBenchmarks>(config);
                break;
            default:
                Console.WriteLine($"Unknown benchmark: {benchmarkName}");
                ShowHelp();
                Environment.Exit(1);
                break;
        }

        await Task.CompletedTask;
    }

    private static IConfig CreateBenchmarkConfig()
    {
        return DefaultConfig.Instance
            .AddJob(Job.Default
                .WithToolchain(InProcessEmitToolchain.Instance) // For faster execution in CI
                .WithWarmupCount(3)
                .WithIterationCount(5)
                .WithInvocationCount(1000))
            .WithOptions(ConfigOptions.DisableOptimizationsValidator); // Allow debug builds for testing
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: BenchmarkRunner [benchmark-name]");
        Console.WriteLine();
        Console.WriteLine("Available benchmarks:");
        Console.WriteLine("  all         - Run all benchmarks (default)");
        Console.WriteLine("  performance - Run general performance benchmarks");
        Console.WriteLine("  allocation  - Run memory allocation benchmarks");
        Console.WriteLine("  throughput  - Run throughput benchmarks");
        Console.WriteLine("  validation  - Run validation rule benchmarks");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --help      - Show this help message");
    }
}

/// <summary>
/// Utility class for running benchmarks programmatically in tests
/// </summary>
public static class BenchmarkTestRunner
{
    /// <summary>
    /// Runs a quick performance validation (not full benchmark)
    /// </summary>
    public static async Task<TimeSpan> QuickPerformanceTest<T>(Func<T, Task> action, T instance, int iterations = 1000)
    {
        // Warmup
        for (int i = 0; i < 10; i++)
        {
            await action(instance);
        }

        // Measure
        var start = DateTime.UtcNow;
        for (int i = 0; i < iterations; i++)
        {
            await action(instance);
        }
        var end = DateTime.UtcNow;

        return end - start;
    }

    /// <summary>
    /// Validates that performance meets minimum requirements
    /// </summary>
    public static async Task ValidatePerformanceRequirements()
    {
        var benchmarks = new RelayPerformanceBenchmarks();
        benchmarks.Setup();

        // Quick validation - should complete 1000 requests in reasonable time
        var duration = await QuickPerformanceTest(
            async b => await b.SendRequest(),
            benchmarks,
            1000);

        if (duration.TotalMilliseconds > 5000) // 5 seconds for 1000 requests
        {
            throw new InvalidOperationException($"Performance requirement not met. 1000 requests took {duration.TotalMilliseconds}ms");
        }

        Console.WriteLine($"Performance validation passed: 1000 requests in {duration.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Validates memory allocation requirements
    /// </summary>
    public static async Task ValidateAllocationRequirements()
    {
        var benchmarks = new RelayAllocationBenchmarks();
        benchmarks.Setup();

        // Measure memory before
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(false);

        // Run benchmark
        for (int i = 0; i < 1000; i++)
        {
            await benchmarks.MinimalAllocationRequest();
        }

        // Measure memory after
        var memoryAfter = GC.GetTotalMemory(false);
        var allocatedBytes = memoryAfter - memoryBefore;

        // Should not allocate more than 1MB for 1000 simple requests
        if (allocatedBytes > 1024 * 1024)
        {
            throw new InvalidOperationException($"Allocation requirement not met. 1000 requests allocated {allocatedBytes} bytes");
        }

        Console.WriteLine($"Allocation validation passed: 1000 requests allocated {allocatedBytes} bytes");
    }
}