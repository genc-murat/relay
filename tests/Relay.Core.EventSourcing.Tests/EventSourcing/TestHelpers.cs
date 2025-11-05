using System;
using System.Reflection;
using Relay.Core.EventSourcing.Core;

namespace Relay.Core.EventSourcing.Tests;

// Test aggregate implementation
public class TestAggregate : AggregateRoot<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public void Create(Guid id, string name)
    {
        Apply(new TestAggregateCreated
        {
            AggregateId = id,
            AggregateName = name,
            AggregateVersion = 0
        });
    }

    public void ChangeName(string newName)
    {
        Apply(new TestAggregateNameChanged
        {
            AggregateId = Id,
            NewName = newName,
            AggregateVersion = Version + 1
        });
    }

    public void When(TestAggregateCreated @event)
    {
        Id = @event.AggregateId;
        Name = @event.AggregateName;
    }

    public void When(TestAggregateNameChanged @event)
    {
        Name = @event.NewName;
    }
}

// Test snapshot aggregate
public class TestSnapshotAggregate : AggregateRoot<Guid>, ISnapshotable<TestAggregateSnapshot>
{
    public string Name { get; private set; } = string.Empty;

    public int SnapshotFrequency => 10;

    public void Create(Guid id, string name)
    {
        Apply(new TestAggregateCreated
        {
            AggregateId = id,
            AggregateName = name,
            AggregateVersion = 0
        });
    }

    public void ChangeName(string newName)
    {
        Apply(new TestAggregateNameChanged
        {
            AggregateId = Id,
            NewName = newName,
            AggregateVersion = Version + 1
        });
    }

    public void When(TestAggregateCreated @event)
    {
        Id = @event.AggregateId;
        Name = @event.AggregateName;
    }

    public void When(TestAggregateNameChanged @event)
    {
        Name = @event.NewName;
    }

    public TestAggregateSnapshot CreateSnapshot()
    {
        return new TestAggregateSnapshot
        {
            Id = Id,
            Name = Name,
            Version = Version
        };
    }

    public void RestoreFromSnapshot(TestAggregateSnapshot snapshot)
    {
        Id = snapshot.Id;
        Name = snapshot.Name;

        // Restore version using reflection with non-public access
        var versionProperty = typeof(AggregateRoot<Guid>).GetProperty(nameof(Version), BindingFlags.NonPublic | BindingFlags.Instance);
        if (versionProperty != null)
        {
            versionProperty.SetValue(this, snapshot.Version);
        }
    }
}

// Test events
public class TestAggregateCreated : Event
{
    public string AggregateName { get; set; } = string.Empty;
}

public class TestAggregateNameChanged : Event
{
    public string NewName { get; set; } = string.Empty;
}

// Test snapshot aggregate with string ID
public class TestSnapshotAggregateWithStringId : AggregateRoot<string>, ISnapshotable<TestAggregateSnapshot>
{
    public string Name { get; private set; } = string.Empty;

    public int SnapshotFrequency => 10;

    public void Create(string id, string name)
    {
        Apply(new TestAggregateCreated
        {
            AggregateId = Guid.NewGuid(), // Use Guid for event, but aggregate ID is string
            AggregateName = name,
            AggregateVersion = 0
        });
        Id = id;
    }

    public void When(TestAggregateCreated @event)
    {
        Name = @event.AggregateName;
    }

    public TestAggregateSnapshot CreateSnapshot()
    {
        return new TestAggregateSnapshot
        {
            Id = Guid.NewGuid(), // Dummy
            Name = Name,
            Version = Version
        };
    }

    public void RestoreFromSnapshot(TestAggregateSnapshot snapshot)
    {
        Name = snapshot.Name;
        var versionProperty = typeof(AggregateRoot<string>).GetProperty(nameof(Version));
        if (versionProperty != null && versionProperty.CanWrite)
        {
            versionProperty.SetValue(this, snapshot.Version);
        }
    }
}

// Test snapshot
public class TestAggregateSnapshot
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
}
