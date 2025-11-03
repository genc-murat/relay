using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Behaviors;
using Relay.Core.Pipeline.Interfaces;
using Relay.Core.Extensions;
using System;

namespace Relay.Core.Pipeline.Extensions
{
    /// <summary>
    /// Extension methods for registering pipeline behaviors (pre/post processors, exception handlers, transactions).
    /// </summary>
    public static class PipelineServiceCollectionExtensions
    {
        /// <summary>
        /// Adds pre-processor and post-processor pipeline behaviors to the service collection.
        /// Pre-processors run before the handler, post-processors run after the handler completes successfully.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayPrePostProcessors(this IServiceCollection services)
        {
            return services.RegisterCoreServices(svc =>
            {
                // Register the pipeline behaviors as open generics
                // This allows them to work with any TRequest and TResponse types
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(RequestPreProcessorBehavior<,>));
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(RequestPostProcessorBehavior<,>));
            });
        }

        /// <summary>
        /// Adds exception handler and exception action pipeline behaviors to the service collection.
        /// Exception handlers can catch and handle exceptions, optionally providing a response.
        /// Exception actions execute for side effects like logging but cannot suppress exceptions.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayExceptionHandlers(this IServiceCollection services)
        {
            return services.RegisterCoreServices(svc =>
            {
                // Register exception handler behavior
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(RequestExceptionHandlerBehavior<,>));

                // Register exception action behavior
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(RequestExceptionActionBehavior<,>));
            });
        }

        /// <summary>
        /// Adds a pre-processor for a specific request type.
        /// Pre-processors execute before the handler and all pipeline behaviors.
        /// Multiple pre-processors can be registered and will execute in registration order.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TPreProcessor">The type of pre-processor implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPreProcessor<TRequest, TPreProcessor>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TPreProcessor : class, IRequestPreProcessor<TRequest>
        {
            return ServiceRegistrationHelper.AddService<IRequestPreProcessor<TRequest>, TPreProcessor>(services, lifetime);
        }

        /// <summary>
        /// Adds a pre-processor with a factory function for a specific request type.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TPreProcessor">The type of pre-processor implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the pre-processor.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPreProcessor<TRequest, TPreProcessor>(
            this IServiceCollection services,
            Func<IServiceProvider, TPreProcessor> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TPreProcessor : class, IRequestPreProcessor<TRequest>
        {
            return ServiceRegistrationHelper.AddService(services, implementationFactory, lifetime);
        }

        /// <summary>
        /// Adds a post-processor for a specific request and response type.
        /// Post-processors execute after the handler and all pipeline behaviors complete successfully.
        /// Multiple post-processors can be registered and will execute in registration order.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <typeparam name="TPostProcessor">The type of post-processor implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPostProcessor<TRequest, TResponse, TPostProcessor>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TPostProcessor : class, IRequestPostProcessor<TRequest, TResponse>
        {
            return ServiceRegistrationHelper.AddService<IRequestPostProcessor<TRequest, TResponse>, TPostProcessor>(services, lifetime);
        }

        /// <summary>
        /// Adds a post-processor with a factory function for a specific request and response type.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <typeparam name="TPostProcessor">The type of post-processor implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the post-processor.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddPostProcessor<TRequest, TResponse, TPostProcessor>(
            this IServiceCollection services,
            Func<IServiceProvider, TPostProcessor> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TPostProcessor : class, IRequestPostProcessor<TRequest, TResponse>
        {
            return ServiceRegistrationHelper.AddService(services, implementationFactory, lifetime);
        }

        /// <summary>
        /// Adds an exception handler for a specific request, response, and exception type.
        /// Exception handlers can catch exceptions and optionally provide a response to suppress the exception.
        /// Multiple handlers can be registered and will execute in order until one handles the exception.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <typeparam name="TException">The type of exception to handle.</typeparam>
        /// <typeparam name="THandler">The type of exception handler implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddExceptionHandler<TRequest, TResponse, TException, THandler>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TException : Exception
            where THandler : class, IRequestExceptionHandler<TRequest, TResponse, TException>
        {
            return ServiceRegistrationHelper.AddService<IRequestExceptionHandler<TRequest, TResponse, TException>, THandler>(services, lifetime);
        }

        /// <summary>
        /// Adds an exception handler with a factory function.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TResponse">The type of response.</typeparam>
        /// <typeparam name="TException">The type of exception to handle.</typeparam>
        /// <typeparam name="THandler">The type of exception handler implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the handler.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddExceptionHandler<TRequest, TResponse, TException, THandler>(
            this IServiceCollection services,
            Func<IServiceProvider, THandler> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TException : Exception
            where THandler : class, IRequestExceptionHandler<TRequest, TResponse, TException>
        {
            return ServiceRegistrationHelper.AddService(services, implementationFactory, lifetime);
        }

        /// <summary>
        /// Adds an exception action for a specific request and exception type.
        /// Exception actions execute for side effects (like logging) but cannot suppress exceptions.
        /// Multiple actions can be registered and all will execute regardless of exceptions in other actions.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TException">The type of exception to handle.</typeparam>
        /// <typeparam name="TAction">The type of exception action implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddExceptionAction<TRequest, TException, TAction>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TException : Exception
            where TAction : class, IRequestExceptionAction<TRequest, TException>
        {
            return ServiceRegistrationHelper.AddService<IRequestExceptionAction<TRequest, TException>, TAction>(services, lifetime);
        }

        /// <summary>
        /// Adds an exception action with a factory function.
        /// </summary>
        /// <typeparam name="TRequest">The type of request.</typeparam>
        /// <typeparam name="TException">The type of exception to handle.</typeparam>
        /// <typeparam name="TAction">The type of exception action implementation.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="implementationFactory">The factory function to create the action.</param>
        /// <param name="lifetime">The service lifetime (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddExceptionAction<TRequest, TException, TAction>(
            this IServiceCollection services,
            Func<IServiceProvider, TAction> implementationFactory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where TException : Exception
            where TAction : class, IRequestExceptionAction<TRequest, TException>
        {
            return ServiceRegistrationHelper.AddService(services, implementationFactory, lifetime);
        }

        /// <summary>
        /// Adds the unified transaction pipeline behavior for requests implementing <see cref="Relay.Core.Transactions.ITransactionalRequest"/>.
        /// This behavior automatically begins a transaction, saves changes via <see cref="Relay.Core.Transactions.IUnitOfWork"/>,
        /// and commits on success or rolls back on failure.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para><strong>OBSOLETE:</strong> This method is obsolete and will be removed in a future version.</para>
        /// <para>Use <see cref="Relay.Core.Transactions.TransactionServiceCollectionExtensions.AddRelayTransactions(IServiceCollection, Action{Relay.Core.Transactions.TransactionOptions})"/> instead.</para>
        /// <para>The new transaction system requires explicit configuration and provides enhanced features including:</para>
        /// <list type="bullet">
        /// <item><description>Mandatory isolation level specification</description></item>
        /// <item><description>Transaction timeout support</description></item>
        /// <item><description>Retry policies for transient failures</description></item>
        /// <item><description>Nested transaction and savepoint support</description></item>
        /// <item><description>Transaction event hooks</description></item>
        /// <item><description>Comprehensive telemetry and metrics</description></item>
        /// </list>
        /// </remarks>
        [Obsolete("This method is obsolete. Use Relay.Core.Transactions.TransactionServiceCollectionExtensions.AddRelayTransactions instead. " +
                  "The new transaction system requires explicit TransactionAttribute with IsolationLevel on all ITransactionalRequest implementations. " +
                  "See BREAKING_CHANGES.md for migration guide.", error: false)]
        public static IServiceCollection AddRelayTransactions(this IServiceCollection services)
        {
            // Delegate to the new implementation with default options
            return Relay.Core.Transactions.TransactionServiceCollectionExtensions.AddRelayTransactions(services);
        }
    }
}
