using System;
using System.Linq.Expressions;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for creating test request objects.
/// </summary>
/// <typeparam name="TRequest">The type of request to build.</typeparam>
public class RequestBuilder<TRequest> : TestDataBuilder<TRequest>
    where TRequest : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestBuilder{TRequest}"/> class.
    /// </summary>
    public RequestBuilder()
    {
        // Set default values
        WithDefaults();
    }

    /// <summary>
    /// Sets sensible default values for the request.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public RequestBuilder<TRequest> WithDefaults()
    {
        // This method can be overridden in derived classes to provide
        // type-specific default values
        return this;
    }

    /// <summary>
    /// Sets a property value using an expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">Expression identifying the property.</param>
    /// <param name="value">The value to set.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public RequestBuilder<TRequest> WithProperty<TProperty>(
        Expression<Func<TRequest, TProperty>> property,
        TProperty value)
    {
        var memberExpression = property.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(property));
        }

        var propertyInfo = typeof(TRequest).GetProperty(memberExpression.Member.Name);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property '{memberExpression.Member.Name}' not found on type '{typeof(TRequest).Name}'");
        }

        propertyInfo.SetValue(Instance, value);
        return this;
    }

    /// <summary>
    /// Builds and validates the request.
    /// </summary>
    /// <returns>The built and validated request.</returns>
    public override TRequest Build()
    {
        // Perform basic validation
        if (Instance == null)
        {
            throw new InvalidOperationException("Request instance is null");
        }

        return Instance;
    }
}