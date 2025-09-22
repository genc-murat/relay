using System;

namespace Relay.Core.EventSourcing
{
    /// <summary>
    /// Base class for events in event sourcing.
    /// </summary>
    public abstract class Event
    {
        /// <summary>
        /// Gets the unique identifier of the event.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        public DateTime Timestamp { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the type of the event.
        /// </summary>
        public string EventType => GetType().FullName ?? GetType().Name;

        /// <summary>
        /// Gets or sets the version of the aggregate when this event was created.
        /// </summary>
        public int AggregateVersion { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the aggregate.
        /// </summary>
        public Guid AggregateId { get; set; }
    }
}