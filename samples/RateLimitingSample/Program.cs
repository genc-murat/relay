using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.RateLimiting.Attributes;
using Relay.Core.RateLimiting.Exceptions;

namespace Relay.RateLimiting.Example
{
    // Example request with rate limiting
    [RateLimit(5, 60, "User")] // 5 requests per minute per user
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    // Example request without rate limiting
    public record GetOrderRequest(int OrderId) : IRequest<Order>;
    
    // Example responses
    public record User(int Id, string Name, string Email);
    public record Order(int Id, int UserId, decimal Total);
    
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
                // Add Relay with rate limiting
                services.AddRelay();
                services.AddRelayRateLimiting();
                services.AddScoped<DataService>();
            });
            
            var host = builder.Build();
            
            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();
            
            Console.WriteLine("Testing rate limiting behavior...");
            
            // Test rate limited request
            var userRequest = new GetUserRequest(1);
            
            try
            {
                // Make 6 requests - 5 should succeed, 1 should fail
                for (int i = 0; i < 6; i++)
                {
                    try
                    {
                        var user = await relay.SendAsync(userRequest);
                        Console.WriteLine($"Request {i + 1}: Success - User: {user.Name}");
                    }
                    catch (RateLimitExceededException ex)
                    {
                        Console.WriteLine($"Request {i + 1}: Rate limited - Retry after {ex.RetryAfter.TotalSeconds} seconds");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine($"User handler call count: {DataService.UserCallCount}");
            
            // Test non-rate limited request
            var orderRequest = new GetOrderRequest(1);
            
            try
            {
                // Make multiple requests - all should succeed
                for (int i = 0; i < 10; i++)
                {
                    var order = await relay.SendAsync(orderRequest);
                    Console.WriteLine($"Order request {i + 1}: Success - Order: {order.Id}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            
            Console.WriteLine($"Order handler call count: {DataService.OrderCallCount}");
        }
    }
}