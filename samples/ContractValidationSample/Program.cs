using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.ContractValidation;

namespace Relay.ContractValidation.Example
{
    // Example request with contract validation
    [ValidateContract]
    public record CreateUserRequest(string Name, string Email) : IRequest<User>;

    // Example request without contract validation
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
        public async ValueTask<User> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _userCallCount);
            await Task.Delay(10, cancellationToken); // Simulate work

            // Validate request manually for demo purposes
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
            {
                throw new ArgumentException("Valid email is required");
            }

            return new User(_userCallCount, request.Name, request.Email);
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
                // Add Relay with contract validation
                services.AddRelay();
                services.AddRelayContractValidation();
                services.AddScoped<DataService>();
            });

            var host = builder.Build();

            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();

            Console.WriteLine("Testing contract validation behavior...");

            // Test request with contract validation
            var userRequest = new CreateUserRequest("John Doe", "john.doe@example.com");

            try
            {
                var user = await relay.SendAsync(userRequest);
                Console.WriteLine($"Contract validation request: Success - User: {user.Name} ({user.Email})");
            }
            catch (ContractValidationException ex)
            {
                Console.WriteLine($"Contract validation request: Failed - {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Contract validation request: Failed - {ex.Message}");
            }

            Console.WriteLine($"User handler call count: {DataService.UserCallCount}");

            // Test request without contract validation
            var orderRequest = new GetOrderRequest(1);

            try
            {
                var order = await relay.SendAsync(orderRequest);
                Console.WriteLine($"Non-contract validation request: Success - Order: {order.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Non-contract validation request: Failed - {ex.Message}");
            }

            Console.WriteLine($"Order handler call count: {DataService.OrderCallCount}");
        }
    }
}