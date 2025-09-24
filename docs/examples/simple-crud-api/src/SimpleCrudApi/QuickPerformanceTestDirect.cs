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

public class QuickPerformanceTestDirect
{
    public static async Task RunTest()
    {
        Console.WriteLine("Setting up services for DIRECT CALL comparison...");

        var services = new ServiceCollection();

        // Common services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Relay setup
        services.AddRelay();
        services.AddScoped<UserService>();
        services.AddScoped<UserNotificationHandlers>();
        services.AddRelayHandlers();

        // MediatR setup
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRUserHandlers).Assembly));
        services.AddScoped<MediatRUserHandlers>();
        services.AddScoped<MediatRUserNotificationHandlers>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var repository = serviceProvider.GetRequiredService<IUserRepository>();
        var userService = serviceProvider.GetRequiredService<UserService>();

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

        const int iterations = 50000;
        var query = new GetUserQuery(1);
        var mediatrQuery = new MediatRGetUserQuery(1);

        // Warm up
        Console.WriteLine("Warming up...");
        for (int i = 0; i < 1000; i++)
        {
            await relay.SendAsync(query);
            await mediator.Send(mediatrQuery);
            await userService.GetUser(query, default);
        }

        // Test Direct Call performance (baseline)
        Console.WriteLine($"Testing DIRECT CALL with {iterations:N0} iterations...");
        var directStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await userService.GetUser(query, default);
        }
        directStopwatch.Stop();

        // Test Relay performance
        Console.WriteLine($"Testing Relay with {iterations:N0} iterations...");
        var relayStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await relay.SendAsync(query);
        }
        relayStopwatch.Stop();

        // Test MediatR performance
        Console.WriteLine($"Testing MediatR with {iterations:N0} iterations...");
        var mediatrStopwatch = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await mediator.Send(mediatrQuery);
        }
        mediatrStopwatch.Stop();

        // Results
        Console.WriteLine("\n=== PERFORMANCE TEST RESULTS ===");
        Console.WriteLine($"Direct:  {directStopwatch.ElapsedMilliseconds:N0} ms ({directStopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op) [BASELINE]");
        Console.WriteLine($"Relay:   {relayStopwatch.ElapsedMilliseconds:N0} ms ({relayStopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine($"MediatR: {mediatrStopwatch.ElapsedMilliseconds:N0} ms ({mediatrStopwatch.ElapsedMilliseconds * 1000.0 / iterations:F3} μs/op)");
        Console.WriteLine();

        var relayOverhead = (double)relayStopwatch.ElapsedMilliseconds / directStopwatch.ElapsedMilliseconds;
        var mediatrOverhead = (double)mediatrStopwatch.ElapsedMilliseconds / directStopwatch.ElapsedMilliseconds;

        Console.WriteLine($"Relay overhead: {relayOverhead:F2}x vs direct calls");
        Console.WriteLine($"MediatR overhead: {mediatrOverhead:F2}x vs direct calls");

        var relayVsMediatr = (double)mediatrStopwatch.ElapsedMilliseconds / relayStopwatch.ElapsedMilliseconds;
        Console.WriteLine($"Relay is {relayVsMediatr:F2}x faster than MediatR");

        serviceProvider.Dispose();
    }
}