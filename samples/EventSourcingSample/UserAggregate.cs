using System;
using Relay.Core.EventSourcing;

namespace Relay.EventSourcing.Example
{
    // Example aggregate root
    public class UserAggregate : AggregateRoot<Guid>
    {
        public string Name { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public bool IsDeleted { get; private set; } = false;

        // Constructor for creating a new user
        public static UserAggregate Create(Guid id, string name, string email)
        {
            var user = new UserAggregate { Id = id };
            user.Apply(new UserCreatedEvent
            {
                Name = name,
                Email = email
            });
            return user;
        }

        // Method for updating user
        public void Update(string name, string email)
        {
            if (IsDeleted)
                throw new InvalidOperationException("Cannot update a deleted user.");

            Apply(new UserUpdatedEvent
            {
                Name = name,
                Email = email
            });
        }

        // Method for deleting user
        public void Delete()
        {
            if (IsDeleted)
                throw new InvalidOperationException("User is already deleted.");

            Apply(new UserDeletedEvent());
        }

        // Event handlers
        private void Apply(UserCreatedEvent @event)
        {
            Name = @event.Name;
            Email = @event.Email;
        }

        private void Apply(UserUpdatedEvent @event)
        {
            Name = @event.Name;
            Email = @event.Email;
        }

        private void Apply(UserDeletedEvent @event)
        {
            IsDeleted = true;
        }
    }
}