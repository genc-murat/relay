using System;

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
