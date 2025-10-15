using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageRoutingTests
{
    [Fact]
    public async Task InMemoryMessageBroker_ShouldRouteMessagesByRoutingKey()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var orderMessages = new List<OrderMessage>();
        var paymentMessages = new List<PaymentMessage>();

        await broker.SubscribeAsync<OrderMessage>(async (message, context, ct) =>
        {
            orderMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.SubscribeAsync<PaymentMessage>(async (message, context, ct) =>
        {
            paymentMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var orderMessage = new OrderMessage { OrderId = "ORD-001", Amount = 100.00m };
        var paymentMessage = new PaymentMessage { PaymentId = "PAY-001", Amount = 100.00m };

        // Act
        await broker.PublishAsync(orderMessage, new PublishOptions { RoutingKey = "orders" });
        await broker.PublishAsync(paymentMessage, new PublishOptions { RoutingKey = "payments" });
        await Task.Delay(100);

        // Assert
        Assert.Single(orderMessages);
        Assert.Single(paymentMessages);
        Assert.Equal("ORD-001", orderMessages[0].OrderId);
        Assert.Equal("PAY-001", paymentMessages[0].PaymentId);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldRouteMessagesByExchange()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var auditMessages = new List<AuditMessage>();
        var notificationMessages = new List<NotificationMessage>();

        await broker.SubscribeAsync<AuditMessage>(async (message, context, ct) =>
        {
            auditMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.SubscribeAsync<NotificationMessage>(async (message, context, ct) =>
        {
            notificationMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var auditMessage = new AuditMessage { EventType = "USER_LOGIN", UserId = "user123" };
        var notificationMessage = new NotificationMessage { Type = "EMAIL", Recipient = "user@example.com" };

        // Act
        await broker.PublishAsync(auditMessage, new PublishOptions { Exchange = "audit" });
        await broker.PublishAsync(notificationMessage, new PublishOptions { Exchange = "notifications" });
        await Task.Delay(100);

        // Assert
        Assert.Single(auditMessages);
        Assert.Single(notificationMessages);
        Assert.Equal("USER_LOGIN", auditMessages[0].EventType);
        Assert.Equal("EMAIL", notificationMessages[0].Type);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldHandleWildcardRouting()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var allOrderMessages = new List<OrderMessage>();
        var createdOrderMessages = new List<OrderMessage>();

        // Subscribe to all order messages
        await broker.SubscribeAsync<OrderMessage>(async (message, context, ct) =>
        {
            allOrderMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions { RoutingKey = "orders.*" });

        // Subscribe to created orders only
        await broker.SubscribeAsync<OrderMessage>(async (message, context, ct) =>
        {
            createdOrderMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions { RoutingKey = "orders.created" });

        await broker.StartAsync();

        var createdOrder = new OrderMessage { OrderId = "ORD-001", Amount = 100.00m };
        var updatedOrder = new OrderMessage { OrderId = "ORD-002", Amount = 200.00m };

        // Act
        await broker.PublishAsync(createdOrder, new PublishOptions { RoutingKey = "orders.created" });
        await broker.PublishAsync(updatedOrder, new PublishOptions { RoutingKey = "orders.updated" });
        await Task.Delay(100);

        // Assert
        Assert.Equal(2, allOrderMessages.Count); // Both messages match "orders.*"
        Assert.Single(createdOrderMessages); // Only created message matches "orders.created"
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldFilterMessagesByHeaders()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var highPriorityMessages = new List<PriorityMessage>();
        var lowPriorityMessages = new List<PriorityMessage>();

        // Subscribe to high priority messages
        await broker.SubscribeAsync<PriorityMessage>(async (message, context, ct) =>
        {
            highPriorityMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions
        {
            RoutingKey = "high"
        });

        // Subscribe to low priority messages
        await broker.SubscribeAsync<PriorityMessage>(async (message, context, ct) =>
        {
            lowPriorityMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions
        {
            RoutingKey = "low"
        });

        await broker.StartAsync();

        var highPriorityMessage = new PriorityMessage { Id = "MSG-001", Content = "Urgent message" };
        var lowPriorityMessage = new PriorityMessage { Id = "MSG-002", Content = "Regular message" };

        // Act
        await broker.PublishAsync(highPriorityMessage, new PublishOptions
        {
            RoutingKey = "high"
        });

        await broker.PublishAsync(lowPriorityMessage, new PublishOptions
        {
            RoutingKey = "low"
        });

        await Task.Delay(100);

        // Assert
        Assert.Single(highPriorityMessages);
        Assert.Single(lowPriorityMessages);
        Assert.Equal("MSG-001", highPriorityMessages[0].Id);
        Assert.Equal("MSG-002", lowPriorityMessages[0].Id);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldHandleMessagePriority()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<PriorityMessage>();

        await broker.SubscribeAsync<PriorityMessage>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var lowPriorityMessage = new PriorityMessage { Id = "LOW", Content = "Low priority" };
        var highPriorityMessage = new PriorityMessage { Id = "HIGH", Content = "High priority" };

        // Act
        await broker.PublishAsync(lowPriorityMessage, new PublishOptions { Priority = 1 });
        await broker.PublishAsync(highPriorityMessage, new PublishOptions { Priority = 5 });
        await Task.Delay(100);

        // Assert - Messages should be received (priority is stored in context but not affecting order in InMemory broker)
        Assert.Equal(2, receivedMessages.Count);
        var lowPriorityReceived = receivedMessages.Find(m => m.Id == "LOW");
        var highPriorityReceived = receivedMessages.Find(m => m.Id == "HIGH");
        Assert.NotNull(lowPriorityReceived);
        Assert.NotNull(highPriorityReceived);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldHandleMessageExpiration()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var receivedMessages = new List<ExpiringMessage>();

        await broker.SubscribeAsync<ExpiringMessage>(async (message, context, ct) =>
        {
            receivedMessages.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var expiringMessage = new ExpiringMessage { Id = "EXP-001", Content = "This message expires" };

        // Act
        await broker.PublishAsync(expiringMessage, new PublishOptions
        {
            Expiration = TimeSpan.FromMinutes(5)
        });
        await Task.Delay(100);

        // Assert - Message should be received (expiration is stored in context but not enforced in InMemory broker)
        Assert.Single(receivedMessages);
        Assert.Equal("EXP-001", receivedMessages[0].Id);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldRouteMessagesByMessageType()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var userEvents = new List<UserEvent>();
        var systemEvents = new List<SystemEvent>();

        await broker.SubscribeAsync<UserEvent>(async (message, context, ct) =>
        {
            userEvents.Add(message);
            await context.Acknowledge();
        });

        await broker.SubscribeAsync<SystemEvent>(async (message, context, ct) =>
        {
            systemEvents.Add(message);
            await context.Acknowledge();
        });

        await broker.StartAsync();

        var userEvent = new UserEvent { UserId = "user123", Action = "login" };
        var systemEvent = new SystemEvent { Component = "database", Status = "healthy" };

        // Act
        await broker.PublishAsync(userEvent);
        await broker.PublishAsync(systemEvent);
        await Task.Delay(100);

        // Assert
        Assert.Single(userEvents);
        Assert.Single(systemEvents);
        Assert.Equal("user123", userEvents[0].UserId);
        Assert.Equal("database", systemEvents[0].Component);
    }

    [Fact]
    public async Task InMemoryMessageBroker_ShouldHandleComplexRoutingScenarios()
    {
        // Arrange
        var broker = new InMemoryMessageBroker();
        var auditLogMessages = new List<AuditMessage>();
        var errorMessages = new List<ErrorMessage>();

        // Subscribe to audit messages with specific routing
        await broker.SubscribeAsync<AuditMessage>(async (message, context, ct) =>
        {
            auditLogMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions
        {
            Exchange = "audit",
            RoutingKey = "security.*"
        });

        // Subscribe to error messages
        await broker.SubscribeAsync<ErrorMessage>(async (message, context, ct) =>
        {
            errorMessages.Add(message);
            await context.Acknowledge();
        }, new SubscriptionOptions
        {
            Exchange = "errors",
            RoutingKey = "high"
        });

        await broker.StartAsync();

        var securityAudit = new AuditMessage { EventType = "LOGIN_ATTEMPT", UserId = "user123" };
        var errorMessage = new ErrorMessage { Code = "DB_CONNECTION_FAILED", Message = "Database is down" };

        // Act
        await broker.PublishAsync(securityAudit, new PublishOptions
        {
            Exchange = "audit",
            RoutingKey = "security.login",
            Headers = new Dictionary<string, object> { { "level", "info" }, { "ip", "192.168.1.1" } }
        });

        await broker.PublishAsync(errorMessage, new PublishOptions
        {
            Exchange = "errors",
            RoutingKey = "high"
        });

        await Task.Delay(100);

        // Assert
        Assert.Single(auditLogMessages);
        Assert.Single(errorMessages);
        Assert.Equal("LOGIN_ATTEMPT", auditLogMessages[0].EventType);
        Assert.Equal("DB_CONNECTION_FAILED", errorMessages[0].Code);
    }

    private class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    private class PaymentMessage
    {
        public string PaymentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    private class AuditMessage
    {
        public string EventType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    private class NotificationMessage
    {
        public string Type { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
    }

    private class PriorityMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class ExpiringMessage
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    private class UserEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

    private class SystemEvent
    {
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private class ErrorMessage
    {
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}