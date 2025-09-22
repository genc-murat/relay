using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.MessageQueue;

namespace Relay.MessageQueue.Example
{
    // Example message queue handlers
    public class UserMessageHandler
    {
        [MessageQueue("user-queue")]
        public async ValueTask HandleCreateUser(CreateUserMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            Console.WriteLine($"Creating user: {message.Name} ({message.Email})");
        }

        [MessageQueue("user-queue")]
        public async ValueTask HandleUpdateUser(UpdateUserMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            Console.WriteLine($"Updating user {message.UserId}: {message.Name} ({message.Email})");
        }

        [MessageQueue("user-queue")]
        public async ValueTask HandleDeleteUser(DeleteUserMessage message, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Simulate work
            Console.WriteLine($"Deleting user {message.UserId}");
        }
    }
}