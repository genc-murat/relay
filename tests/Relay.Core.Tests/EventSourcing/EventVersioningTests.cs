using System;
using System.Collections.Generic;
using System.Linq;
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
        Assert.Equal(1, manager.MigrationCount);
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
        Assert.IsType<TestEventV3>(migratedEvent);
        var v3Event = (TestEventV3)migratedEvent;
        Assert.Equal("Final: Migrated: Original", v3Event.FinalField);
        Assert.Equal(3, v3Event.SchemaVersion);
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
        Assert.IsType<TestEventV3>(migratedEvent);
        var v3Event = (TestEventV3)migratedEvent;
        Assert.Equal("Final: Migrated: Original", v3Event.FinalField);
        Assert.Equal(3, v3Event.SchemaVersion);
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
        Assert.IsType<TestEventV3>(migratedEvent);
        var v3Event = (TestEventV3)migratedEvent;
        Assert.Equal("Final: Already V2", v3Event.FinalField);
        Assert.Equal(3, v3Event.SchemaVersion);
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
        Assert.Empty(migratedEvents);
    }

    [Fact]
    public void MigrationCount_ShouldReturnCorrectCount()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        Assert.Equal(0, manager.MigrationCount);

        manager.RegisterMigration(new TestEventMigrationV1ToV2());
        Assert.Equal(1, manager.MigrationCount);

        manager.RegisterMigrations(new[] { new TestEventMigrationV2ToV3() });
        Assert.Equal(2, manager.MigrationCount);

        manager.Clear();
        Assert.Equal(0, manager.MigrationCount);
    }

    [Fact]
    public void VersionedEvent_ShouldHaveDefaultSchemaVersion()
    {
        // Arrange & Act
        var versionedEvent = new TestEventV1();

        // Assert
        Assert.Equal(1, versionedEvent.SchemaVersion);
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
        Assert.Equal(2, manager.MigrationCount);
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
        Assert.IsType<TestEventV3>(migratedEvent);
        var v3Event = (TestEventV3)migratedEvent;
        Assert.Equal("Direct to V3: Test", v3Event.FinalField);
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
        Assert.IsType<TestEventV3>(migratedEvent);
        var v3Event = (TestEventV3)migratedEvent;
        Assert.Equal("Final: Migrated: Start", v3Event.FinalField);
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
        Assert.Equal(3, migratedEvents.Count);
        Assert.IsType<TestEventV2>(migratedEvents[0]); // No change
        Assert.IsType<TestEventV2>(migratedEvents[1]); // Migrated
        Assert.IsType<TestEventV2>(migratedEvents[2]); // Migrated
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
        Assert.Equal(0, manager.MigrationCount);

        // Verify no migrations apply
        var eventV1 = new TestEventV1 { SchemaVersion = 1, OldField = "Test" };
        var migratedEvent = manager.MigrateEvent(eventV1);
        Assert.Same(eventV1, migratedEvent);
    }

    [Fact]
    public void RegisterMigration_ShouldThrowArgumentNullException_WhenMigrationIsNull()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterMigration(null!));
    }

    [Fact]
    public void RegisterMigrations_ShouldThrowArgumentNullException_WhenMigrationsIsNull()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterMigrations(null!));
    }

    [Fact]
    public void MigrateEvent_ShouldThrowArgumentNullException_WhenEventIsNull()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.MigrateEvent(null!));
    }

    [Fact]
    public void MigrateEvents_ShouldThrowArgumentNullException_WhenEventsIsNull()
    {
        // Arrange
        var manager = new EventMigrationManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.MigrateEvents(null!));
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
