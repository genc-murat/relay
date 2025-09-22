using System;

namespace Relay.EventSourcing.Example
{
    // Example events
    public class UserCreatedEvent : Relay.Core.EventSourcing.Event
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserUpdatedEvent : Relay.Core.EventSourcing.Event
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UserDeletedEvent : Relay.Core.EventSourcing.Event
    {
    }
}