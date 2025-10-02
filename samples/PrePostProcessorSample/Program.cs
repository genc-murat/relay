using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core;
using Relay.Core.Pipeline;
using System.Diagnostics;

namespace PrePostProcessorSample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Relay Pre/Post Processor Sample ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Add Relay core services
        services.AddSingleton<IRelay, SimpleRelay>();

        // Add handlers
        services.AddTransient<CreateUserHandler>();
        services.AddTransient<UpdateUserHandler>();

        // Add pre/post processor pipeline behaviors
        services.AddRelayPrePostProcessors();

        // Register pre-processors
        services.AddPreProcessor<CreateUserCommand, LoggingPreProcessor>();
        services.AddPreProcessor<CreateUserCommand, ValidationPreProcessor>();
        services.AddPreProcessor<UpdateUserCommand, LoggingPreProcessor>();

        // Register post-processors
        services.AddPostProcessor<CreateUserCommand, User, AuditPostProcessor>();
        services.AddPostProcessor<CreateUserCommand, User, NotificationPostProcessor>();
        services.AddPostProcessor<UpdateUserCommand, User, AuditPostProcessor>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();

        Console.WriteLine("1. Creating a new user with pre/post processors...\n");
        var createCommand = new CreateUserCommand("John Doe", "john@example.com");
        var user = await relay.SendAsync(createCommand);
        Console.WriteLine($"\n✓ User created: {user.Name} (ID: {user.Id})\n");

        Console.WriteLine(new string('-', 60));

        Console.WriteLine("\n2. Updating user with pre/post processors...\n");
        var updateCommand = new UpdateUserCommand(user.Id, "John Smith", "john.smith@example.com");
        var updatedUser = await relay.SendAsync(updateCommand);
        Console.WriteLine($"\n✓ User updated: {updatedUser.Name} (ID: {updatedUser.Id})\n");

        Console.WriteLine("\n=== Sample Completed ===");
    }
}

#region Models

public record User(int Id, string Name, string Email);

public record CreateUserCommand(string Name, string Email) : IRequest<User>;

public record UpdateUserCommand(int Id, string Name, string Email) : IRequest<User>;

#endregion

#region Handlers

public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    private static int _nextId = 1;

    public ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [Handler] Creating user: {request.Name}");
        var user = new User(_nextId++, request.Name, request.Email);
        return new ValueTask<User>(user);
    }
}

public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, User>
{
    public ValueTask<User> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [Handler] Updating user ID {request.Id}");
        var user = new User(request.Id, request.Name, request.Email);
        return new ValueTask<User>(user);
    }
}

#endregion

#region Pre-Processors

public class LoggingPreProcessor : IRequestPreProcessor<CreateUserCommand>, IRequestPreProcessor<UpdateUserCommand>
{
    public ValueTask ProcessAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PreProcessor - Logging] Request received: CreateUser '{request.Name}'");
        return default;
    }

    public ValueTask ProcessAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PreProcessor - Logging] Request received: UpdateUser ID {request.Id}");
        return default;
    }
}

public class ValidationPreProcessor : IRequestPreProcessor<CreateUserCommand>
{
    public ValueTask ProcessAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PreProcessor - Validation] Validating user data...");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required");

        if (!request.Email.Contains("@"))
            throw new ArgumentException("Invalid email format");

        Console.WriteLine($"  [PreProcessor - Validation] ✓ Validation passed");
        return default;
    }
}

#endregion

#region Post-Processors

public class AuditPostProcessor : IRequestPostProcessor<CreateUserCommand, User>,
                                    IRequestPostProcessor<UpdateUserCommand, User>
{
    public ValueTask ProcessAsync(CreateUserCommand request, User response, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PostProcessor - Audit] User created - ID: {response.Id}, Name: {response.Name}");
        Console.WriteLine($"  [PostProcessor - Audit] Audit log saved at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        return default;
    }

    public ValueTask ProcessAsync(UpdateUserCommand request, User response, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PostProcessor - Audit] User updated - ID: {response.Id}, Name: {response.Name}");
        Console.WriteLine($"  [PostProcessor - Audit] Audit log saved at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        return default;
    }
}

public class NotificationPostProcessor : IRequestPostProcessor<CreateUserCommand, User>
{
    public ValueTask ProcessAsync(CreateUserCommand request, User response, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [PostProcessor - Notification] Sending welcome email to {response.Email}");
        Console.WriteLine($"  [PostProcessor - Notification] ✓ Welcome email queued");
        return default;
    }
}

#endregion

#region Simple Relay Implementation for Demo

public class SimpleRelay : IRelay
{
    private readonly IServiceProvider _serviceProvider;

    public SimpleRelay(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        // Get pre-processor behavior
        var preProcessorBehavior = ActivatorUtilities.CreateInstance<RequestPreProcessorBehavior<IRequest<TResponse>, TResponse>>(_serviceProvider);

        // Get post-processor behavior
        var postProcessorBehavior = ActivatorUtilities.CreateInstance<RequestPostProcessorBehavior<IRequest<TResponse>, TResponse>>(_serviceProvider);

        // Execute pipeline: PreProcessor -> Handler -> PostProcessor
        var response = await preProcessorBehavior.HandleAsync(
            request,
            async () => await postProcessorBehavior.HandleAsync(
                request,
                async () =>
                {
                    // Execute actual handler
                    if (request is CreateUserCommand createCmd)
                    {
                        var handler = _serviceProvider.GetRequiredService<CreateUserHandler>();
                        return (TResponse)(object)(await handler.HandleAsync(createCmd, cancellationToken));
                    }
                    else if (request is UpdateUserCommand updateCmd)
                    {
                        var handler = _serviceProvider.GetRequiredService<UpdateUserHandler>();
                        return (TResponse)(object)(await handler.HandleAsync(updateCmd, cancellationToken));
                    }
                    throw new NotSupportedException($"Handler not found for {request.GetType().Name}");
                },
                cancellationToken),
            cancellationToken);

        return response;
    }

    public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        throw new NotImplementedException();
    }
}

#endregion
