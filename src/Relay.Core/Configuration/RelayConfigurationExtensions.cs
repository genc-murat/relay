using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Relay.Core.Configuration
{
    /// <summary>
    /// Extension methods for configuring Relay framework options.
    /// </summary>
    public static class RelayConfigurationExtensions
    {
        /// <summary>
        /// Adds Relay configuration services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayConfiguration(this IServiceCollection services)
        {
            services.Configure<RelayOptions>(options => { }); // Register default options
            services.AddSingleton<IConfigurationResolver, ConfigurationResolver>();
            return services;
        }

        /// <summary>
        /// Configures Relay options using the provided configuration action.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureRelay(this IServiceCollection services, Action<RelayOptions> configureOptions)
        {
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddRelayConfiguration();
            return services;
        }

        /// <summary>
        /// Configures Relay options using the provided configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration section.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureRelay(this IServiceCollection services, IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            services.Configure<RelayOptions>(options => configuration.Bind(options));
            services.AddRelayConfiguration();
            return services;
        }

        /// <summary>
        /// Configures Relay options using the provided configuration section name.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration root.</param>
        /// <param name="sectionName">The name of the configuration section.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureRelay(this IServiceCollection services, IConfiguration configuration, string sectionName)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (string.IsNullOrEmpty(sectionName)) throw new ArgumentException("Section name cannot be null or empty.", nameof(sectionName));

            var section = configuration.GetSection(sectionName);
            return services.ConfigureRelay(section);
        }

        /// <summary>
        /// Configures handler-specific options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="handlerKey">The handler key (Type.FullName.MethodName).</param>
        /// <param name="configureOptions">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureHandler(this IServiceCollection services, string handlerKey, Action<HandlerOptions> configureOptions)
        {
            if (string.IsNullOrEmpty(handlerKey)) throw new ArgumentException("Handler key cannot be null or empty.", nameof(handlerKey));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure<RelayOptions>(options =>
            {
                if (!options.HandlerOverrides.ContainsKey(handlerKey))
                {
                    options.HandlerOverrides[handlerKey] = new HandlerOptions();
                }
                configureOptions(options.HandlerOverrides[handlerKey]);
            });

            return services;
        }

        /// <summary>
        /// Configures notification-specific options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="notificationKey">The notification handler key (Type.FullName.MethodName).</param>
        /// <param name="configureOptions">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureNotification(this IServiceCollection services, string notificationKey, Action<NotificationOptions> configureOptions)
        {
            if (string.IsNullOrEmpty(notificationKey)) throw new ArgumentException("Notification key cannot be null or empty.", nameof(notificationKey));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure<RelayOptions>(options =>
            {
                if (!options.NotificationOverrides.ContainsKey(notificationKey))
                {
                    options.NotificationOverrides[notificationKey] = new NotificationOptions();
                }
                configureOptions(options.NotificationOverrides[notificationKey]);
            });

            return services;
        }

        /// <summary>
        /// Configures pipeline-specific options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="pipelineKey">The pipeline key (Type.FullName.MethodName).</param>
        /// <param name="configureOptions">The configuration action.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigurePipeline(this IServiceCollection services, string pipelineKey, Action<PipelineOptions> configureOptions)
        {
            if (string.IsNullOrEmpty(pipelineKey)) throw new ArgumentException("Pipeline key cannot be null or empty.", nameof(pipelineKey));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.Configure<RelayOptions>(options =>
            {
                if (!options.PipelineOverrides.ContainsKey(pipelineKey))
                {
                    options.PipelineOverrides[pipelineKey] = new PipelineOptions();
                }
                configureOptions(options.PipelineOverrides[pipelineKey]);
            });

            return services;
        }

        /// <summary>
        /// Adds and configures in-memory caching for Relay requests.
        /// This registers the <see cref="Caching.CachingPipelineBehavior{TRequest, TResponse}"/>
        /// and the required <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> service.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayCaching(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Caching.CachingPipelineBehavior<,>));
            return services;
        }

        /// <summary>
        /// Adds and configures advanced caching for Relay requests.
        /// This registers the <see cref="Caching.AdvancedCachingPipelineBehavior{TRequest, TResponse}"/>
        /// and the required caching services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayAdvancedCaching(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Caching.AdvancedCachingPipelineBehavior<,>));
            return services;
        }

        /// <summary>
        /// Adds and configures rate limiting for Relay requests.
        /// This registers the <see cref="RateLimiting.RateLimitingPipelineBehavior{TRequest, TResponse}"/>
        /// and the required rate limiting services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayRateLimiting(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddTransient<IRateLimiter, InMemoryRateLimiter>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RateLimiting.RateLimitingPipelineBehavior<,>));
            return services;
        }

        /// <summary>
        /// Adds and configures authorization for Relay requests.
        /// This registers the <see cref="Authorization.AuthorizationPipelineBehavior{TRequest, TResponse}"/>
        /// and the required authorization services.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayAuthorization(this IServiceCollection services)
        {
            services.AddTransient<IAuthorizationService, DefaultAuthorizationService>();
            services.AddTransient<IAuthorizationContext, DefaultAuthorizationContext>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Authorization.AuthorizationPipelineBehavior<,>));
            return services;
        }

        /// <summary>
        /// Validates the Relay configuration and throws an exception if invalid.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ValidateRelayConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IValidateOptions<RelayOptions>, RelayOptionsValidator>();
            return services;
        }
    }
}