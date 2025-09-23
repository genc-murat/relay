using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
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

namespace SimpleCrudApi.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(iterationCount: 3, warmupCount: 3)] // Shorter run
[RankColumn]
public class QuickBenchmark
{
    private IServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;
    private IMediator _mediator = null!;
    private IUserRepository _repository = null!;

    [GlobalSetup]
    public void Setup()
    {
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

        _serviceProvider = services.BuildServiceProvider();
        _relay = _serviceProvider.GetRequiredService<IRelay>();
        _mediator = _serviceProvider.GetRequiredService<IMediator>();
        _repository = _serviceProvider.GetRequiredService<IUserRepository>();

        // Pre-populate repository with test data
        PopulateTestData();
    }

    private void PopulateTestData()
    {
        for (int i = 1; i <= 10; i++)
        {
            _repository.CreateAsync(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                CreatedAt = DateTime.UtcNow
            }).AsTask().Wait();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    // Single User Operations
    [Benchmark]
    public async Task<User?> Relay_GetUser()
    {
        return await _relay.SendAsync(new GetUserQuery(1));
    }

    [Benchmark]
    public async Task<User?> MediatR_GetUser()
    {
        return await _mediator.Send(new MediatRGetUserQuery(1));
    }

    // Batch Operations (smaller set)
    [Benchmark]
    public async Task<User[]> Relay_GetUsers()
    {
        var result = await _relay.SendAsync(new GetUsersQuery(1, 5));
        return result.ToArray();
    }

    [Benchmark]
    public async Task<User[]> MediatR_GetUsers()
    {
        var result = await _mediator.Send(new MediatRGetUsersQuery(1, 5));
        return result.ToArray();
    }

    // Create Operations
    [Benchmark]
    public async Task<User> Relay_CreateUser()
    {
        return await _relay.SendAsync(new CreateUserCommand($"New User {Random.Shared.Next()}", "new@example.com"));
    }

    [Benchmark]
    public async Task<User> MediatR_CreateUser()
    {
        return await _mediator.Send(new MediatRCreateUserCommand($"New User {Random.Shared.Next()}", "new@example.com"));
    }
}