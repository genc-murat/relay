using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.Observability.Example
{
    public record ProcessOrderCommand(int OrderId, decimal Amount) : IRequest<OrderResult>;
    public record OrderResult(int OrderId, string Status);

    public class OrderService
    {
        private static int _totalRequests = 0;
        private static int _successCount = 0;
        private static int _failureCount = 0;
        private static long _totalDuration = 0;

        [Handle]
        public async ValueTask<OrderResult> ProcessOrder(ProcessOrderCommand command, CancellationToken ct)
        {
            Interlocked.Increment(ref _totalRequests);
            var sw = Stopwatch.StartNew();

            try
            {
                // Simulate processing
                await Task.Delay(Random.Shared.Next(100, 500), ct);

                // Random failures for demo (10% chance)
                if (Random.Shared.Next(100) < 10)
                {
                    throw new Exception("Processing failed");
                }

                Interlocked.Increment(ref _successCount);
                Interlocked.Add(ref _totalDuration, sw.ElapsedMilliseconds);
                return new OrderResult(command.OrderId, "Success");
            }
            catch
            {
                Interlocked.Increment(ref _failureCount);
                throw;
            }
        }

        public static void PrintMetrics()
        {
            var avgDuration = _totalRequests > 0 ? _totalDuration / (double)_totalRequests : 0;
            var successRate = _totalRequests > 0 ? (_successCount / (double)_totalRequests) * 100 : 0;

            Console.WriteLine("📊 Metrics Summary:");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine($"  Total Requests:     {_totalRequests}");
            Console.WriteLine($"  Successful:         {_successCount}");
            Console.WriteLine($"  Failed:             {_failureCount}");
            Console.WriteLine($"  Success Rate:       {successRate:F1}%");
            Console.WriteLine($"  Avg Response Time:  {avgDuration:F2}ms");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Observability Demo");
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine();
            Console.WriteLine("⚠️  Note: This demonstrates observability CONCEPTS");
            Console.WriteLine("    Full metrics require RelayMetrics implementation");
            Console.WriteLine();

            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddScoped<OrderService>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            Console.WriteLine("Processing 50 orders with metrics collection...");
            Console.WriteLine();

            for (int i = 1; i <= 50; i++)
            {
                try
                {
                    var command = new ProcessOrderCommand(i, Random.Shared.Next(10, 1000));
                    await relay.SendAsync(command);
                    Console.Write(".");
                }
                catch
                {
                    Console.Write("X");
                }

                if (i % 10 == 0)
                {
                    Console.WriteLine($" ({i}/50)");
                }
            }

            Console.WriteLine();
            Console.WriteLine();

            OrderService.PrintMetrics();

            Console.WriteLine();
            Console.WriteLine("✅ Observability demo completed!");
            Console.WriteLine();
            Console.WriteLine("💡 For production observability:");
            Console.WriteLine("  • Implement RelayMetrics service");
            Console.WriteLine("  • Add OpenTelemetry integration");
            Console.WriteLine("  • Configure distributed tracing");
            Console.WriteLine("  • Add custom metrics collection");
            Console.WriteLine("  • Set up health checks");
        }
    }
}
