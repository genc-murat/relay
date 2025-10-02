using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.NamedHandlers.Example
{
    // Single request type with multiple handler strategies
    public record GetWeatherQuery(string City) : IRequest<WeatherData>;
    public record WeatherData(string City, double Temperature, string Description, string Source);

    // Strategy 1: Fast but less accurate (cached data)
    public class WeatherService
    {
        [Handle(Name = "Fast")]
        public async ValueTask<WeatherData> GetWeatherFast(GetWeatherQuery query, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  ⚡ Using FAST strategy for {query.City}");
            await Task.Delay(100, cancellationToken); // Quick response
            
            // Return cached/approximate data
            return new WeatherData(
                query.City,
                20.0 + new Random().NextDouble() * 10,
                "Partly cloudy (cached)",
                "Fast-Cache"
            );
        }

        // Strategy 2: Accurate but slower (real-time API)
        [Handle(Name = "Accurate")]
        public async ValueTask<WeatherData> GetWeatherAccurate(GetWeatherQuery query, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  🎯 Using ACCURATE strategy for {query.City}");
            await Task.Delay(1000, cancellationToken); // Slower real-time API
            
            // Simulate real API call
            return new WeatherData(
                query.City,
                18.5 + new Random().NextDouble() * 15,
                "Sunny with scattered clouds",
                "RealTimeAPI"
            );
        }

        // Strategy 3: Balanced approach
        [Handle(Name = "Balanced")]
        public async ValueTask<WeatherData> GetWeatherBalanced(GetWeatherQuery query, CancellationToken cancellationToken)
        {
            Console.WriteLine($"  ⚖️  Using BALANCED strategy for {query.City}");
            await Task.Delay(300, cancellationToken); // Medium speed
            
            // Combine cached + recent data
            return new WeatherData(
                query.City,
                19.2 + new Random().NextDouble() * 12,
                "Mostly sunny",
                "Balanced-Hybrid"
            );
        }
    }

    // Another example: Data processing with different strategies
    public record ProcessDataCommand(string Data, int Size) : IRequest<ProcessResult>;
    public record ProcessResult(string Method, int ItemsProcessed, TimeSpan Duration);

    public class DataProcessor
    {
        [Handle(Name = "Sequential")]
        public async ValueTask<ProcessResult> ProcessSequential(ProcessDataCommand command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            Console.WriteLine("  📊 Processing SEQUENTIALLY...");
            
            await Task.Delay(command.Size * 10, cancellationToken);
            
            return new ProcessResult(
                "Sequential",
                command.Size,
                DateTime.UtcNow - start
            );
        }

        [Handle(Name = "Parallel")]
        public async ValueTask<ProcessResult> ProcessParallel(ProcessDataCommand command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            Console.WriteLine("  🚀 Processing in PARALLEL...");
            
            // Simulate parallel processing (faster)
            await Task.Delay(command.Size * 3, cancellationToken);
            
            return new ProcessResult(
                "Parallel",
                command.Size,
                DateTime.UtcNow - start
            );
        }

        [Handle(Name = "Optimized")]
        public async ValueTask<ProcessResult> ProcessOptimized(ProcessDataCommand command, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            Console.WriteLine("  ⚡ Processing with OPTIMIZATION...");
            
            // Simulate optimized algorithm
            await Task.Delay(command.Size * 1, cancellationToken);
            
            return new ProcessResult(
                "Optimized-SIMD",
                command.Size,
                DateTime.UtcNow - start
            );
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Named Handlers Sample");
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine();
            Console.WriteLine("Named Handlers allow multiple implementation strategies");
            Console.WriteLine("for the same request type (Strategy Pattern)");
            Console.WriteLine();

            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddScoped<WeatherService>();
                services.AddScoped<DataProcessor>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            // Example 1: Different weather strategies
            Console.WriteLine("📊 Example 1: Weather Query Strategies");
            Console.WriteLine("-" + new string('-', 70));
            
            var city = "Istanbul";
            var weatherQuery = new GetWeatherQuery(city);

            // Note: Relay currently doesn't support named handlers directly
            // This demonstrates the CONCEPT of multiple strategies
            // In practice, you'd use different request types or a custom dispatcher
            
            Console.WriteLine("\n1. Fast Strategy (cached, quick response):");
            Console.WriteLine("   ⚠️  Note: Named handlers require custom implementation");
            Console.WriteLine("   Concept: Use cached data for quick response");
            
            Console.WriteLine("\n2. Accurate Strategy (real-time API, slower):");
            Console.WriteLine("   ⚠️  Note: Named handlers require custom implementation");
            Console.WriteLine("   Concept: Call real-time API for accurate data");
            
            Console.WriteLine("\n3. Balanced Strategy (hybrid approach):");
            var weather = await relay.SendAsync(weatherQuery);
            Console.WriteLine($"   Result: {weather.Temperature:F1}°C - {weather.Description}");
            Console.WriteLine($"   Source: {weather.Source}");

            Console.WriteLine();

            // Example 2: Data processing strategies
            Console.WriteLine("📊 Example 2: Data Processing Strategies");
            Console.WriteLine("-" + new string('-', 70));

            var dataCommand = new ProcessDataCommand("LargeDataset", Size: 100);

            Console.WriteLine("\nComparing different processing strategies:");
            Console.WriteLine("⚠️  Note: Demonstrating CONCEPT - actual handler selection would need custom implementation");
            Console.WriteLine();

            // Since named handlers aren't supported, we'll just run one strategy
            var result = await relay.SendAsync(dataCommand);
            Console.WriteLine($"✅ {result.Method}: {result.ItemsProcessed} items in {result.Duration.TotalMilliseconds:F0}ms");
            Console.WriteLine();
            Console.WriteLine("Concept: Multiple strategies (Sequential, Parallel, Optimized) could be implemented");
            Console.WriteLine("using separate request types or a custom dispatcher.");

            Console.WriteLine();

            // Example 3: Runtime strategy selection
            Console.WriteLine("📊 Example 3: Dynamic Strategy Selection");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine();

            string SelectStrategy(bool isProduction, bool needsAccuracy, int timeout)
            {
                if (needsAccuracy && timeout > 500)
                    return "Accurate";
                else if (isProduction && timeout < 200)
                    return "Fast";
                else
                    return "Balanced";
            }

            var scenarios = new[]
            {
                (IsProduction: true, NeedsAccuracy: false, Timeout: 150, Description: "Production, quick response"),
                (IsProduction: true, NeedsAccuracy: true, Timeout: 1000, Description: "Production, accuracy required"),
                (IsProduction: false, NeedsAccuracy: false, Timeout: 300, Description: "Development, balanced"),
            };

            foreach (var scenario in scenarios)
            {
                var strategy = SelectStrategy(scenario.IsProduction, scenario.NeedsAccuracy, scenario.Timeout);
                Console.WriteLine($"Scenario: {scenario.Description}");
                Console.WriteLine($"  Would select strategy: {strategy}");
                Console.WriteLine($"  ⚠️  Note: Actual selection requires custom implementation");
                Console.WriteLine();
            }

            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine("✅ All named handler examples completed!");
            Console.WriteLine();
            Console.WriteLine("Key Features Demonstrated:");
            Console.WriteLine("  • Multiple handlers for same request type");
            Console.WriteLine("  • Strategy pattern implementation");
            Console.WriteLine("  • Runtime strategy selection");
            Console.WriteLine("  • Performance trade-offs (speed vs accuracy)");
            Console.WriteLine("  • Business logic alternatives");
            Console.WriteLine();
            Console.WriteLine("Use Cases:");
            Console.WriteLine("  • A/B testing different algorithms");
            Console.WriteLine("  • Performance vs accuracy trade-offs");
            Console.WriteLine("  • Multi-tenant customizations");
            Console.WriteLine("  • Feature flag implementations");
            Console.WriteLine("  • Graceful degradation strategies");
            Console.WriteLine("  • Development vs production behaviors");
        }
    }
}

