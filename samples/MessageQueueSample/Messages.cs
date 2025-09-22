namespace Relay.MessageQueue.Example
{
    // Example messages
    public record CreateUserMessage(string Name, string Email);
    public record UpdateUserMessage(Guid UserId, string Name, string Email);
    public record DeleteUserMessage(Guid UserId);
}