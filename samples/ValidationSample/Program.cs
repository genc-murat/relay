using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core;
using Relay.Core.Validation;

namespace Relay.Validation.Example
{
    // Example request
    public record CreateUserRequest(string Name, string Email) : IRequest<User>;

    // Example response
    public record User(int Id, string Name, string Email);

    // Example validation rule for CreateUserRequest
    [ValidationRule(Order = 1)]
    public class CreateUserRequestValidationRule : IValidationRule<CreateUserRequest>
    {
        public async ValueTask<IEnumerable<string>> ValidateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Name is required.");
            }
            else if (request.Name.Length < 2)
            {
                errors.Add("Name must be at least 2 characters long.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors.Add("Email is required.");
            }
            else if (!request.Email.Contains("@"))
            {
                errors.Add("Email must be a valid email address.");
            }

            // Simulate async operation
            await Task.Delay(1, cancellationToken);

            return errors;
        }
    }

    // Example handler
    public class UserService
    {
        private static int _idCounter = 1;

        [Handle]
        public async ValueTask<User> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
        {
            // Simulate async operation
            await Task.Delay(10, cancellationToken);

            return new User(_idCounter++, request.Name, request.Email);
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
                // Add Relay with validation
                services.AddRelay();
                services.AddRelayValidation();
                services.AddValidationRulesFromCallingAssembly();
                services.AddScoped<UserService>();
            });

            var host = builder.Build();

            // Get relay instance
            var relay = host.Services.GetRequiredService<IRelay>();

            // Test valid request
            try
            {
                var validRequest = new CreateUserRequest("Murat Genc", "murat.genc@example.com");
                var user = await relay.SendAsync(validRequest);
                Console.WriteLine($"Created user: {user.Name} ({user.Email})");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation failed: {string.Join(", ", ex.Errors)}");
            }

            // Test invalid request
            try
            {
                var invalidRequest = new CreateUserRequest("", "invalid-email");
                var user = await relay.SendAsync(invalidRequest);
                Console.WriteLine($"Created user: {user.Name} ({user.Email})");
            }
            catch (ValidationException ex)
            {
                Console.WriteLine($"Validation failed: {string.Join(", ", ex.Errors)}");
            }
        }
    }
}