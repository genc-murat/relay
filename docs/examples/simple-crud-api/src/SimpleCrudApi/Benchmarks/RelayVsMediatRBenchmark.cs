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
[SimpleJob]
[RankColumn]
public class RelayVsMediatRBenchmark
{
    private IServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;
    private IMediator _mediator = null!;
    private IUserRepository _repository = null!;

    // Pre-created test data
    private readonly User _testUser = new User
    {
        Id = 1,
        Name = "Test User",
        Email = "test@example.com",
        CreatedAt = DateTime.UtcNow
    };

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        // Common services
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        // Relay setup - properly register Relay with manual handler registration
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
        for (int i = 1; i <= 100; i++)
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

    // Read Operations Benchmarks
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

    [Benchmark]
    public async Task<User[]> Relay_GetUsers()
    {
        var result = await _relay.SendAsync(new GetUsersQuery(1, 10));
        return result.ToArray(); // Materialize the result
    }

    [Benchmark]
    public async Task<User[]> MediatR_GetUsers()
    {
        var result = await _mediator.Send(new MediatRGetUsersQuery(1, 10));
        return result.ToArray(); // Materialize the result
    }

    // Write Operations Benchmarks
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

    [Benchmark]
    public async Task<User?> Relay_UpdateUser()
    {
        return await _relay.SendAsync(new UpdateUserCommand(1, "Updated User", "updated@example.com"));
    }

    [Benchmark]
    public async Task<User?> MediatR_UpdateUser()
    {
        return await _mediator.Send(new MediatRUpdateUserCommand(1, "Updated User", "updated@example.com"));
    }

    // Notification Operations Benchmarks
    [Benchmark]
    public async Task Relay_PublishNotification()
    {
        await _relay.PublishAsync(new UserCreatedNotification(_testUser));
    }

    [Benchmark]
    public async Task MediatR_PublishNotification()
    {
        await _mediator.Publish(new MediatRUserCreatedNotification(_testUser));
    }

    // Batch Operations Benchmarks
    [Benchmark]
    public async Task Relay_BatchOperations()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_relay.SendAsync(new GetUserQuery(i + 1)).AsTask());
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task MediatR_BatchOperations()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(_mediator.Send(new MediatRGetUserQuery(i + 1)));
        }
        await Task.WhenAll(tasks);
    }
}

