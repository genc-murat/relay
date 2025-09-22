using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.DistributedTracing;

namespace Relay.DistributedTracing.Example
{
    // Example request with distributed tracing
    [Trace]
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    // Example request without distributed tracing
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
                // Add Relay with distributed tracing
                services.AddRelay();
                services.AddRelayDistributedTracing();
                services.AddScoped<DataService>();
            });
            
            var host = builder.Build();
            
            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();
            
            Console.WriteLine("Testing distributed tracing behavior...");
            
            // Test request with distributed tracing
            var userRequest = new GetUserRequest(1);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var user = await relay.SendAsync(userRequest);
                var duration = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"Distributed tracing request: Success - User: {user.Name} (Duration: {duration.TotalMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Distributed tracing request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"User handler call count: {DataService.UserCallCount}");
            
            // Test request without distributed tracing
            var orderRequest = new GetOrderRequest(1);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var order = await relay.SendAsync(orderRequest);
                var duration = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"Non-distributed tracing request: Success - Order: {order.Id} (Duration: {duration.TotalMilliseconds}ms)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Non-distributed tracing request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"Order handler call count: {DataService.OrderCallCount}");
        }
    }
}