using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.Resilience;

namespace Relay.CircuitBreaker.Example
{
    // Sample requests
    public record CallExternalApiCommand(string Endpoint) : IRequest<ApiResponse>;
    public record ApiResponse(bool Success, string Data, int AttemptNumber);

    // Simulates an unreliable external service
    public class ExternalApiService
    {
        private int _attemptCount = 0;
        private bool _serviceHealthy = true;

        [Handle]
        public async ValueTask<ApiResponse> CallExternalApi(CallExternalApiCommand command, CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _attemptCount);
            
            Console.WriteLine($"  📡 Calling {command.Endpoint} (attempt #{attempt})");

            // Simulate network delay
            await Task.Delay(100, cancellationToken);

            // Simulate service failures (fails 70% of time when unhealthy)
            if (!_serviceHealthy && new Random().Next(100) < 70)
            {
                Console.WriteLine($"  ❌ API call failed (service unhealthy)");
                throw new Exception("External service unavailable");
            }

            Console.WriteLine($"  ✅ API call succeeded");
            return new ApiResponse(true, $"Data from {command.Endpoint}", attempt);
        }

        public void SetServiceHealth(bool healthy)
        {
            _serviceHealthy = healthy;
            Console.WriteLine($"\n⚙️  External service health set to: {(healthy ? "HEALTHY" : "UNHEALTHY")}\n");
        }

        public int GetAttemptCount() => _attemptCount;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Circuit Breaker Pattern Sample");
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine();
            Console.WriteLine("Circuit Breaker prevents cascading failures by monitoring");
            Console.WriteLine("failure rates and temporarily blocking requests to failing services.");
            Console.WriteLine();

            // Setup host with Circuit Breaker configuration
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddSingleton<ExternalApiService>();

                // Configure Circuit Breaker options
                services.Configure<CircuitBreakerOptions>(options =>
                {
                    options.FailureThreshold = 0.5;        // Open circuit at 50% failure rate
                    options.MinimumThroughput = 5;         // Minimum 5 requests before evaluating
                    options.OpenCircuitDuration = TimeSpan.FromSeconds(3);  // Stay open for 3 seconds
                    options.SamplingDuration = TimeSpan.FromSeconds(10);    // Evaluate over 10 second window
                });

                // Add Circuit Breaker pipeline behavior
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CircuitBreakerPipelineBehavior<,>));
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();
            var apiService = host.Services.GetRequiredService<ExternalApiService>();

            // Example 1: Normal operation (service healthy)
            Console.WriteLine("📊 Example 1: Normal Operation (Circuit Closed)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Service is healthy - all requests should succeed");
            Console.WriteLine();

            apiService.SetServiceHealth(true);

            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    var result = await relay.SendAsync(new CallExternalApiCommand($"/api/users/{i}"));
                    Console.WriteLine($"✅ Request {i}: {result.Data}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Request {i} failed: {ex.Message}");
                }
                await Task.Delay(200);
            }

            Console.WriteLine("\n✅ Circuit remains CLOSED (all requests successful)");
            Console.WriteLine();
            await Task.Delay(1000);

            // Example 2: Service degradation - Circuit opens
            Console.WriteLine("📊 Example 2: Service Degradation (Circuit Opens)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Service becomes unhealthy - circuit breaker will open");
            Console.WriteLine();

            apiService.SetServiceHealth(false);

            var successCount = 0;
            var failureCount = 0;
            var circuitOpenCount = 0;

            // Send requests until circuit opens
            for (int i = 1; i <= 15; i++)
            {
                try
                {
                    var result = await relay.SendAsync(new CallExternalApiCommand($"/api/data/{i}"));
                    successCount++;
                    Console.WriteLine($"✅ Request {i}: Success");
                }
                catch (CircuitBreakerOpenException)
                {
                    circuitOpenCount++;
                    Console.WriteLine($"🔴 Request {i}: CIRCUIT OPEN - Request blocked (fast fail)");
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"❌ Request {i}: Failed - {ex.Message}");
                }
                await Task.Delay(200);
            }

            Console.WriteLine();
            Console.WriteLine($"📈 Results:");
            Console.WriteLine($"   Successful: {successCount}");
            Console.WriteLine($"   Failed: {failureCount}");
            Console.WriteLine($"   Circuit Open (blocked): {circuitOpenCount}");
            Console.WriteLine();
            Console.WriteLine("✅ Circuit is now OPEN (protecting the system)");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 3: Half-open state and recovery
            Console.WriteLine("📊 Example 3: Circuit Recovery (Half-Open → Closed)");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine("Waiting for circuit to enter half-open state...");
            Console.WriteLine();

            // Wait for circuit to move to half-open
            await Task.Delay(3500); // OpenCircuitDuration + buffer

            Console.WriteLine("Circuit is now HALF-OPEN (testing with probe requests)");
            Console.WriteLine("Restoring service health...");
            Console.WriteLine();

            apiService.SetServiceHealth(true);

            // Send probe requests
            for (int i = 1; i <= 5; i++)
            {
                try
                {
                    var result = await relay.SendAsync(new CallExternalApiCommand($"/api/probe/{i}"));
                    Console.WriteLine($"✅ Probe request {i}: Success - {result.Data}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Probe request {i}: Failed - {ex.Message}");
                }
                await Task.Delay(300);
            }

            Console.WriteLine();
            Console.WriteLine("✅ Circuit is now CLOSED again (service recovered)");

            Console.WriteLine();
            await Task.Delay(1000);

            // Example 4: Circuit breaker states visualization
            Console.WriteLine("📊 Example 4: Circuit Breaker State Transitions");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine();
            Console.WriteLine("State Diagram:");
            Console.WriteLine("  ┌─────────┐");
            Console.WriteLine("  │ CLOSED  │ ◄──────────────────┐");
            Console.WriteLine("  └────┬────┘                    │");
            Console.WriteLine("       │ Failure threshold       │ Success");
            Console.WriteLine("       │ exceeded                │ threshold");
            Console.WriteLine("       ▼                         │ met");
            Console.WriteLine("  ┌─────────┐                    │");
            Console.WriteLine("  │  OPEN   │                    │");
            Console.WriteLine("  └────┬────┘                    │");
            Console.WriteLine("       │ Timeout expires         │");
            Console.WriteLine("       ▼                         │");
            Console.WriteLine("  ┌──────────┐                   │");
            Console.WriteLine("  │ HALF-OPEN│───────────────────┘");
            Console.WriteLine("  └──────────┘");
            Console.WriteLine();

            Console.WriteLine("State Behaviors:");
            Console.WriteLine();
            Console.WriteLine("  CLOSED (Normal Operation):");
            Console.WriteLine("    • All requests are forwarded to the service");
            Console.WriteLine("    • Monitors failure rate");
            Console.WriteLine("    • Opens if failure threshold exceeded");
            Console.WriteLine();

            Console.WriteLine("  OPEN (Circuit Tripped):");
            Console.WriteLine("    • All requests fail fast (CircuitBreakerOpenException)");
            Console.WriteLine("    • No requests reach the failing service");
            Console.WriteLine("    • System is protected from cascading failures");
            Console.WriteLine("    • Transitions to HALF-OPEN after timeout");
            Console.WriteLine();

            Console.WriteLine("  HALF-OPEN (Testing Recovery):");
            Console.WriteLine("    • Limited requests allowed (probe requests)");
            Console.WriteLine("    • Success → CLOSED (service recovered)");
            Console.WriteLine("    • Failure → OPEN (service still unhealthy)");
            Console.WriteLine();

            // Example 5: Benefits demonstration
            Console.WriteLine("📊 Example 5: Circuit Breaker Benefits");
            Console.WriteLine("-" + new string('-', 70));
            Console.WriteLine();

            Console.WriteLine("Without Circuit Breaker:");
            Console.WriteLine("  ❌ Continuous requests to failing service");
            Console.WriteLine("  ❌ Thread pool exhaustion");
            Console.WriteLine("  ❌ Increased latency (timeout for each request)");
            Console.WriteLine("  ❌ Resource waste");
            Console.WriteLine("  ❌ Cascading failures to dependent services");
            Console.WriteLine();

            Console.WriteLine("With Circuit Breaker:");
            Console.WriteLine("  ✅ Fast-fail when service is unhealthy");
            Console.WriteLine("  ✅ Immediate response (no timeout wait)");
            Console.WriteLine("  ✅ Resource conservation");
            Console.WriteLine("  ✅ Prevents cascading failures");
            Console.WriteLine("  ✅ Automatic recovery detection");
            Console.WriteLine("  ✅ Graceful degradation");

            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 70));
            Console.WriteLine("✅ All circuit breaker examples completed!");
            Console.WriteLine();
            Console.WriteLine($"📊 Total API calls attempted: {apiService.GetAttemptCount()}");
            Console.WriteLine();
            Console.WriteLine("Key Features Demonstrated:");
            Console.WriteLine("  • Automatic failure detection");
            Console.WriteLine("  • Circuit state transitions (Closed → Open → Half-Open)");
            Console.WriteLine("  • Fast-fail when circuit is open");
            Console.WriteLine("  • Automatic recovery testing");
            Console.WriteLine("  • Configurable failure thresholds");
            Console.WriteLine("  • Configurable timeout durations");
            Console.WriteLine("  • Protection against cascading failures");
            Console.WriteLine();
            Console.WriteLine("Configuration Used:");
            Console.WriteLine("  • FailureThreshold: 50%");
            Console.WriteLine("  • MinimumThroughput: 5 requests");
            Console.WriteLine("  • OpenCircuitDuration: 3 seconds");
            Console.WriteLine("  • SamplingDuration: 10 seconds");
            Console.WriteLine();
            Console.WriteLine("Use Cases:");
            Console.WriteLine("  • Protecting external API calls");
            Console.WriteLine("  • Database connection resilience");
            Console.WriteLine("  • Microservice communication");
            Console.WriteLine("  • Third-party service integration");
            Console.WriteLine("  • Distributed system stability");
        }
    }
}

