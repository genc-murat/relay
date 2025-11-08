using System;
using System.Linq.Expressions;
using Xunit;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Tests.Builders;

public class NotificationBuilderTests
{
    [Fact]
    public void Constructor_WithDefaults_SetsTimestampProperty()
    {
        // Arrange & Act
        var builder = new NotificationBuilder<TestNotificationWithTimestamp>();

        // Assert
        var notification = builder.Build();
        Assert.NotEqual(default(DateTimeOffset), notification.Timestamp);
    }

    [Fact]
    public void WithDefaults_SetsCreatedAtProperty()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithCreatedAt>();

        // Act
        builder.WithDefaults();

        // Assert
        var notification = builder.Build();
        Assert.NotEqual(default(DateTimeOffset), notification.CreatedAt);
    }

    [Fact]
    public void WithDefaults_NoTimestampProperties_DoesNothing()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationNoTimestamp>();

        // Act
        builder.WithDefaults();

        // Assert
        var notification = builder.Build();
        Assert.NotNull(notification);
    }

    [Fact]
    public void WithProperty_ValidExpression_SetsPropertyValue()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithMessage>();
        var expectedMessage = "Test Message";

        // Act
        builder.WithProperty(n => n.Message, expectedMessage);

        // Assert
        var notification = builder.Build();
        Assert.Equal(expectedMessage, notification.Message);
    }

    [Fact]
    public void WithProperty_InvalidExpression_ThrowsArgumentException()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithMessage>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty<string>(n => n.Message.Length.ToString(), "value"));
        Assert.Contains("Expression must be a member expression", exception.Message);
    }

    [Fact]
    public void WithProperty_PropertyNotFound_ThrowsArgumentException()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithField>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            builder.WithProperty(n => n.SomeField, "value"));
        Assert.Contains("Property 'SomeField' not found on type 'TestNotificationWithField'", exception.Message);
    }



    [Fact]
    public void WithTimestamp_TimestampPropertyExists_SetsValue()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithTimestamp>();
        var expectedTimestamp = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        builder.WithTimestamp(expectedTimestamp);

        // Assert
        var notification = builder.Build();
        Assert.Equal(expectedTimestamp, notification.Timestamp);
    }

    [Fact]
    public void WithTimestamp_CreatedAtPropertyExists_SetsValue()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationWithCreatedAt>();
        var expectedTimestamp = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        builder.WithTimestamp(expectedTimestamp);

        // Assert
        var notification = builder.Build();
        Assert.Equal(expectedTimestamp, notification.CreatedAt);
    }

    [Fact]
    public void WithTimestamp_NoPropertiesExist_DoesNothing()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationNoTimestamp>();
        var timestamp = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        builder.WithTimestamp(timestamp);

        // Assert
        var notification = builder.Build();
        Assert.NotNull(notification);
    }

    [Fact]
    public void Build_ReturnsInstance()
    {
        // Arrange
        var builder = new NotificationBuilder<TestNotificationNoTimestamp>();

        // Act
        var notification = builder.Build();

        // Assert
        Assert.NotNull(notification);
        Assert.IsType<TestNotificationNoTimestamp>(notification);
    }

    // Test notification classes
    public class TestNotificationWithTimestamp : INotification
    {
        public DateTimeOffset Timestamp { get; set; }
    }

    public class TestNotificationWithCreatedAt : INotification
    {
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class TestNotificationNoTimestamp : INotification
    {
    }

    public class TestNotificationWithMessage : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TestNotificationWithField : INotification
    {
        public string SomeField = string.Empty;
    }
}