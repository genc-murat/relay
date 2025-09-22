using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.Retry;

namespace Relay.Retry.Example
{
    // Example request with retry
    [Retry(3, 1000)] // Retry 3 times with 1 second delay
    public record GetUserRequest(int UserId) : IRequest<User>;
    
    // Example request with exponential backoff retry
    [Retry(typeof(ExponentialBackoffRetryStrategy), 5)]
    public record GetOrderRequest(int OrderId) : IRequest<Order>;
    
    // Example request without retry
    public record GetProductRequest(int ProductId) : IRequest<Product>;
    
    // Example responses
    public record User(int Id, string Name, string Email);
    public record Order(int Id, int UserId, decimal Total);
    public record Product(int Id, string Name, decimal Price);
    
    // Example handler with intermittent failures
    public class DataService
    {
        private static int _userCallCount = 0;
        private static int _orderCallCount = 0;
        private static int _productCallCount = 0;
        
        [Handle]
        public async ValueTask<User> GetUser(GetUserRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _userCallCount);
            
            // Simulate intermittent failure - fail on first two attempts
            if (_userCallCount <= 2)
            {
                await Task.Delay(10, cancellationToken); // Simulate work
                throw new InvalidOperationException("Simulated intermittent failure");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate work
            return new User(request.UserId, $"User {request.UserId}", $"user{request.UserId}@example.com");
        }
        
        [Handle]
        public async ValueTask<Order> GetOrder(GetOrderRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _orderCallCount);
            
            // Simulate intermittent failure - fail on first three attempts
            if (_orderCallCount <= 3)
            {
                await Task.Delay(10, cancellationToken); // Simulate work
                throw new InvalidOperationException("Simulated intermittent failure");
            }
            
            await Task.Delay(10, cancellationToken); // Simulate work
            return new Order(request.OrderId, 1, 99.99m);
        }
        
        [Handle]
        public async ValueTask<Product> GetProduct(GetProductRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _productCallCount);
            await Task.Delay(10, cancellationToken); // Simulate work
            return new Product(request.ProductId, $"Product {request.ProductId}", 19.99m);
        }
        
        public static int UserCallCount => _userCallCount;
        public static int OrderCallCount => _orderCallCount;
        public static int ProductCallCount => _productCallCount;
    }
    
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add Relay with retry behavior
                services.AddRelay();
                services.AddRelayRetry();
                
                // Register retry strategies
                services.AddTransient<ExponentialBackoffRetryStrategy>(sp => 
                    new ExponentialBackoffRetryStrategy(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(5)));
                
                services.AddScoped<DataService>();
            });
            
            var host = builder.Build();
            
            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();
            
            Console.WriteLine("Testing retry behavior...");
            
            // Test request with linear retry
            var userRequest = new GetUserRequest(1);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var user = await relay.SendAsync(userRequest);
                var duration = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"Linear retry request: Success - User: {user.Name} (Duration: {duration.TotalMilliseconds}ms)");
            }
            catch (RetryExhaustedException ex)
            {
                Console.WriteLine($"Linear retry request: Failed - Retry exhausted after {ex.Exceptions.Count} attempts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linear retry request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"User handler call count: {DataService.UserCallCount}");
            
            // Test request with exponential backoff retry
            var orderRequest = new GetOrderRequest(1);
            
            try
            {
                var startTime = DateTime.UtcNow;
                var order = await relay.SendAsync(orderRequest);
                var duration = DateTime.UtcNow - startTime;
                
                Console.WriteLine($"Exponential backoff request: Success - Order: {order.Id} (Duration: {duration.TotalMilliseconds}ms)");
            }
            catch (RetryExhaustedException ex)
            {
                Console.WriteLine($"Exponential backoff request: Failed - Retry exhausted after {ex.Exceptions.Count} attempts");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exponential backoff request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"Order handler call count: {DataService.OrderCallCount}");
            
            // Test request without retry
            var productRequest = new GetProductRequest(1);
            
            try
            {
                var product = await relay.SendAsync(productRequest);
                Console.WriteLine($"Non-retry request: Success - Product: {product.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Non-retry request: Failed - {ex.Message}");
            }
            
            Console.WriteLine($"Product handler call count: {DataService.ProductCallCount}");
        }
    }
}