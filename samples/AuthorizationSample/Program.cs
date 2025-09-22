using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.Authorization;

namespace Relay.Authorization.Example
{
    // Example request with authorization
    [Authorize("Admin", "User")]
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    // Example request without authorization
    public record GetOrderRequest(int OrderId) : IRequest<Order>;
    
    // Example responses
    public record User(int Id, string Name, string Email);
    public record Order(int Id, int UserId, decimal Total);
    
    // Custom authorization service
    public class CustomAuthorizationService : IAuthorizationService
    {
        public async ValueTask<bool> AuthorizeAsync(IAuthorizationContext context, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // Simulate async work
            
            // Check if user has required roles
            var requiredRoles = new HashSet<string>(context.Properties.TryGetValue("RequiredRoles", out var roles) 
                ? (string[])roles 
                : Array.Empty<string>());
            
            // For demo purposes, we'll just check if user has "User" role
            return context.UserRoles.Contains("User");
        }
    }
    
    // Custom authorization context
    public class CustomAuthorizationContext : IAuthorizationContext
    {
        public IEnumerable<Claim> UserClaims { get; set; } = new List<Claim>();
        public IEnumerable<string> UserRoles { get; set; } = new[] { "User" }; // Simulate authenticated user
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
    
    // Example handler
    public class DataService
    {
        private static int _userCallCount = 0;
        private static int _orderCallCount = 0;
        
        [Handle]
        public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _userCallCount);
            await Task.Delay(10, cancellationToken); // Simulate work
            return new User(request.UserId, $"User {request.UserId}", $"user{request.UserId}@example.com");
        }
        
        [Handle]
        public async ValueTask<Order> GetOrder(GetOrderRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _orderCallCount);
            await Task.Delay(10, cancellationToken); // Simulate work
            return new Order(request.OrderId, 1, 99.99m);
        }
        
        public static int UserCallCount => _userCallCount;
        public static int OrderCallCount => _orderCallCount;
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add Relay with authorization
                services.AddRelay();
                services.AddRelayAuthorization();
                
                // Override default authorization services with custom ones
                services.AddTransient<IAuthorizationService, CustomAuthorizationService>();
                services.AddTransient<IAuthorizationContext, CustomAuthorizationContext>();
                
                services.AddScoped<DataService>();
            });
            
            var host = builder.Build();
            
            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();
            
            Console.WriteLine("Testing authorization behavior...");
            
            // Test authorized request
            var userRequest = new GetUserRequest(1);
            
            try
            {
                var user = await relay.SendAsync(userRequest);
                Console.WriteLine($"Authorized request: Success - User: {user.Name}");
            }
            catch (AuthorizationException ex)
            {
                Console.WriteLine($"Authorized request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"User handler call count: {DataService.UserCallCount}");
            
            // Test non-authorized request
            var orderRequest = new GetOrderRequest(1);
            
            try
            {
                var order = await relay.SendAsync(orderRequest);
                Console.WriteLine($"Non-authorized request: Success - Order: {order.Id}");
            }
            catch (AuthorizationException ex)
            {
                Console.WriteLine($"Non-authorized request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"Order handler call count: {DataService.OrderCallCount}");
        }
    }
}