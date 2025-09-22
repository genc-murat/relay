using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay;
using Relay.Core.MessageQueue;
using Relay.MessageQueue.Example;

namespace Relay.MessageQueue.Sample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup host
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureServices(services =>
            {
                // Add Relay with message queue integration
                services.AddRelay();
                services.AddRelayMessageQueue();
                services.AddScoped<UserMessageHandler>();
            });
            
            var host = builder.Build();
            
            // Get message queue publisher
            var publisher = host.Services.GetRequiredService<IMessageQueuePublisher>();
            
            Console.WriteLine("Testing message queue integration...");
            
            // Test publishing messages
            await publisher.PublishAsync("user-queue", new CreateUserMessage("John Doe", "john.doe@example.com"));
            Console.WriteLine("Published CreateUserMessage");
            
            await publisher.PublishAsync("user-queue", new UpdateUserMessage(Guid.NewGuid(), "John Smith", "john.smith@example.com"));
            Console.WriteLine("Published UpdateUserMessage");
            
            await publisher.PublishAsync("user-queue", new DeleteUserMessage(Guid.NewGuid()));
            Console.WriteLine("Published DeleteUserMessage");
            
            // Get message queue consumer
            var consumer = host.Services.GetRequiredService<IMessageQueueConsumer>();
            
            // Start consuming messages
            // Note: In a real application, you would wire up the consumer to your handlers
            // For this example, we'll just show that the services are registered correctly
            
            Console.WriteLine("Message queue integration test completed.");
        }
    }
}