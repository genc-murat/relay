using System;
using System.Linq.Expressions;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for creating test response objects.
/// </summary>
/// <typeparam name="TResponse">The type of response to build.</typeparam>
public class ResponseBuilder<TResponse> : TestDataBuilder<TResponse>
    where TResponse : class, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseBuilder{TResponse}"/> class.
    /// </summary>
    public ResponseBuilder()
    {
        // Set default values
        WithDefaults();
    }

    /// <summary>
    /// Sets sensible default values for the response.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public ResponseBuilder<TResponse> WithDefaults()
    {
        // Set default success status if the response has one
        var successProperty = typeof(TResponse).GetProperty("Success");
        if (successProperty != null && successProperty.PropertyType == typeof(bool))
        {
            successProperty.SetValue(Instance, true);
        }

        var isSuccessProperty = typeof(TResponse).GetProperty("IsSuccess");
        if (isSuccessProperty != null && isSuccessProperty.PropertyType == typeof(bool))
        {
            isSuccessProperty.SetValue(Instance, true);
        }

        // Set default message if present
        var messageProperty = typeof(TResponse).GetProperty("Message");
        if (messageProperty != null && messageProperty.PropertyType == typeof(string))
        {
            messageProperty.SetValue(Instance, "Success");
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
    public ResponseBuilder<TResponse> WithProperty<TProperty>(
        Expression<Func<TResponse, TProperty>> property,
        TProperty value)
    {
        var memberExpression = property.Body as MemberExpression;
        if (memberExpression == null)
        {
            throw new ArgumentException("Expression must be a member expression", nameof(property));
        }

        var propertyInfo = typeof(TResponse).GetProperty(memberExpression.Member.Name);
        if (propertyInfo == null)
        {
            throw new ArgumentException($"Property '{memberExpression.Member.Name}' not found on type '{typeof(TResponse).Name}'");
        }

        propertyInfo.SetValue(Instance, value);
        return this;
    }

    /// <summary>
    /// Sets the response as successful.
    /// </summary>
    /// <returns>The current builder instance for method chaining.</returns>
    public ResponseBuilder<TResponse> WithSuccess()
    {
        var successProperty = typeof(TResponse).GetProperty("Success");
        if (successProperty != null)
        {
            successProperty.SetValue(Instance, true);
        }

        var isSuccessProperty = typeof(TResponse).GetProperty("IsSuccess");
        if (isSuccessProperty != null)
        {
            isSuccessProperty.SetValue(Instance, true);
        }

        return this;
    }

    /// <summary>
    /// Sets the response as failed.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>The current builder instance for method chaining.</returns>
    public ResponseBuilder<TResponse> WithFailure(string message = "Operation failed")
    {
        var successProperty = typeof(TResponse).GetProperty("Success");
        if (successProperty != null)
        {
            successProperty.SetValue(Instance, false);
        }

        var isSuccessProperty = typeof(TResponse).GetProperty("IsSuccess");
        if (isSuccessProperty != null)
        {
            isSuccessProperty.SetValue(Instance, false);
        }

        var messageProperty = typeof(TResponse).GetProperty("Message");
        if (messageProperty != null)
        {
            messageProperty.SetValue(Instance, message);
        }

        var errorMessageProperty = typeof(TResponse).GetProperty("ErrorMessage");
        if (errorMessageProperty != null)
        {
            errorMessageProperty.SetValue(Instance, message);
        }

        return this;
    }

    /// <summary>
    /// Builds and validates the response.
    /// </summary>
    /// <returns>The built and validated response.</returns>
    public override TResponse Build()
    {
        // Perform basic validation
        if (Instance == null)
        {
            throw new InvalidOperationException("Response instance is null");
        }

        return Instance;
    }
}