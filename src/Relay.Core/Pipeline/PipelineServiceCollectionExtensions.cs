using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Transactions;

namespace Relay.Core.Pipeline
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the pipeline behaviors as open generics
            // This allows them to work with any TRequest and TResponse types
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(RequestPreProcessorBehavior<,>)));

            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(RequestPostProcessorBehavior<,>)));

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register exception handler behavior
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(RequestExceptionHandlerBehavior<,>)));

            // Register exception action behavior
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(RequestExceptionActionBehavior<,>)));

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestPreProcessor<TRequest>),
                typeof(TPreProcessor),
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestPreProcessor<TRequest>),
                implementationFactory,
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestPostProcessor<TRequest, TResponse>),
                typeof(TPostProcessor),
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestPostProcessor<TRequest, TResponse>),
                implementationFactory,
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestExceptionHandler<TRequest, TResponse, TException>),
                typeof(THandler),
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestExceptionHandler<TRequest, TResponse, TException>),
                implementationFactory,
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestExceptionAction<TRequest, TException>),
                typeof(TAction),
                lifetime);

            services.Add(descriptor);

            return services;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            var descriptor = new ServiceDescriptor(
                typeof(IRequestExceptionAction<TRequest, TException>),
                implementationFactory,
                lifetime);

            services.Add(descriptor);

            return services;
        }

        /// <summary>
        /// Adds transaction support using TransactionScope for requests implementing ITransactionalRequest.
        /// Automatically wraps handler execution in a transaction that commits on success and rolls back on failure.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="scopeOption">Transaction scope option (default: Required).</param>
        /// <param name="isolationLevel">Transaction isolation level (default: ReadCommitted).</param>
        /// <param name="timeout">Transaction timeout (default: 1 minute).</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayTransactions(
            this IServiceCollection services,
            TransactionScopeOption scopeOption = TransactionScopeOption.Required,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            TimeSpan? timeout = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var actualTimeout = timeout ?? TimeSpan.FromMinutes(1);

            // Store configuration
            services.Configure<Transactions.TransactionOptions>(options =>
            {
                options.ScopeOption = scopeOption;
                options.IsolationLevel = isolationLevel;
                options.Timeout = actualTimeout;
            });

            // Register TransactionBehavior as open generic
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(Transactions.TransactionBehavior<,>)));

            return services;
        }

        /// <summary>
        /// Adds Unit of Work pattern support that automatically calls SaveChangesAsync after successful handler execution.
        /// Works with any IUnitOfWork implementation (EF Core DbContext, custom repositories, etc.).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="saveOnlyForTransactionalRequests">
        /// If true, only saves changes for requests implementing ITransactionalRequest.
        /// If false, saves changes for all requests. Default is false.
        /// </param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayUnitOfWork(
            this IServiceCollection services,
            bool saveOnlyForTransactionalRequests = false)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register UnitOfWorkBehavior as open generic
            services.TryAddEnumerable(
                ServiceDescriptor.Transient(
                    typeof(IPipelineBehavior<,>),
                    typeof(Transactions.UnitOfWorkBehavior<,>)));

            // Store configuration
            services.Configure<Transactions.UnitOfWorkOptions>(options =>
            {
                options.SaveOnlyForTransactionalRequests = saveOnlyForTransactionalRequests;
            });

            return services;
        }
    }
}
