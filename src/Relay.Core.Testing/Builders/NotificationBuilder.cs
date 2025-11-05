using System;
using System.Linq.Expressions;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for creating test notification objects.
/// </summary>
/// <typeparam name="TNotification">The type of notification to build.</typeparam>
public class NotificationBuilder<TNotification> : TestDataBuilder<TNotification>
    where TNotification : class, INotification, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationBuilder{TNotification}"/> class.
    /// </summary>
    public NotificationBuilder()
    {
        // Set default values
        WithDefaults();
    }

    /// <summary>
    /// Sets sensible default values for the notification.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public NotificationBuilder<TNotification> WithDefaults()
    {
        // Set default timestamp if the notification has one
        var timestampProperty = typeof(TNotification).GetProperty("Timestamp");
        if (timestampProperty != null && timestampProperty.PropertyType == typeof(DateTimeOffset))
        {
            timestampProperty.SetValue(Instance, DateTimeOffset.UtcNow);
        }

        var timestampProperty2 = typeof(TNotification).GetProperty("CreatedAt");
        if (timestampProperty2 != null && timestampProperty2.PropertyType == typeof(DateTimeOffset))
        {
            timestampProperty2.SetValue(Instance, DateTimeOffset.UtcNow);
        }

        return this;
    }

    /// <summary>
    /// Sets a property value using an expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">Expression identifying the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public NotificationBuilder<TNotification> WithProperty<TProperty>(
        Expression<Func<TNotification, TProperty>> property,
        TProperty value)
    {
        var memberExpression = property.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(property));
        }

        var propertyInfo = typeof(TNotification).GetProperty(memberExpression.Member.Name);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property '{memberExpression.Member.Name}' not found on type '{typeof(TNotification).Name}'");
        }

        propertyInfo.SetValue(Instance, value);
        return this;
    }

    /// <summary>
    /// Sets the timestamp for the notification.
    /// </summary>
    /// <param name="timestamp">The timestamp to set.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public NotificationBuilder<TNotification> WithTimestamp(DateTimeOffset timestamp)
    {
        var timestampProperty = typeof(TNotification).GetProperty("Timestamp");
        if (timestampProperty != null)
        {
            timestampProperty.SetValue(Instance, timestamp);
        }

        var createdAtProperty = typeof(TNotification).GetProperty("CreatedAt");
        if (createdAtProperty != null)
        {
            createdAtProperty.SetValue(Instance, timestamp);
        }

        return this;
    }

    /// <summary>
    /// Builds and validates the notification.
    /// </summary>
    /// <returns>The built and validated notification.</returns>
    public override TNotification Build()
    {
        // Perform basic validation
        if (Instance == null)
        {
            throw new InvalidOperationException("Notification instance is null");
        }

        return Instance;
    }
}