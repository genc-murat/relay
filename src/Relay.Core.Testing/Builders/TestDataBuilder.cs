using System;
using System.Linq.Expressions;

namespace Relay.Core.Testing;

/// <summary>
/// Base class for fluent test data builders.
/// </summary>
/// <typeparam name="T">The type of object being built.</typeparam>
public abstract class TestDataBuilder<T> where T : class
{
    /// <summary>
    /// Gets the instance being built.
    /// </summary>
    internal T Instance { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestDataBuilder{T}"/> class.
    /// </summary>
    protected TestDataBuilder()
    {
        Instance = Activator.CreateInstance<T>();
    }

    /// <summary>
    /// Configures the instance using an action.
    /// </summary>
    /// <param name="configure">The action to configure the instance.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public TestDataBuilder<T> With(Action<T> configure)
    {
        configure?.Invoke(Instance);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured instance.
    /// </summary>
    /// <returns>The built instance.</returns>
    public virtual T Build()
    {
        return Instance;
    }
}

/// <summary>
/// Extension methods for test data builders.
/// </summary>
public static class TestDataBuilderExtensions
{
    /// <summary>
    /// Sets a property value using an expression.
    /// </summary>
    /// <typeparam name="T">The type of object being built.</typeparam>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="propertyExpression">Expression identifying the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public static TestDataBuilder<T> WithProperty<T, TProperty>(
        this TestDataBuilder<T> builder,
        Expression<Func<T, TProperty>> propertyExpression,
        TProperty value) where T : class
    {
        var memberExpression = propertyExpression.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(propertyExpression));
        }

        var propertyInfo = typeof(T).GetProperty(memberExpression.Member.Name);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property '{memberExpression.Member.Name}' not found on type '{typeof(T).Name}'");
        }

        propertyInfo.SetValue(builder.Instance, value);
        return builder;
    }
}