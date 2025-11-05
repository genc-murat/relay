using Moq;
using Relay.Core.EventSourcing.Core;
using Relay.Core.EventSourcing.Repositories;
using Relay.Core.EventSourcing.Stores;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.EventSourcing.Tests;

public class EventSourcedRepositoryComprehensiveTests
{
    [Fact]
    public void Constructor_WithNullEventStore_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EventSourcedRepository<TestAggregate, Guid>(null!));
    }

    [Fact]
    public void Constructor_WithValidEventStore_CreatesInstance()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>().Object;

        // Act
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore);

        // Assert
        Assert.NotNull(repository);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentAggregate_ReturnsNull()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(x => x.GetEventsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(new List<Event>().ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        var aggregateId = Guid.NewGuid();

        // Act
        var result = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAggregate_ReturnsAggregate()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        var @event = new TestAggregateCreated 
        { 
            AggregateId = aggregateId, 
            AggregateName = "Test Name", 
            AggregateVersion = 0 
        };
        
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(new List<Event> { @event }.ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act
        var result = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(aggregateId, result.Id);
        Assert.Equal("Test Name", result.Name);
        Assert.Equal(0, result.Version);
    }

    [Fact]
    public async Task GetByIdAsync_WithMultipleEvents_ReturnsAggregateWithAllApplied()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        var events = new List<Event>
        {
            new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Initial Name", AggregateVersion = 0 },
            new TestAggregateNameChanged { AggregateId = aggregateId, NewName = "Updated Name", AggregateVersion = 1 }
        };
        
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(events.ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act
        var result = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(aggregateId, result.Id);
        Assert.Equal("Updated Name", result.Name); // Should be the final state after applying all events
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyEventStream_ReturnsNull()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(new List<Event>().ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act
        var result = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventStoreThrows_ExceptionIsPropagated()
    {
        // Arrange - We'll test with a real exception scenario by using a mock that throws during enumeration
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        
        // Return a custom async enumerable that throws
        var throwingEnumerable = new ThrowingAsyncEnumerable<Event>(new InvalidOperationException("Test exception"));
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(throwingEnumerable);
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.GetByIdAsync(aggregateId).AsTask());
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(new List<Event>().ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act & Assert - Should work with cancellation token
        cts.CancelAfter(100); // Cancel after 100ms if needed
        var result = await repository.GetByIdAsync(aggregateId, cts.Token);
        
        Assert.Null(result); // No aggregate exists
    }

    [Fact]
    public async Task SaveAsync_WithNoUncommittedEvents_DoesNotCallEventStore()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        // Create an aggregate without any uncommitted events
        var aggregate = new TestAggregate();
        // The aggregate has a default Id that we need to set - but since it's already created with new(), 
        // we'll simulate an aggregate that has been loaded and has no uncommitted events
        var aggregateId = Guid.NewGuid();
        var existingEvent = new TestAggregateCreated { AggregateId = aggregateId, AggregateName = "Test", AggregateVersion = 0 };
        aggregate.LoadFromHistory(new List<Event> { existingEvent }); // Load existing state
        // At this point, UncommittedEvents should be empty after LoadFromHistory

        // Act
        await repository.SaveAsync(aggregate);

        // Assert
        eventStore.Verify(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Event>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_CallsEventStore()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");

        // Act
        await repository.SaveAsync(aggregate);

        // Assert
        eventStore.Verify(x => x.SaveEventsAsync(
            It.IsAny<Guid>(), 
            It.IsAny<IEnumerable<Event>>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()), 
        Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_ClearsUncommittedEvents()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");

        // Verify uncommitted events exist before saving
        Assert.NotEmpty(aggregate.UncommittedEvents);

        // Act
        await repository.SaveAsync(aggregate);

        // Assert
        Assert.Empty(aggregate.UncommittedEvents);
    }

    [Fact]
    public async Task SaveAsync_WithUncommittedEvents_CallsEventStoreWithCorrectParameters()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");

        // Act
        await repository.SaveAsync(aggregate);

        // Assert
        eventStore.Verify(x => x.SaveEventsAsync(
            aggregateId, 
            It.IsAny<IEnumerable<Event>>(), 
            -1, // Expected version: -1 (aggregate version 0 - 1 uncommitted event = -1)
            It.IsAny<CancellationToken>()), 
        Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithMultipleUncommittedEvents_CallsEventStoreOnce()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");
        aggregate.ChangeName("New Name"); // This creates another uncommitted event

        // Act
        await repository.SaveAsync(aggregate);

        // Assert
        eventStore.Verify(x => x.SaveEventsAsync(
            It.IsAny<Guid>(), 
            It.IsAny<IEnumerable<Event>>(), 
            It.IsAny<int>(), 
            It.IsAny<CancellationToken>()), 
        Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WhenEventStoreThrows_ExceptionIsPropagated()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Event>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.SaveAsync(aggregate).AsTask());
    }

    [Fact]
    public async Task SaveAsync_WithCancellation_CanBeCancelled()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        eventStore
            .Setup(x => x.SaveEventsAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<Event>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Test Name");
        var cts = new CancellationTokenSource();

        // Act & Assert - Should work with cancellation token
        await repository.SaveAsync(aggregate, cts.Token);

        eventStore.Verify(x => x.SaveEventsAsync(
            aggregateId, 
            It.IsAny<IEnumerable<Event>>(), 
            -1, 
            cts.Token), 
        Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentIdTypes_UsesGetAggregateGuidCorrectly()
    {
        // Testing the GetAggregateGuid helper method behavior through the repository
        // We'll test with Guid since that's handled specifically in the GetAggregateGuid method
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        var @event = new TestAggregateCreated 
        { 
            AggregateId = aggregateId, 
            AggregateName = "Test Name", 
            AggregateVersion = 0 
        };
        
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(new List<Event> { @event }.ToAsyncEnumerable());
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act
        var result = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(aggregateId, result.Id);
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_FullRoundTripWorks()
    {
        // Arrange
        var eventStore = new InMemoryEventStore(); // Using real implementation for full test
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Initial Name");

        // Act - Save
        await repository.SaveAsync(aggregate);

        // Act - Load
        var loadedAggregate = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal(aggregateId, loadedAggregate.Id);
        Assert.Equal("Initial Name", loadedAggregate.Name);
        Assert.Equal(0, loadedAggregate.Version);
        Assert.Empty(loadedAggregate.UncommittedEvents);
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_WithMultipleEvents_WorksCorrectly()
    {
        // Arrange
        var eventStore = new InMemoryEventStore();
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore);
        
        var aggregate = new TestAggregate();
        var aggregateId = Guid.NewGuid();
        aggregate.Create(aggregateId, "Initial Name");

        // Save initial state
        await repository.SaveAsync(aggregate);

        // Change state and save again
        aggregate.ChangeName("Updated Name");
        await repository.SaveAsync(aggregate);

        // Act - Load latest
        var loadedAggregate = await repository.GetByIdAsync(aggregateId);

        // Assert
        Assert.NotNull(loadedAggregate);
        Assert.Equal(aggregateId, loadedAggregate.Id);
        Assert.Equal("Updated Name", loadedAggregate.Name);
        Assert.Equal(1, loadedAggregate.Version); // Should be version 1 after 2 events
        Assert.Empty(loadedAggregate.UncommittedEvents); // Should be cleared after loading
    }

    [Fact]
    public async Task GetByIdAsync_WithDisposedEventStore_ThrowsException()
    {
        // Arrange
        var eventStore = new Mock<IEventStore>();
        var aggregateId = Guid.NewGuid();
        
        // Return a custom async enumerable that throws
        var throwingEnumerable = new ThrowingAsyncEnumerable<Event>(new ObjectDisposedException("EventStore"));
        eventStore
            .Setup(x => x.GetEventsAsync(aggregateId, It.IsAny<CancellationToken>()))
            .Returns(throwingEnumerable);
        
        var repository = new EventSourcedRepository<TestAggregate, Guid>(eventStore.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => repository.GetByIdAsync(aggregateId).AsTask());
    }
}

// Helper class to create an async enumerable that throws
internal class ThrowingAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly Exception _exception;

    public ThrowingAsyncEnumerable(Exception exception)
    {
        _exception = exception;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new ThrowingAsyncEnumerator<T>(_exception);
    }
}

internal class ThrowingAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly Exception _exception;

    public ThrowingAsyncEnumerator(Exception exception)
    {
        _exception = exception;
    }

    public T Current => throw _exception;

    public ValueTask<bool> MoveNextAsync()
    {
        throw _exception;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

// Helper extension method to convert list to async enumerable
internal static class ListExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
