using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;

namespace Relay.Streaming.Example
{
    // Streaming request - returns multiple results asynchronously
    public record StreamLogsQuery(DateTime StartDate, DateTime EndDate, string? Level = null) : IStreamRequest<LogEntry>;

    // Response model
    public record LogEntry(DateTime Timestamp, string Level, string Message, string Source);

    // Another streaming example - large dataset pagination
    public record StreamUsersQuery(int PageSize = 100) : IStreamRequest<User>;

    public record User(int Id, string Name, string Email);

    // Streaming handler for logs
    public class LogService
    {
        [Handle]
        public async IAsyncEnumerable<LogEntry> StreamLogs(
            StreamLogsQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Console.WriteLine($"🔄 Starting to stream logs from {query.StartDate:yyyy-MM-dd} to {query.EndDate:yyyy-MM-dd}");

            // Simulate streaming from a large log file or database
            var random = new Random();
            var levels = new[] { "INFO", "WARN", "ERROR", "DEBUG" };
            var sources = new[] { "API", "Database", "Cache", "Queue" };

            var currentDate = query.StartDate;
            int count = 0;

            while (currentDate <= query.EndDate && !cancellationToken.IsCancellationRequested)
            {
                // Simulate processing delay
                await Task.Delay(50, cancellationToken);

                var level = levels[random.Next(levels.Length)];
                
                // Apply filter if specified
                if (query.Level == null || level == query.Level)
                {
                    var logEntry = new LogEntry(
                        currentDate,
                        level,
                        $"Sample log message {count++}",
                        sources[random.Next(sources.Length)]
                    );

                    yield return logEntry;
                }

                currentDate = currentDate.AddMinutes(random.Next(1, 30));
            }

            Console.WriteLine($"✅ Finished streaming {count} log entries");
        }
    }

    // Streaming handler for users with backpressure handling
    public class UserService
    {
        [Handle]
        public async IAsyncEnumerable<User> StreamUsers(
            StreamUsersQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Console.WriteLine($"🔄 Starting to stream users (page size: {query.PageSize})");

            int totalUsers = 1000; // Simulate large dataset
            int page = 0;

            while (page * query.PageSize < totalUsers && !cancellationToken.IsCancellationRequested)
            {
                // Simulate database page fetch
                await Task.Delay(100, cancellationToken);

                var startId = page * query.PageSize;
                var endId = Math.Min((page + 1) * query.PageSize, totalUsers);

                for (int i = startId; i < endId; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Console.WriteLine("⚠️  Streaming cancelled by consumer");
                        yield break;
                    }

                    yield return new User(
                        i + 1,
                        $"User{i + 1}",
                        $"user{i + 1}@example.com"
                    );
                }

                page++;
                Console.WriteLine($"  Streamed page {page} ({endId}/{totalUsers} users)");
            }

            Console.WriteLine("✅ Finished streaming all users");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Relay Streaming Sample");
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine();

            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                services.AddRelayConfiguration();
                services.AddScoped<LogService>();
                services.AddScoped<UserService>();
            });

            var host = builder.Build();
            var relay = host.Services.GetRequiredService<IRelay>();

            // Example 1: Stream logs with filter
            Console.WriteLine("📋 Example 1: Streaming Logs (ERROR level only)");
            Console.WriteLine("-" + new string('-', 50));

            var logQuery = new StreamLogsQuery(
                DateTime.Now.AddHours(-2),
                DateTime.Now,
                "ERROR"
            );

            var errorCount = 0;
            await foreach (var log in relay.StreamAsync(logQuery))
            {
                Console.WriteLine($"  [{log.Timestamp:HH:mm:ss}] {log.Level} - {log.Message} (Source: {log.Source})");
                errorCount++;

                // Example: Stop after first 5 errors
                if (errorCount >= 5)
                {
                    Console.WriteLine("  ... stopping after 5 errors");
                    break;
                }
            }

            Console.WriteLine();

            // Example 2: Stream all logs (no filter)
            Console.WriteLine("📋 Example 2: Streaming All Logs (limited to 10)");
            Console.WriteLine("-" + new string('-', 50));

            var allLogsQuery = new StreamLogsQuery(
                DateTime.Now.AddHours(-1),
                DateTime.Now
            );

            var logCount = 0;
            await foreach (var log in relay.StreamAsync(allLogsQuery))
            {
                Console.WriteLine($"  [{log.Timestamp:HH:mm:ss}] {log.Level} - {log.Message}");
                logCount++;

                if (logCount >= 10)
                {
                    Console.WriteLine("  ... showing first 10 only");
                    break;
                }
            }

            Console.WriteLine();

            // Example 3: Stream users with cancellation
            Console.WriteLine("📋 Example 3: Streaming Users (with early cancellation)");
            Console.WriteLine("-" + new string('-', 50));

            var userQuery = new StreamUsersQuery(PageSize: 50);
            var cts = new CancellationTokenSource();

            var userTask = Task.Run(async () =>
            {
                var count = 0;
                await foreach (var user in relay.StreamAsync(userQuery, cts.Token))
                {
                    if (count < 5)
                        Console.WriteLine($"  User: {user.Name} ({user.Email})");
                    count++;

                    // Cancel after processing 150 users
                    if (count >= 150)
                    {
                        Console.WriteLine("  ... processed 150 users, cancelling stream");
                        cts.Cancel();
                        break;
                    }
                }
                return count;
            });

            var processedUsers = await userTask;
            Console.WriteLine($"  Total processed: {processedUsers} users");

            Console.WriteLine();

            // Example 4: Backpressure demonstration
            Console.WriteLine("📋 Example 4: Backpressure Handling (slow consumer)");
            Console.WriteLine("-" + new string('-', 50));

            var backpressureQuery = new StreamUsersQuery(PageSize: 20);
            var slowCount = 0;

            await foreach (var user in relay.StreamAsync(backpressureQuery))
            {
                // Simulate slow consumer processing
                await Task.Delay(200);
                
                if (slowCount < 10)
                    Console.WriteLine($"  Processing: {user.Name}");
                
                slowCount++;

                if (slowCount >= 10)
                {
                    Console.WriteLine("  ... stopping slow consumer after 10 users");
                    break;
                }
            }

            Console.WriteLine();
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine("✅ All streaming examples completed!");
            Console.WriteLine();
            Console.WriteLine("Key Features Demonstrated:");
            Console.WriteLine("  • IAsyncEnumerable<T> streaming support");
            Console.WriteLine("  • Backpressure handling (producer waits for consumer)");
            Console.WriteLine("  • Cancellation token support");
            Console.WriteLine("  • Large dataset pagination");
            Console.WriteLine("  • Memory efficient processing");
        }
    }
}

