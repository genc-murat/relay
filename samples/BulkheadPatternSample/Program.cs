using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.BulkheadDemo.Example
{
    // Sample requests
    public record ProcessDataCommand(int Id, string Data) : IRequest<ProcessResult>;
    public record ProcessResult(int Id, string Status, TimeSpan ProcessingTime);

    // Simulate a resource-intensive operation
    public class DataProcessingService
    {
        private static int _activeRequests = 0;
        private const int MaxConcurrent = 5;

        [Handle]
        public async ValueTask<ProcessResult> ProcessData(ProcessDataCommand command, CancellationToken cancellationToken)
        {
            // Simple concurrency control (demonstrates concept)
            var current = Interlocked.Increment(ref _activeRequests);
            
            if (current > MaxConcurrent)
            {
                Interlocked.Decrement(ref _activeRequests);
                throw new InvalidOperationException($"Bulkhead limit exceeded: {current} > {MaxConcurrent}");
            }

            try
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"  🔧 Processing request #{command.Id} (Active: {current})");

                // Simulate heavy processing
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                var processingTime = DateTime.UtcNow - startTime;
                Console.WriteLine($"  ✅ Completed request #{command.Id} in {processingTime.TotalMilliseconds:F0}ms");

                return new ProcessResult(command.Id, "Success", processingTime);
            }
            finally
            {
                Interlocked.Decrement(ref _activeRequests);
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Bulkhead Pattern Demo");
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine();
            Console.WriteLine("⚠️  Note: This demonstrates bulkhead CONCEPT with simple concurrency control");
            Console.WriteLine("    Full bulkhead requires BulkheadPipelineBehavior configuration");
            Console.WriteLine();
            Console.WriteLine("Bulkhead Pattern limits concurrent requests to prevent resource exhaustion");
            Console.WriteLine();

            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddScoped<DataProcessingService>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            // Example 1: Normal load (within limits)
            Console.WriteLine("📊 Example 1: Normal Load (within limit of 5)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Sending 5 concurrent requests...");
            Console.WriteLine();

            var normalLoadTasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                var id = i + 1;
                normalLoadTasks[i] = Task.Run(async () => await relay.SendAsync(new ProcessDataCommand(id, $"Data{id}")));
            }

            await Task.WhenAll(normalLoadTasks);
            
            // Get results (simplified)
            Console.WriteLine($"\n✅ All 5 requests completed successfully");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 2: Overload scenario
            Console.WriteLine("📊 Example 2: Overload Scenario (exceeds limit)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Sending 10 concurrent requests (max 5 allowed)...");
            Console.WriteLine();

            var overloadTasks = new Task<ProcessResult?>[10];
            var successCount = 0;
            var rejectedCount = 0;

            for (int i = 0; i < 10; i++)
            {
                var id = i + 1;
                overloadTasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        var result = await relay.SendAsync(new ProcessDataCommand(id + 100, $"Data{id}"));
                        Interlocked.Increment(ref successCount);
                        return result;
                    }
                    catch (InvalidOperationException ex)
                    {
                        Interlocked.Increment(ref rejectedCount);
                        Console.WriteLine($"  ⚠️  Request #{id + 100} REJECTED: {ex.Message}");
                        return null;
                    }
                });
            }

            await Task.WhenAll(overloadTasks);

            Console.WriteLine();
            Console.WriteLine($"✅ Bulkhead Protection Results:");
            Console.WriteLine($"   Total requests: 10");
            Console.WriteLine($"   Successful: {successCount}");
            Console.WriteLine($"   Rejected: {rejectedCount} (system protected!)");

            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine("✅ Bulkhead pattern demo completed!");
            Console.WriteLine();
            Console.WriteLine("Key Concepts Demonstrated:");
            Console.WriteLine("  • Concurrency limiting");
            Console.WriteLine("  • Fast-fail for excess load");
            Console.WriteLine("  • Resource isolation");
            Console.WriteLine("  • System stability under high load");
            Console.WriteLine();
            Console.WriteLine("💡 For production bulkhead:");
            Console.WriteLine("  • Configure BulkheadPipelineBehavior");
            Console.WriteLine("  • Set MaxConcurrentRequests per handler type");
            Console.WriteLine("  • Add request queuing support");
            Console.WriteLine("  • Monitor bulkhead metrics");
        }
    }
}
