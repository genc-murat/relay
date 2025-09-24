using System.Diagnostics;
using System.Runtime;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay;
using Relay.Core;
using Relay.Core.Performance;
using SimpleCrudApi.Data;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;
using SimpleCrudApi.Services;
using SimpleCrudApi.MediatR.Requests;
using SimpleCrudApi.MediatR.Handlers;

namespace SimpleCrudApi;

public class UltimatePerformanceBenchmark
{
    public static async Task RunUltimateBenchmark()
    {
        Console.WriteLine("🚀 ULTIMATE RELAY PERFORMANCE BENCHMARK SUITE");
        Console.WriteLine("==============================================");
        Console.WriteLine($"🔧 Hardware Info: {Environment.ProcessorCount} CPU cores");
        Console.WriteLine($"💾 Memory Info: {GC.GetTotalMemory(false) / 1024 / 1024:N0} MB allocated");
        Console.WriteLine($"⚡ SIMD Support: {SIMDHelpers.Capabilities.GetCapabilityString()}");
        Console.WriteLine();

        await RunBenchmarkSuite();
    }

    private static async Task RunBenchmarkSuite()
    {
        var services = new ServiceCollection();

        // Common services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Relay setup with all optimizations
        services.AddRelay();
        services.AddScoped<UserService>();
        services.AddScoped<UserNotificationHandlers>();
        services.AddRelayHandlers();

        // Register optimized implementations
        services.AddScoped<AOTOptimizedRelay>();
        services.AddScoped<SIMDOptimizedRelay>();

        // MediatR setup
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRUserHandlers).Assembly));
        services.AddScoped<MediatRUserHandlers>();
        services.AddScoped<MediatRUserNotificationHandlers>();

        var serviceProvider = services.BuildServiceProvider();

        // Prepare test data
        Console.WriteLine("📊 Preparing test environment...");
        var repository = serviceProvider.GetRequiredService<IUserRepository>();
        await PrepareTestData(repository);

        // Force GC before benchmarks
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        var testResults = new List<BenchmarkResult>();

        // Run all benchmark categories
        testResults.AddRange(await RunSingleRequestBenchmarks(serviceProvider));
        testResults.AddRange(await RunBatchRequestBenchmarks(serviceProvider));
        testResults.AddRange(await RunConcurrencyBenchmarks(serviceProvider));
        testResults.AddRange(await RunMemoryBenchmarks(serviceProvider));
        testResults.AddRange(await RunThroughputBenchmarks(serviceProvider));

        // Display comprehensive results
        DisplayBenchmarkResults(testResults);

        serviceProvider.Dispose();
    }

    private static async Task PrepareTestData(IUserRepository repository)
    {
        for (int i = 1; i <= 1000; i++)
        {
            await repository.CreateAsync(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static async Task<List<BenchmarkResult>> RunSingleRequestBenchmarks(IServiceProvider serviceProvider)
    {
        Console.WriteLine("🎯 Running Single Request Benchmarks...");
        var results = new List<BenchmarkResult>();

        const int iterations = 1_000_000;
        var query = new GetUserQuery(1);
        var mediatrQuery = new MediatRGetUserQuery(1);

        // Get all implementations
        var standardRelay = serviceProvider.GetRequiredService<IRelay>();
        var aotOptimized = serviceProvider.GetRequiredService<AOTOptimizedRelay>();
        var simdOptimized = serviceProvider.GetRequiredService<SIMDOptimizedRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var userService = serviceProvider.GetRequiredService<UserService>();

        // Warmup
        Console.WriteLine("  🔥 Warming up...");
        for (int i = 0; i < 10000; i++)
        {
            await standardRelay.SendAsync(query);
            await mediator.Send(mediatrQuery);
        }

        // Benchmark each implementation
        results.Add(await BenchmarkImplementation("Direct Call", iterations, async () => await userService.GetUser(query, default)));
        results.Add(await BenchmarkImplementation("Standard Relay", iterations, async () => await standardRelay.SendAsync(query)));
        results.Add(await BenchmarkImplementation("AOT Optimized", iterations, async () => await aotOptimized.SendAsync(query)));
        results.Add(await BenchmarkImplementation("SIMD Optimized", iterations, async () => await simdOptimized.SendAsync(query)));
        results.Add(await BenchmarkImplementation("MediatR", iterations, async () => await mediator.Send(mediatrQuery)));

        return results;
    }

    private static async Task<List<BenchmarkResult>> RunBatchRequestBenchmarks(IServiceProvider serviceProvider)
    {
        Console.WriteLine("📦 Running Batch Request Benchmarks...");
        var results = new List<BenchmarkResult>();

        const int batchSize = 100;
        const int iterations = 10_000;

        var queries = Enumerable.Range(1, batchSize).Select(i => new GetUserQuery(i % 100 + 1)).ToArray();
        var mediatrQueries = queries.Select(q => new MediatRGetUserQuery(q.Id)).ToArray();

        var simdOptimized = serviceProvider.GetRequiredService<SIMDOptimizedRelay>();
        var standardRelay = serviceProvider.GetRequiredService<IRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Batch benchmarks
        results.Add(await BenchmarkImplementation("Standard Batch", iterations, async () =>
        {
            var tasks = queries.Select(q => standardRelay.SendAsync(q)).ToArray();
            await Task.WhenAll(tasks.Select(t => t.AsTask()));
        }));

        results.Add(await BenchmarkImplementation("SIMD Batch", iterations, async () =>
            await simdOptimized.SendBatchAsync(queries)));

        results.Add(await BenchmarkImplementation("MediatR Batch", iterations, async () =>
        {
            var tasks = mediatrQueries.Select(q => mediator.Send(q)).ToArray();
            await Task.WhenAll(tasks);
        }));

        return results;
    }

    private static async Task<List<BenchmarkResult>> RunConcurrencyBenchmarks(IServiceProvider serviceProvider)
    {
        Console.WriteLine("🔄 Running Concurrency Benchmarks...");
        var results = new List<BenchmarkResult>();

        const int concurrentRequests = 1000;
        const int iterations = 1000;

        var standardRelay = serviceProvider.GetRequiredService<IRelay>();
        var simdOptimized = serviceProvider.GetRequiredService<SIMDOptimizedRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Concurrent execution benchmarks
        results.Add(await BenchmarkImplementation("Standard Concurrent", iterations, async () =>
        {
            var tasks = new Task[concurrentRequests];
            for (int i = 0; i < concurrentRequests; i++)
            {
                int userId = i % 100 + 1;
                tasks[i] = standardRelay.SendAsync(new GetUserQuery(userId)).AsTask();
            }
            await Task.WhenAll(tasks);
        }));

        results.Add(await BenchmarkImplementation("SIMD Concurrent", iterations, async () =>
        {
            var tasks = new Task[concurrentRequests];
            for (int i = 0; i < concurrentRequests; i++)
            {
                int userId = i % 100 + 1;
                tasks[i] = simdOptimized.SendAsync(new GetUserQuery(userId)).AsTask();
            }
            await Task.WhenAll(tasks);
        }));

        results.Add(await BenchmarkImplementation("MediatR Concurrent", iterations, async () =>
        {
            var tasks = new Task[concurrentRequests];
            for (int i = 0; i < concurrentRequests; i++)
            {
                int userId = i % 100 + 1;
                tasks[i] = mediator.Send(new MediatRGetUserQuery(userId));
            }
            await Task.WhenAll(tasks);
        }));

        return results;
    }

    private static async Task<List<BenchmarkResult>> RunMemoryBenchmarks(IServiceProvider serviceProvider)
    {
        Console.WriteLine("💾 Running Memory Allocation Benchmarks...");
        var results = new List<BenchmarkResult>();

        const int iterations = 100_000;
        var query = new GetUserQuery(1);

        var standardRelay = serviceProvider.GetRequiredService<IRelay>();

        // Memory allocation benchmarks
        results.Add(await BenchmarkImplementationWithMemory("Standard Memory", iterations, async () =>
            await standardRelay.SendAsync(query)));

        return results;
    }

    private static async Task<List<BenchmarkResult>> RunThroughputBenchmarks(IServiceProvider serviceProvider)
    {
        Console.WriteLine("⚡ Running Throughput Benchmarks...");
        var results = new List<BenchmarkResult>();

        const int duration = 5000; // 5 seconds
        var query = new GetUserQuery(1);
        var mediatrQuery = new MediatRGetUserQuery(1);

        var standardRelay = serviceProvider.GetRequiredService<IRelay>();
        var simdOptimized = serviceProvider.GetRequiredService<SIMDOptimizedRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Throughput benchmarks
        results.Add(await BenchmarkThroughput("Standard Throughput", duration, () => standardRelay.SendAsync(query).AsTask()));
        results.Add(await BenchmarkThroughput("SIMD Throughput", duration, () => simdOptimized.SendAsync(query).AsTask()));
        results.Add(await BenchmarkThroughput("MediatR Throughput", duration, () => mediator.Send(mediatrQuery)));

        return results;
    }

    private static async Task<BenchmarkResult> BenchmarkImplementation(string name, int iterations, Func<Task> operation)
    {
        // Force GC before benchmark
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await operation();
        }

        stopwatch.Stop();

        var result = new BenchmarkResult
        {
            Name = name,
            Iterations = iterations,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            MicrosPerOp = stopwatch.ElapsedMilliseconds * 1000.0 / iterations,
            Category = "Single Request"
        };

        Console.WriteLine($"  ✅ {name}: {result.ElapsedMs:N0} ms ({result.MicrosPerOp:F3} μs/op)");
        return result;
    }

    private static async Task<BenchmarkResult> BenchmarkImplementationWithMemory(string name, int iterations, Func<Task> operation)
    {
        // Force GC and measure initial memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await operation();
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);

        var result = new BenchmarkResult
        {
            Name = name,
            Iterations = iterations,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            MicrosPerOp = stopwatch.ElapsedMilliseconds * 1000.0 / iterations,
            MemoryAllocated = Math.Max(0, finalMemory - initialMemory),
            Category = "Memory"
        };

        Console.WriteLine($"  ✅ {name}: {result.ElapsedMs:N0} ms, {result.MemoryAllocated:N0} bytes allocated");
        return result;
    }

    private static async Task<BenchmarkResult> BenchmarkThroughput(string name, int durationMs, Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        var count = 0;

        while (stopwatch.ElapsedMilliseconds < durationMs)
        {
            await operation();
            count++;
        }

        stopwatch.Stop();
        var throughput = count * 1000.0 / stopwatch.ElapsedMilliseconds;

        var result = new BenchmarkResult
        {
            Name = name,
            Iterations = count,
            ElapsedMs = stopwatch.ElapsedMilliseconds,
            Throughput = throughput,
            Category = "Throughput"
        };

        Console.WriteLine($"  ✅ {name}: {throughput:F0} ops/sec ({count:N0} ops in {stopwatch.ElapsedMilliseconds:N0} ms)");
        return result;
    }

    private static void DisplayBenchmarkResults(List<BenchmarkResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("📊 COMPREHENSIVE BENCHMARK RESULTS");
        Console.WriteLine("==================================");

        var categories = results.GroupBy(r => r.Category);

        foreach (var category in categories)
        {
            Console.WriteLine();
            Console.WriteLine($"🏆 {category.Key} Results:");
            Console.WriteLine(new string('-', 50));

            var categoryResults = category.ToList();
            var fastest = categoryResults.OrderBy(r => r.MicrosPerOp > 0 ? r.MicrosPerOp : r.ElapsedMs).First();

            foreach (var result in categoryResults.OrderBy(r => r.MicrosPerOp > 0 ? r.MicrosPerOp : r.ElapsedMs))
            {
                var speedup = result == fastest ? 1.0 :
                    (result.MicrosPerOp > 0 ? result.MicrosPerOp / fastest.MicrosPerOp :
                     result.ElapsedMs / (double)fastest.ElapsedMs);

                var icon = result == fastest ? "🥇" : speedup < 1.5 ? "🥈" : speedup < 2.0 ? "🥉" : "📊";

                Console.WriteLine($"{icon} {result.Name}:");

                if (result.MicrosPerOp > 0)
                    Console.WriteLine($"   Time: {result.MicrosPerOp:F3} μs/op");

                if (result.Throughput > 0)
                    Console.WriteLine($"   Throughput: {result.Throughput:F0} ops/sec");

                if (result.MemoryAllocated > 0)
                    Console.WriteLine($"   Memory: {result.MemoryAllocated:N0} bytes");

                if (speedup > 1.0)
                    Console.WriteLine($"   Speedup: {speedup:F2}x slower than fastest");

                Console.WriteLine();
            }
        }

        // Overall summary
        Console.WriteLine("🎯 PERFORMANCE SUMMARY:");
        Console.WriteLine("======================");

        var singleRequestResults = results.Where(r => r.Category == "Single Request").ToList();
        if (singleRequestResults.Any())
        {
            var directCall = singleRequestResults.FirstOrDefault(r => r.Name.Contains("Direct"));
            var fastestRelay = singleRequestResults.Where(r => !r.Name.Contains("MediatR") && !r.Name.Contains("Direct"))
                .OrderBy(r => r.MicrosPerOp).FirstOrDefault();
            var mediatr = singleRequestResults.FirstOrDefault(r => r.Name.Contains("MediatR"));

            if (directCall != null && fastestRelay != null)
            {
                var overhead = fastestRelay.MicrosPerOp / directCall.MicrosPerOp;
                Console.WriteLine($"🔥 Fastest Relay overhead vs Direct: {overhead:F2}x");
            }

            if (fastestRelay != null && mediatr != null)
            {
                var speedup = mediatr.MicrosPerOp / fastestRelay.MicrosPerOp;
                Console.WriteLine($"🚀 Fastest Relay vs MediatR: {speedup:F2}x faster");
            }
        }

        Console.WriteLine();
        Console.WriteLine("✨ Benchmark completed successfully!");
    }

    private class BenchmarkResult
    {
        public string Name { get; set; } = "";
        public int Iterations { get; set; }
        public long ElapsedMs { get; set; }
        public double MicrosPerOp { get; set; }
        public long MemoryAllocated { get; set; }
        public double Throughput { get; set; }
        public string Category { get; set; } = "";
    }
}