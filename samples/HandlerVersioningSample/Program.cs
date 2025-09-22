using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.HandlerVersioning;

namespace Relay.HandlerVersioning.Example
{
    // Example request
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    // Example response
    public record User(int Id, string Name, string Email);
    
    // Example handler with version 1.0
    public class UserServiceV1
    {
        [Handle]
        [HandlerVersion("1.0", IsDefault = true)]
        public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            return new User(request.UserId, $"User {request.UserId} (V1)", $"user{request.UserId}@example.com");
        }
    }
    
    // Example handler with version 2.0
    public class UserServiceV2
    {
        [Handle]
        [HandlerVersion("2.0")]
        public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            return new User(request.UserId, $"User {request.UserId} (V2)", $"user{request.UserId}@example.com");
        }
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add Relay with handler versioning
                services.AddRelay();
                services.AddRelayHandlerVersioning();
                services.AddScoped<UserServiceV1>();
                services.AddScoped<UserServiceV2>();
            });
            
            var host = builder.Build();
            
            // Get relay instances
            var relay = host.Services.GetRequiredService<IRelay>();
            var versionedRelay = host.Services.GetRequiredService<IVersionedRelay>();
            
            Console.WriteLine("Testing handler versioning behavior...");
            
            // Test default version (should use V1 handler)
            try
            {
                var request = new GetUserRequest(1);
                var user = await relay.SendAsync(request);
                Console.WriteLine($"Default version request: Success - User: {user.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Default version request: Failed - {ex.Message}");
            }
            
            // Test specific version (V2 handler)
            try
            {
                var request = new GetUserRequest(1);
                var user = await versionedRelay.SendAsync(request, "2.0");
                Console.WriteLine($"Version 2.0 request: Success - User: {user.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Version 2.0 request: Failed - {ex.Message}");
            }
            
            // Test specific version (V1 handler)
            try
            {
                var request = new GetUserRequest(1);
                var user = await versionedRelay.SendAsync(request, "1.0");
                Console.WriteLine($"Version 1.0 request: Success - User: {user.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Version 1.0 request: Failed - {ex.Message}");
            }
        }
    }
}