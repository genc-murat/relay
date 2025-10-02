using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;

namespace Relay.OptimizationDemo;

/// <summary>
/// Demo showing the performance improvements of our optimizations
/// </summary>
public class OptimizedRelayDemo
{
    public static async Task RunDemo()
    {
        Console.WriteLine("🚀 RELAY PERFORMANCE OPTIMIZATION DEMO");
        Console.WriteLine("======================================");

        await TestTypeCacheOptimization();
        await TestExceptionOptimization();
        await TestSIMDHashOptimization();
        await TestBufferPoolOptimization();

        Console.WriteLine("\n✅ All optimization demos completed successfully!");
    }

    private static async Task TestTypeCacheOptimization()
    {
        Console.WriteLine("\n🔥 Testing TypeCache Optimization (FrozenDictionary vs ConcurrentDictionary)");
        Console.WriteLine("==========================================================================");

        const int iterations = 1_000_000;
        var testObjects = new object[] { "test", 42, DateTime.Now, Guid.NewGuid(), new List<int>() };

        // Test old approach (ConcurrentDictionary simulation)
        var legacyCache = new Dictionary<object, Type>();
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var obj = testObjects[i % testObjects.Length];
            if (!legacyCache.TryGetValue(obj, out var type))
            {
                type = obj.GetType();
                legacyCache[obj] = type;
            }
        }
        sw1.Stop();

        // Test new optimized approach (FrozenDictionary simulation)
        var optimizedCache = testObjects.ToFrozenDictionary(o => o, o => o.GetType());
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var obj = testObjects[i % testObjects.Length];
            optimizedCache.TryGetValue(obj, out var type);
        }
        sw2.Stop();

        var improvement = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        Console.WriteLine($"📊 Legacy Cache:    {sw1.ElapsedMilliseconds:N0} ms ({sw1.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"🚀 Optimized Cache: {sw2.ElapsedMilliseconds:N0} ms ({sw2.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"⚡ Performance Gain: {improvement:F2}x faster ({(improvement - 1) * 100:F1}% improvement)");
    }

    private static async Task TestExceptionOptimization()
    {
        Console.WriteLine("\n💥 Testing Exception Pre-allocation Optimization");
        Console.WriteLine("================================================");

        const int iterations = 100_000;

        // Test traditional exception allocation
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                throw new ArgumentNullException("request");
            }
            catch (ArgumentNullException)
            {
                // Caught
            }
        }
        sw1.Stop();

        // Test pre-allocated exception (simulation)
        var preallocatedException = new ArgumentNullException("request");
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                throw preallocatedException;
            }
            catch (ArgumentNullException)
            {
                // Caught
            }
        }
        sw2.Stop();

        var improvement = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        Console.WriteLine($"📊 Traditional:     {sw1.ElapsedMilliseconds:N0} ms ({sw1.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"🚀 Pre-allocated:   {sw2.ElapsedMilliseconds:N0} ms ({sw2.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"⚡ Performance Gain: {improvement:F2}x faster ({(improvement - 1) * 100:F1}% improvement)");
    }

    private static async Task TestSIMDHashOptimization()
    {
        Console.WriteLine("\n🔧 Testing SIMD Hash Optimization");
        Console.WriteLine("==================================");

        const int iterations = 100_000;
        var testData = new byte[1024];
        new Random(42).NextBytes(testData);

        // Test traditional hash
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var hash = testData.GetHashCode();
        }
        sw1.Stop();

        // Test SIMD hash (our optimized version)
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var hash = SIMDHelpers.ComputeSIMDHash(testData);
        }
        sw2.Stop();

        var improvement = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        Console.WriteLine($"📊 Traditional Hash: {sw1.ElapsedMilliseconds:N0} ms ({sw1.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"🚀 SIMD Hash:       {sw2.ElapsedMilliseconds:N0} ms ({sw2.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"⚡ Performance Gain: {improvement:F2}x faster ({(improvement - 1) * 100:F1}% improvement)");
        Console.WriteLine($"🔥 SIMD Capabilities: {SIMDHelpers.Capabilities.GetCapabilityString()}");
    }

    private static async Task TestBufferPoolOptimization()
    {
        Console.WriteLine("\n💾 Testing Buffer Pool Optimization");
        Console.WriteLine("===================================");

        const int iterations = 10_000;
        const int bufferSize = 1024;

        // Test without pooling (allocate each time)
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var buffer = new byte[bufferSize];
            // Simulate some work
            buffer[0] = (byte)(i % 256);
        }
        sw1.Stop();

        // Test with optimized pooling
        using var optimizedManager = new OptimizedPooledBufferManager();
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var buffer = optimizedManager.RentBuffer(bufferSize);
            buffer[0] = (byte)(i % 256);
            optimizedManager.ReturnBuffer(buffer);
        }
        sw2.Stop();

        var improvement = (double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds;
        var metrics = optimizedManager.GetMetrics();

        Console.WriteLine($"📊 Direct Allocation: {sw1.ElapsedMilliseconds:N0} ms ({sw1.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"🚀 Pooled Buffers:    {sw2.ElapsedMilliseconds:N0} ms ({sw2.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"⚡ Performance Gain:  {improvement:F2}x faster ({(improvement - 1) * 100:F1}% improvement)");
        Console.WriteLine($"📈 Pool Metrics: {metrics}");
    }

    /// <summary>
    /// Comprehensive performance comparison
    /// </summary>
    public static async Task RunComprehensiveTest()
    {
        Console.WriteLine("🏆 COMPREHENSIVE RELAY OPTIMIZATION ANALYSIS");
        Console.WriteLine("===========================================");

        const int warmupIterations = 1_000;
        const int testIterations = 100_000;

        Console.WriteLine($"🔥 Warming up with {warmupIterations:N0} iterations...");

        // Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            await SimulateRelayRequest();
        }

        Console.WriteLine($"⚡ Running test with {testIterations:N0} iterations...");

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < testIterations; i++)
        {
            await SimulateRelayRequest();
        }
        sw.Stop();

        Console.WriteLine($"✅ Completed {testIterations:N0} operations in {sw.ElapsedMilliseconds:N0} ms");
        Console.WriteLine($"📊 Average: {sw.ElapsedMilliseconds * 1000.0 / testIterations:F3} μs per operation");
        Console.WriteLine($"🚀 Throughput: {testIterations / (sw.ElapsedMilliseconds / 1000.0):F0} operations/second");

        // Memory usage analysis
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false);
        Console.WriteLine($"💾 Memory Usage: {memoryAfter / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"🗑️ Gen 0 Collections: {GC.CollectionCount(0)}");
        Console.WriteLine($"🗑️ Gen 1 Collections: {GC.CollectionCount(1)}");
        Console.WriteLine($"🗑️ Gen 2 Collections: {GC.CollectionCount(2)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async ValueTask SimulateRelayRequest()
    {
        // Simulate type lookup optimization
        var type = typeof(string);
        UltraFastRelay.GetOptimizedTypeInfo<string>();

        // Simulate optimized buffer usage
        var data = new byte[64];
        SIMDHelpers.ComputeSIMDHash(data);

        await ValueTask.CompletedTask;
    }
}

/// <summary>
/// Entry point for running the optimization demo
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && args[0] == "--comprehensive")
            {
                await OptimizedRelayDemo.RunComprehensiveTest();
            }
            else
            {
                await OptimizedRelayDemo.RunDemo();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}