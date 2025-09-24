using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay;
using Relay.Core;
using SimpleCrudApi.Data;
using SimpleCrudApi.Models;
using SimpleCrudApi.Models.Requests;
using SimpleCrudApi.Services;
using SimpleCrudApi.MediatR.Requests;
using SimpleCrudApi.MediatR.Handlers;

namespace SimpleCrudApi;

public class QuickPerformanceTest
{
    public static async Task RunTest()
    {
        Console.WriteLine("Setting up services...");

        var services = new ServiceCollection();

        // Common services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Relay setup - use ultra-optimized implementation
        services.AddRelay();
        services.AddScoped<UserService>();
        services.AddScoped<UserNotificationHandlers>();
        services.AddRelayHandlers();

        // Override with ultra-optimized relay
        services.AddScoped<SimpleCrudApi.Optimizations.UltraOptimizedRelay>();

        // MediatR setup
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRUserHandlers).Assembly));
        services.AddScoped<MediatRUserHandlers>();
        services.AddScoped<MediatRUserNotificationHandlers>();

        var serviceProvider = services.BuildServiceProvider();

        // Use ultra-optimized relay for testing
        var relay = serviceProvider.GetRequiredService<SimpleCrudApi.Optimizations.UltraOptimizedRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var repository = serviceProvider.GetRequiredService<IUserRepository>();

        // Pre-populate repository with test data
        Console.WriteLine("Populating test data...");
        for (int i = 1; i <= 10; i++)
        {
            await repository.CreateAsync(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                CreatedAt = DateTime.UtcNow
            });
        }

        Console.WriteLine("Running performance tests...");

        const int iterations = 10000;

        // Warm up
        Console.WriteLine("Warming up...");
        for (int i = 0; i < 100; i++)
        {
            await relay.SendAsync(new GetUserQuery(1));
            await mediator.Send(new MediatRGetUserQuery(1));
        }

        // Test Relay performance
        Console.WriteLine($"Testing Relay with {iterations:N0} iterations...");
        var relayStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await relay.SendAsync(new GetUserQuery(1));
        }
        relayStopwatch.Stop();

        // Test MediatR performance
        Console.WriteLine($"Testing MediatR with {iterations:N0} iterations...");
        var mediatrStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await mediator.Send(new MediatRGetUserQuery(1));
        }
        mediatrStopwatch.Stop();

        // Results
        Console.WriteLine("\n=== PERFORMANCE TEST RESULTS ===");
        Console.WriteLine($"Relay:   {relayStopwatch.ElapsedMilliseconds:N0} ms ({relayStopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"MediatR: {mediatrStopwatch.ElapsedMilliseconds:N0} ms ({mediatrStopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");

        var improvement = (double)mediatrStopwatch.ElapsedMilliseconds / relayStopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Relay is {improvement:F2}x faster than MediatR");

        var savedMs = mediatrStopwatch.ElapsedMilliseconds - relayStopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Time saved: {savedMs:N0} ms ({savedMs / (double)iterations * 1000:F3} μs per operation)");

        serviceProvider.Dispose();
    }
}