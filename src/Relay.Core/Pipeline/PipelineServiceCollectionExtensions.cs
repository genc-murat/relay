using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Relay.Core.Pipeline
{
    /// <summary>
    /// Extension methods for registering pre/post processor pipeline behaviors.
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
    }
}
