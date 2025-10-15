using System.Text.Json;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class SerializationTests
{
    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeComplexObjects()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        ComplexMessage? receivedMessage = null;

        await broker.SubscribeAsync<ComplexMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new ComplexMessage
        {
            Id = Guid.NewGuid(),
            Name = "Test Message",
            CreatedAt = DateTimeOffset.UtcNow,
            Items = new List<string> { "item1", "item2", "item3" },
            Metadata = new Dictionary<string, object>
            {
                { "priority", 1 },
                { "category", "test" }
            },
            IsActive = true
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Equal(originalMessage.Name, receivedMessage.Name);
        Assert.Equal(originalMessage.CreatedAt, receivedMessage.CreatedAt);
        Assert.Equal(originalMessage.Items, receivedMessage.Items);
        Assert.Equal(originalMessage.Metadata["priority"], receivedMessage.Metadata["priority"]);
        Assert.Equal(originalMessage.Metadata["category"], receivedMessage.Metadata["category"]);
        Assert.Equal(originalMessage.IsActive, receivedMessage.IsActive);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeNestedObjects()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        OrderMessage? receivedMessage = null;

        await broker.SubscribeAsync<OrderMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new OrderMessage
        {
            OrderId = "ORD-12345",
            Customer = new Customer
            {
                Id = "CUST-001",
                Name = "John Doe",
                Email = "john.doe@example.com"
            },
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = "PROD-001", Quantity = 2, Price = 29.99m },
                new OrderItem { ProductId = "PROD-002", Quantity = 1, Price = 49.99m }
            },
            TotalAmount = 109.97m,
            OrderDate = DateTimeOffset.UtcNow
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.OrderId, receivedMessage.OrderId);
        Assert.Equal(originalMessage.Customer.Id, receivedMessage.Customer.Id);
        Assert.Equal(originalMessage.Customer.Name, receivedMessage.Customer.Name);
        Assert.Equal(originalMessage.Customer.Email, receivedMessage.Customer.Email);
        Assert.Equal(2, receivedMessage.Items.Count);
        Assert.Equal(originalMessage.Items[0].ProductId, receivedMessage.Items[0].ProductId);
        Assert.Equal(originalMessage.Items[0].Quantity, receivedMessage.Items[0].Quantity);
        Assert.Equal(originalMessage.Items[0].Price, receivedMessage.Items[0].Price);
        Assert.Equal(originalMessage.TotalAmount, receivedMessage.TotalAmount);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeEnums()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        StatusMessage? receivedMessage = null;

        await broker.SubscribeAsync<StatusMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new StatusMessage
        {
            Id = "MSG-001",
            Status = OrderStatus.Processing,
            Priority = Priority.High,
            Tags = new List<string> { "urgent", "vip" }
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Equal(OrderStatus.Processing, receivedMessage.Status);
        Assert.Equal(Priority.High, receivedMessage.Priority);
        Assert.Equal(originalMessage.Tags, receivedMessage.Tags);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeNullableTypes()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        NullableMessage? receivedMessage = null;

        await broker.SubscribeAsync<NullableMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new NullableMessage
        {
            Id = "MSG-001",
            Count = 42,
            OptionalText = null,
            OptionalNumber = 3.14,
            IsCompleted = true
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Equal(originalMessage.Count, receivedMessage.Count);
        Assert.Null(receivedMessage.OptionalText);
        Assert.Equal(originalMessage.OptionalNumber, receivedMessage.OptionalNumber);
        Assert.Equal(originalMessage.IsCompleted, receivedMessage.IsCompleted);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeDateTime()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        TimeMessage? receivedMessage = null;

        await broker.SubscribeAsync<TimeMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var specificTime = new DateTime(2023, 12, 25, 10, 30, 45, DateTimeKind.Utc);
        var originalMessage = new TimeMessage
        {
            Id = "TIME-001",
            Timestamp = specificTime,
            OffsetTimestamp = new DateTimeOffset(specificTime, TimeSpan.Zero),
            Duration = TimeSpan.FromHours(2.5)
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Equal(originalMessage.Timestamp, receivedMessage.Timestamp);
        Assert.Equal(originalMessage.OffsetTimestamp, receivedMessage.OffsetTimestamp);
        Assert.Equal(originalMessage.Duration, receivedMessage.Duration);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeArrays()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        ArrayMessage? receivedMessage = null;

        await broker.SubscribeAsync<ArrayMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new ArrayMessage
        {
            Id = "ARRAY-001",
            Numbers = new int[] { 1, 2, 3, 4, 5 },
            Strings = new string[] { "a", "b", "c" },
            Booleans = new bool[] { true, false, true },
            Matrix = new int[][]
            {
                new int[] { 1, 2 },
                new int[] { 3, 4 }
            }
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Equal(originalMessage.Numbers, receivedMessage.Numbers);
        Assert.Equal(originalMessage.Strings, receivedMessage.Strings);
        Assert.Equal(originalMessage.Booleans, receivedMessage.Booleans);
        Assert.Equal(originalMessage.Matrix.Length, receivedMessage.Matrix.Length);
        Assert.Equal(originalMessage.Matrix[0], receivedMessage.Matrix[0]);
        Assert.Equal(originalMessage.Matrix[1], receivedMessage.Matrix[1]);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldSerializeEmptyCollections()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        CollectionMessage? receivedMessage = null;

        await broker.SubscribeAsync<CollectionMessage>(async (message, context, ct) =>
        {
            receivedMessage = message;
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var originalMessage = new CollectionMessage
        {
            Id = "COLL-001",
            EmptyList = new List<string>(),
            EmptyDictionary = new Dictionary<string, int>(),
            NullList = null,
            NullDictionary = null
        };

        // Act
        await broker.PublishAsync(originalMessage);
        await Task.Delay(100);

        // Assert
        Assert.NotNull(receivedMessage);
        Assert.Equal(originalMessage.Id, receivedMessage.Id);
        Assert.Empty(receivedMessage.EmptyList);
        Assert.Empty(receivedMessage.EmptyDictionary);
        Assert.Null(receivedMessage.NullList);
        Assert.Null(receivedMessage.NullDictionary);
    }

    private class ComplexMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public List<string> Items { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool IsActive { get; set; }
    }

    private class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public Customer Customer { get; set; } = new();
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public DateTimeOffset OrderDate { get; set; }
    }

    private class Customer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    private enum OrderStatus
    {
        Pending,
        Processing,
        Completed,
        Cancelled
    }

    private enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    private class StatusMessage
    {
        public string Id { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public Priority Priority { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    private class NullableMessage
    {
        public string Id { get; set; } = string.Empty;
        public int Count { get; set; }
        public string? OptionalText { get; set; }
        public double? OptionalNumber { get; set; }
        public bool IsCompleted { get; set; }
    }

    private class TimeMessage
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTimeOffset OffsetTimestamp { get; set; }
        public TimeSpan Duration { get; set; }
    }

    private class ArrayMessage
    {
        public string Id { get; set; } = string.Empty;
        public int[] Numbers { get; set; } = Array.Empty<int>();
        public string[] Strings { get; set; } = Array.Empty<string>();
        public bool[] Booleans { get; set; } = Array.Empty<bool>();
        public int[][] Matrix { get; set; } = Array.Empty<int[]>();
    }

    private class CollectionMessage
    {
        public string Id { get; set; } = string.Empty;
        public List<string> EmptyList { get; set; } = new();
        public Dictionary<string, int> EmptyDictionary { get; set; } = new();
        public List<string>? NullList { get; set; }
        public Dictionary<string, int>? NullDictionary { get; set; }
    }
}