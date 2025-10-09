using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Versioning;
using Xunit;

namespace Relay.Core.Tests.EventSourcing;

public class EventVersioningTests
{
    [Fact]
    public void EventMigrationManager_ShouldRegisterMigration()
    {
        // Arrange
        var manager = new EventMigrationManager();
        var migration = new TestEventMigrationV1ToV2();

        // Act
        manager.RegisterMigration(migration);

        // Assert
        manager.MigrationCount.Should().Be(1);
    }

    [Fact]
    public void EventMigrationManager_ShouldMigrateEvent()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var oldEvent = new TestEventV1
        {
            AggregateId = Guid.NewGuid(),
            AggregateVersion = 0,
            SchemaVersion = 1,
            OldField = "Old Value"
        };

        // Act
        var migratedEvent = manager.MigrateEvent(oldEvent);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV2>();
        var v2Event = (TestEventV2)migratedEvent;
        v2Event.NewField.Should().Be("Migrated: Old Value");
        v2Event.SchemaVersion.Should().Be(2);
    }

    [Fact]
    public void EventMigrationManager_ShouldMigrateMultipleEvents()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var events = new List<Event>
        {
            new TestEventV1 { SchemaVersion = 1, OldField = "Event 1" },
            new TestEventV1 { SchemaVersion = 1, OldField = "Event 2" },
            new TestEventV2 { SchemaVersion = 2, NewField = "Already V2" }
        };

        // Act
        var migratedEvents = manager.MigrateEvents(events).ToList();

        // Assert
        migratedEvents.Should().HaveCount(3);
        migratedEvents[0].Should().BeOfType<TestEventV2>();
        migratedEvents[1].Should().BeOfType<TestEventV2>();
        migratedEvents[2].Should().BeOfType<TestEventV2>();
    }

    [Fact]
    public void EventMigrationManager_ShouldNotMigrateWhenNoMigrationApplies()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var eventV2 = new TestEventV2
        {
            SchemaVersion = 2,
            NewField = "Already V2"
        };

        // Act
        var migratedEvent = manager.MigrateEvent(eventV2);

        // Assert
        migratedEvent.Should().BeSameAs(eventV2);
    }

    [Fact]
    public void EventMigrationManager_ShouldClearMigrations()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        // Act
        manager.Clear();

        // Assert
        manager.MigrationCount.Should().Be(0);
    }

    [Fact]
    public void VersionedEvent_ShouldHaveDefaultSchemaVersion()
    {
        // Arrange & Act
        var versionedEvent = new TestEventV1();

        // Assert
        versionedEvent.SchemaVersion.Should().Be(1);
    }

    // Test Event Classes
    public class TestEventV1 : VersionedEvent
    {
        public string OldField { get; set; } = string.Empty;
    }

    public class TestEventV2 : VersionedEvent
    {
        public TestEventV2()
        {
            SchemaVersion = 2;
        }

        public string NewField { get; set; } = string.Empty;
    }

    // Test Migration
    public class TestEventMigrationV1ToV2 : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV1);
        public Type NewEventType => typeof(TestEventV2);
        public int FromVersion => 1;
        public int ToVersion => 2;

        public Event Migrate(Event oldEvent)
        {
            if (oldEvent is TestEventV1 v1)
            {
                return new TestEventV2
                {
                    AggregateId = v1.AggregateId,
                    AggregateVersion = v1.AggregateVersion,
                    NewField = $"Migrated: {v1.OldField}"
                };
            }

            return oldEvent;
        }

        public bool CanMigrate(Type eventType, int version)
        {
            return eventType == OldEventType && version == FromVersion;
        }
    }
}
