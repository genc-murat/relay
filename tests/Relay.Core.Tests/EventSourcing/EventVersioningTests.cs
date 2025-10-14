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
        manager.RegisterMigration(new TestEventMigrationV2ToV3());
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var oldEvent = new TestEventV1
        {
            AggregateId = Guid.NewGuid(),
            AggregateVersion = 0,
            SchemaVersion = 1,
            OldField = "Original"
        };

        // Act
        var migratedEvent = manager.MigrateEvent(oldEvent);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV3>();
        var v3Event = (TestEventV3)migratedEvent;
        v3Event.FinalField.Should().Be("Final: Migrated: Original");
        v3Event.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void MigrateEvent_ShouldApplyMigrationsInCorrectOrder()
    {
        // Arrange
        var manager = new EventMigrationManager();
        // Register in reverse order to test sorting
        manager.RegisterMigration(new TestEventMigrationV2ToV3());
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var oldEvent = new TestEventV1
        {
            AggregateId = Guid.NewGuid(),
            AggregateVersion = 0,
            SchemaVersion = 1,
            OldField = "Original"
        };

        // Act
        var migratedEvent = manager.MigrateEvent(oldEvent);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV3>();
        var v3Event = (TestEventV3)migratedEvent;
        v3Event.FinalField.Should().Be("Final: Migrated: Original");
        v3Event.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void MigrateEvent_ShouldStopAtLatestVersion()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());
        manager.RegisterMigration(new TestEventMigrationV2ToV3());

        var v2Event = new TestEventV2
        {
            AggregateId = Guid.NewGuid(),
            AggregateVersion = 0,
            SchemaVersion = 2,
            NewField = "Already V2"
        };

        // Act
        var migratedEvent = manager.MigrateEvent(v2Event);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV3>();
        var v3Event = (TestEventV3)migratedEvent;
        v3Event.FinalField.Should().Be("Final: Already V2");
        v3Event.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void MigrateEvents_ShouldReturnEmptyCollection_WhenInputIsEmpty()
    {
        // Arrange
        var manager = new EventMigrationManager();
        var emptyEvents = new List<Event>();

        // Act
        var migratedEvents = manager.MigrateEvents(emptyEvents);

        // Assert
        migratedEvents.Should().BeEmpty();
    }

    [Fact]
    public void MigrationCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        manager.MigrationCount.Should().Be(0);

        manager.RegisterMigration(new TestEventMigrationV1ToV2());
        manager.MigrationCount.Should().Be(1);

        manager.RegisterMigrations(new[] { new TestEventMigrationV2ToV3() });
        manager.MigrationCount.Should().Be(2);

        manager.Clear();
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

    [Fact]
    public void RegisterMigration_ShouldAllowDuplicateMigrations()
    {
        // Arrange
        var manager = new EventMigrationManager();
        var migration1 = new TestEventMigrationV1ToV2();
        var migration2 = new DuplicateMigration();

        // Act
        manager.RegisterMigration(migration1);
        manager.RegisterMigration(migration2);

        // Assert
        manager.MigrationCount.Should().Be(2);
    }

    [Fact]
    public void MigrateEvent_ShouldHandleMigrationThatThrowsException()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new FailingMigration());

        var eventV1 = new TestEventV1 { SchemaVersion = 1, OldField = "Test" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.MigrateEvent(eventV1));
    }

    [Fact]
    public void MigrateEvent_ShouldHandleMigrationWithGaps()
    {
        // Arrange
        var manager = new EventMigrationManager();
        // Register only V1 to V3 migration, skipping V2
        manager.RegisterMigration(new TestEventMigrationV1ToV3());

        var eventV1 = new TestEventV1 { SchemaVersion = 1, OldField = "Test" };

        // Act
        var migratedEvent = manager.MigrateEvent(eventV1);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV3>();
        var v3Event = (TestEventV3)migratedEvent;
        v3Event.FinalField.Should().Be("Direct to V3: Test");
    }

    [Fact]
    public void MigrateEvent_ShouldApplyMigrationsSequentially()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());
        manager.RegisterMigration(new TestEventMigrationV2ToV3());

        var eventV1 = new TestEventV1 { SchemaVersion = 1, OldField = "Start" };

        // Act
        var migratedEvent = manager.MigrateEvent(eventV1);

        // Assert
        migratedEvent.Should().BeOfType<TestEventV3>();
        var v3Event = (TestEventV3)migratedEvent;
        v3Event.FinalField.Should().Be("Final: Migrated: Start");
    }

    [Fact]
    public void MigrateEvents_ShouldPreserveOrder()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());

        var events = new List<Event>
        {
            new TestEventV2 { SchemaVersion = 2, NewField = "Already V2" },
            new TestEventV1 { SchemaVersion = 1, OldField = "V1 First" },
            new TestEventV1 { SchemaVersion = 1, OldField = "V1 Second" }
        };

        // Act
        var migratedEvents = manager.MigrateEvents(events).ToList();

        // Assert
        migratedEvents.Should().HaveCount(3);
        migratedEvents[0].Should().BeOfType<TestEventV2>(); // No change
        migratedEvents[1].Should().BeOfType<TestEventV2>(); // Migrated
        migratedEvents[2].Should().BeOfType<TestEventV2>(); // Migrated
    }

    [Fact]
    public void Clear_ShouldRemoveAllMigrations()
    {
        // Arrange
        var manager = new EventMigrationManager();
        manager.RegisterMigration(new TestEventMigrationV1ToV2());
        manager.RegisterMigration(new TestEventMigrationV2ToV3());

        // Act
        manager.Clear();

        // Assert
        manager.MigrationCount.Should().Be(0);

        // Verify no migrations apply
        var eventV1 = new TestEventV1 { SchemaVersion = 1, OldField = "Test" };
        var migratedEvent = manager.MigrateEvent(eventV1);
        migratedEvent.Should().BeSameAs(eventV1);
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

    public class TestEventV3 : VersionedEvent
    {
        public TestEventV3()
        {
            SchemaVersion = 3;
        }

        public string FinalField { get; set; } = string.Empty;
    }

    public class TestNonVersionedEvent : Event
    {
        public string Data { get; set; } = string.Empty;
    }

    // Test Migrations
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
                    SchemaVersion = 2,
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

    public class TestEventMigrationV2ToV3 : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV2);
        public Type NewEventType => typeof(TestEventV3);
        public int FromVersion => 2;
        public int ToVersion => 3;

        public Event Migrate(Event oldEvent)
        {
            if (oldEvent is TestEventV2 v2)
            {
                return new TestEventV3
                {
                    AggregateId = v2.AggregateId,
                    AggregateVersion = v2.AggregateVersion,
                    SchemaVersion = 3,
                    FinalField = $"Final: {v2.NewField}"
                };
            }

            return oldEvent;
        }

        public bool CanMigrate(Type eventType, int version)
        {
            return eventType == OldEventType && version == FromVersion;
        }
    }

    public class InvalidMigration : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV1);
        public Type NewEventType => typeof(TestEventV2);
        public int FromVersion => 2; // Invalid: FromVersion > ToVersion
        public int ToVersion => 1;

        public Event Migrate(Event oldEvent) => oldEvent;

        public bool CanMigrate(Type eventType, int version) => true;
    }

    public class DuplicateMigration : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV1);
        public Type NewEventType => typeof(TestEventV2);
        public int FromVersion => 1;
        public int ToVersion => 2;

        public Event Migrate(Event oldEvent) => oldEvent;

        public bool CanMigrate(Type eventType, int version) => true;
    }

    public class FailingMigration : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV1);
        public Type NewEventType => typeof(TestEventV2);
        public int FromVersion => 1;
        public int ToVersion => 2;

        public Event Migrate(Event oldEvent)
        {
            throw new InvalidOperationException("Migration failed");
        }

        public bool CanMigrate(Type eventType, int version) => true;
    }

    public class TestEventMigrationV1ToV3 : IEventMigration
    {
        public Type OldEventType => typeof(TestEventV1);
        public Type NewEventType => typeof(TestEventV3);
        public int FromVersion => 1;
        public int ToVersion => 3;

        public Event Migrate(Event oldEvent)
        {
            if (oldEvent is TestEventV1 v1)
            {
                return new TestEventV3
                {
                    AggregateId = v1.AggregateId,
                    AggregateVersion = v1.AggregateVersion,
                    FinalField = $"Direct to V3: {v1.OldField}"
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
