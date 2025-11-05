using System;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Infrastructure;

namespace Relay.Core.Testing;

/// <summary>
/// Extension methods for TestRelay to support mock handler registration.
/// </summary>
public static class TestRelayExtensions
{
    /// <summary>
    /// Registers a mock handler for the specified request type.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="builder">The mock handler builder.</param>
    /// <returns>The TestRelay instance for chaining.</returns>
    public static TestRelay WithMockHandler<TRequest, TResponse>(
        this TestRelay relay,
        MockHandlerBuilder<TRequest, TResponse> builder)
        where TRequest : IRequest<TResponse>
    {
        var handler = builder.Build();
        relay.SetupRequestHandler<TRequest, TResponse>(handler);
        return relay;
    }

    /// <summary>
    /// Registers a mock handler for the specified request type using a builder action.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="relay">The TestRelay instance.</param>
    /// <param name="configureBuilder">Action to configure the mock handler builder.</param>
    /// <returns>The TestRelay instance for chaining.</returns>
    public static TestRelay WithMockHandler<TRequest, TResponse>(
        this TestRelay relay,
        Action<MockHandlerBuilder<TRequest, TResponse>> configureBuilder)
        where TRequest : IRequest<TResponse>
    {
        var builder = new MockHandlerBuilder<TRequest, TResponse>();
        configureBuilder(builder);
        return relay.WithMockHandler(builder);
    }


}