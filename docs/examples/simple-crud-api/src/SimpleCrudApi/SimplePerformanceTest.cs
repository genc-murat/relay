using System.Diagnostics;
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

public class SimplePerformanceTest
{
    public static async Task RunSimpleTest()
    {
        Console.WriteLine("🚀 RELAY VS MEDIATR PERFORMANCE BENCHMARK");
        Console.WriteLine("==========================================");

        var services = new ServiceCollection();

        // Common services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Error));
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Relay setup
        services.AddRelay();
        services.AddScoped<UserService>();
        services.AddScoped<UserNotificationHandlers>();
        services.AddRelayHandlers();

        // Add ultra-fast relay - disabled for now
        // services.AddScoped<UltraFastRelay>();

        // MediatR setup
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRUserHandlers).Assembly));
        services.AddScoped<MediatRUserHandlers>();

        var serviceProvider = services.BuildServiceProvider();

        var relay = serviceProvider.GetRequiredService<IRelay>();
        // var ultraFastRelay = serviceProvider.GetRequiredService<UltraFastRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var userService = serviceProvider.GetRequiredService<UserService>();
        var repository = serviceProvider.GetRequiredService<IUserRepository>();

        // Populate test data
        Console.WriteLine("📊 Preparing test data...");
        for (int i = 1; i <= 100; i++)
        {
            await repository.CreateAsync(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                CreatedAt = DateTime.UtcNow
            });
        }

        const int iterations = 1_000_000;
        var query = new GetUserQuery(1);
        var mediatrQuery = new MediatRGetUserQuery(1);

        // Warmup
        Console.WriteLine("🔥 Warming up...");
        for (int i = 0; i < 10000; i++)
        {
            await relay.SendAsync(query);
            // await ultraFastRelay.SendAsync(query);
            await mediator.Send(mediatrQuery);
            await userService.GetUser(query, default);
        }

        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Console.WriteLine($"⚡ Running benchmark with {iterations:N0} iterations...");
        Console.WriteLine();

        // Test Direct Call (baseline)
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await userService.GetUser(query, default);
        }
        stopwatch.Stop();
        var directTime = stopwatch.ElapsedMilliseconds;
        var directMicros = stopwatch.ElapsedMilliseconds * 1000.0 / iterations;
        Console.WriteLine($"🔧 Direct Call:    {directTime:N0} ms ({directMicros:F3} μs/op)");

        // Test Relay
        GC.Collect();
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            await relay.SendAsync(query);
        }
        stopwatch.Stop();
        var relayTime = stopwatch.ElapsedMilliseconds;
        var relayMicros = stopwatch.ElapsedMilliseconds * 1000.0 / iterations;
        Console.WriteLine($"🚀 Relay:          {relayTime:N0} ms ({relayMicros:F3} μs/op)");

        // Test MediatR
        GC.Collect();
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            await mediator.Send(mediatrQuery);
        }
        stopwatch.Stop();
        var mediatrTime = stopwatch.ElapsedMilliseconds;
        var mediatrMicros = stopwatch.ElapsedMilliseconds * 1000.0 / iterations;
        Console.WriteLine($"📨 MediatR:        {mediatrTime:N0} ms ({mediatrMicros:F3} μs/op)");

        Console.WriteLine();
        Console.WriteLine("🏆 PERFORMANCE ANALYSIS");
        Console.WriteLine("=======================");

        var relayOverhead = relayMicros / directMicros;
        var relayVsMediatr = mediatrMicros / relayMicros;

        Console.WriteLine($"📊 Direct Call:    {directMicros:F3} μs/op [BASELINE]");
        Console.WriteLine($"🚀 Relay:          {relayMicros:F3} μs/op ({relayOverhead:F2}x vs direct)");
        Console.WriteLine($"📨 MediatR:        {mediatrMicros:F3} μs/op ({mediatrMicros / directMicros:F2}x vs direct)");
        Console.WriteLine();
        Console.WriteLine($"🔥 Relay is {relayVsMediatr:F1}x faster than MediatR");

        if (relayOverhead <= 1.05)
        {
            Console.WriteLine("⚡ ZERO OVERHEAD: Relay achieves near-direct call performance!");
        }
        else if (relayOverhead <= 1.15)
        {
            Console.WriteLine($"✨ LOW OVERHEAD: Relay only {(relayOverhead - 1) * 100:F1}% vs direct calls");
        }
        else
        {
            Console.WriteLine($"💡 Overhead vs direct calls: {(relayOverhead - 1) * 100:F1}%");
        }

        serviceProvider.Dispose();

        Console.WriteLine();
        Console.WriteLine("✨ Benchmark completed successfully!");
    }
}