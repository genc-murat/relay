using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core.EventSourcing;
using Relay.EventSourcing.Example;

namespace Relay.EventSourcing.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add Relay with event sourcing
                services.AddRelay();
                services.AddRelayEventSourcing();
            });

            var host = builder.Build();

            // Get event sourcing repository
            var repository = host.Services.GetRequiredService<IEventSourcedRepository<UserAggregate, Guid>>();

            Console.WriteLine("Testing event sourcing behavior...");

            // Test creating a user
            var userId = Guid.NewGuid();
            var user = UserAggregate.Create(userId, "Murat Genc", "murat.genc@example.com");

            await repository.SaveAsync(user);
            Console.WriteLine($"Created user: {user.Name} ({user.Email})");

            // Test updating a user
            user.Update("Murat Genc", "murat.genc@example.com");
            await repository.SaveAsync(user);
            Console.WriteLine($"Updated user: {user.Name} ({user.Email})");

            // Test loading a user from events
            var loadedUser = await repository.GetByIdAsync(userId);
            if (loadedUser != null)
            {
                Console.WriteLine($"Loaded user: {loadedUser.Name} ({loadedUser.Email})");
                Console.WriteLine($"User version: {loadedUser.Version}");
                Console.WriteLine($"Uncommitted events: {loadedUser.UncommittedEvents.Count}");
            }

            // Test deleting a user
            if (loadedUser != null)
            {
                loadedUser.Delete();
                await repository.SaveAsync(loadedUser);
                Console.WriteLine($"Deleted user: {loadedUser.Name}");
            }

            // Test loading a deleted user
            var deletedUser = await repository.GetByIdAsync(userId);
            if (deletedUser != null)
            {
                Console.WriteLine($"Loaded deleted user: {deletedUser.Name}");
                Console.WriteLine($"Is deleted: {deletedUser.IsDeleted}");
            }
        }
    }
}