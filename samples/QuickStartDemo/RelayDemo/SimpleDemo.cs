using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.Core;

namespace RelayDemo;

// Simple demo without source generator (uses fallback dispatcher)
public class SimpleDemo
{
    // 1. Define requests and responses
    public record GetUserQuery(int UserId) : IRequest<User>;
    public record CreateUserCommand(string Name, string Email) : IRequest<User>;

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // 2. Define handlers using traditional interface approach
    public class GetUserHandler : IRequestHandler<GetUserQuery, User>
    {
        private readonly ILogger<GetUserHandler> _logger;

        public GetUserHandler(ILogger<GetUserHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting user with ID: {UserId}", request.UserId);
            
            await Task.Delay(100, cancellationToken);
            
            return new User 
            { 
                Id = request.UserId, 
                Name = $"User {request.UserId}", 
                Email = $"user{request.UserId}@example.com" 
            };
        }
    }

    public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
    {
        private readonly ILogger<CreateUserHandler> _logger;
        private static int _nextId = 1;

        public CreateUserHandler(ILogger<CreateUserHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating user: {UserName}", request.Name);
            
            await Task.Delay(200, cancellationToken);
            
            return new User 
            { 
                Id = _nextId++, 
                Name = request.Name, 
                Email = request.Email 
            };
        }
    }

    public static async Task RunAsync()
    {
        Console.WriteLine("🚀 Relay Framework Simple Demo (Fallback Dispatcher)");
        Console.WriteLine("=====================================================");

        // Configure services
        var services = new ServiceCollection();
        
        // Add Relay framework components manually (since AddRelay is in different project)
        services.AddTransient<IRelay, RelayImplementation>();
        services.AddTransient<IRequestDispatcher, FallbackRequestDispatcher>();
        services.AddTransient<IStreamDispatcher, StreamDispatcher>();
        services.AddTransient<INotificationDispatcher, NotificationDispatcher>();
        
        // Register handlers manually
        services.AddTransient<IRequestHandler<GetUserQuery, User>, GetUserHandler>();
        services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
        
        // Add logging
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var logger = serviceProvider.GetRequiredService<ILogger<SimpleDemo>>();

        try
        {
            Console.WriteLine("\n📝 Testing Request/Response Pattern:");
            Console.WriteLine("-----------------------------------");

            // Create a user
            var createCommand = new CreateUserCommand("Murat Genc", "murat@example.com");
            var createdUser = await relay.SendAsync(createCommand);
            Console.WriteLine($"✅ Created user: {createdUser.Name} (ID: {createdUser.Id})");

            // Get the user
            var getUserQuery = new GetUserQuery(createdUser.Id);
            var retrievedUser = await relay.SendAsync(getUserQuery);
            Console.WriteLine($"✅ Retrieved user: {retrievedUser.Name} (Email: {retrievedUser.Email})");

            Console.WriteLine("\n🔥 Performance Test (Fallback Dispatcher):");
            Console.WriteLine("------------------------------------------");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            const int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var query = new GetUserQuery(1);
                await relay.SendAsync(query);
            }

            stopwatch.Stop();
            var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
            Console.WriteLine($"✅ Processed {iterations} requests in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"✅ Average time per request: {avgTime:F3}ms");
            Console.WriteLine($"✅ Throughput: {iterations * 1000.0 / stopwatch.ElapsedMilliseconds:F0} requests/second");

            Console.WriteLine("\n🎉 Simple demo completed successfully!");
            Console.WriteLine("🔧 Features working:");
            Console.WriteLine("   • Request/Response handling via fallback dispatcher");
            Console.WriteLine("   • Traditional IRequestHandler interfaces");
            Console.WriteLine("   • Dependency injection");
            Console.WriteLine("   • Performance measurement");
            Console.WriteLine("   • Reflection-based handler resolution");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during demo execution");
            Console.WriteLine($"❌ Error: {ex.Message}");
        }
    }
}