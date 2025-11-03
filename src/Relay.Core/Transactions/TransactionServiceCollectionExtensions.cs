using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Extensions;
using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Extension methods for registering transaction services with the dependency injection container.
    /// </summary>
    /// <remarks>
    /// This class provides extension methods for configuring transaction-related services, including
    /// event handlers, coordinators, and other transaction infrastructure components.
    /// 
    /// <para><strong>Basic Usage:</strong></para>
    /// <code>
    /// services.AddRelayTransactions(options =>
    /// {
    ///     options.DefaultTimeout = TimeSpan.FromSeconds(30);
    ///     options.EnableMetrics = true;
    /// });
    /// </code>
    /// 
    /// <para><strong>Registering Event Handlers:</strong></para>
    /// <code>
    /// services.AddTransactionEventHandler&lt;AuditEventHandler&gt;();
    /// services.AddTransactionEventHandler&lt;CacheInvalidationHandler&gt;();
    /// </code>
    /// </remarks>
    public static class TransactionServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the complete Relay transaction system to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Optional action to configure transaction options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method registers all transaction infrastructure services including:
        /// <list type="bullet">
        /// <item><description>Transaction pipeline behavior</description></item>
        /// <item><description>Transaction coordinator and distributed transaction coordinator</description></item>
        /// <item><description>Transaction event publisher</description></item>
        /// <item><description>Transaction configuration resolver</description></item>
        /// <item><description>Transaction retry handler</description></item>
        /// <item><description>Nested transaction manager</description></item>
        /// <item><description>Transaction metrics collector</description></item>
        /// <item><description>Transaction health check</description></item>
        /// <item><description>Transaction telemetry services</description></item>
        /// </list>
        /// </para>
        /// 
        /// <para><strong>BREAKING CHANGE:</strong> This replaces the old transaction system completely.</para>
        /// 
        /// <para><strong>Basic Usage:</strong></para>
        /// <code>
        /// services.AddRelayTransactions();
        /// </code>
        /// 
        /// <para><strong>With Configuration:</strong></para>
        /// <code>
        /// services.AddRelayTransactions(options =>
        /// {
        ///     options.DefaultTimeout = TimeSpan.FromSeconds(60);
        ///     options.EnableMetrics = true;
        ///     options.EnableDistributedTracing = true;
        ///     options.RequireExplicitTransactionAttribute = true;
        /// });
        /// </code>
        /// 
        /// <para><strong>With Configuration Section:</strong></para>
        /// <code>
        /// services.AddRelayTransactions(configuration.GetSection("Relay:Transactions"));
        /// </code>
        /// 
        /// <para><strong>Requirements:</strong></para>
        /// <list type="bullet">
        /// <item><description>All ITransactionalRequest implementations MUST have TransactionAttribute</description></item>
        /// <item><description>TransactionAttribute MUST specify an explicit IsolationLevel (not Unspecified)</description></item>
        /// <item><description>IUnitOfWork implementations must support the new BeginTransactionAsync(IsolationLevel) signature</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddRelayTransactions(
            this IServiceCollection services,
            Action<TransactionOptions>? configureOptions = null)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register and configure options
            var optionsBuilder = services.AddOptions<TransactionOptions>();
            
            if (configureOptions != null)
            {
                optionsBuilder.Configure(configureOptions);
            }

            // Add options validation
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TransactionOptions>, TransactionOptionsValidator>());

            // Register all transaction infrastructure services
            services.AddTransactionInfrastructure();

            // Register the new TransactionBehavior as a pipeline behavior
            services.RegisterCoreServices(svc =>
            {
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });

            return services;
        }

        /// <summary>
        /// Adds the complete Relay transaction system to the service collection with configuration from a configuration section.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configurationSection">The configuration section containing transaction options.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// <para>
        /// This overload binds transaction options from a configuration section (e.g., appsettings.json).
        /// </para>
        /// 
        /// <para><strong>Configuration Example (appsettings.json):</strong></para>
        /// <code>
        /// {
        ///   "Relay": {
        ///     "Transactions": {
        ///       "DefaultTimeoutSeconds": 30,
        ///       "EnableMetrics": true,
        ///       "EnableDistributedTracing": true,
        ///       "EnableNestedTransactions": true,
        ///       "EnableSavepoints": true,
        ///       "RequireExplicitTransactionAttribute": true
        ///     }
        ///   }
        /// }
        /// </code>
        /// 
        /// <para><strong>Usage:</strong></para>
        /// <code>
        /// services.AddRelayTransactions(configuration.GetSection("Relay:Transactions"));
        /// </code>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services or configurationSection is null.</exception>
        public static IServiceCollection AddRelayTransactions(
            this IServiceCollection services,
            IConfigurationSection configurationSection)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configurationSection == null)
                throw new ArgumentNullException(nameof(configurationSection));

            // Register and bind options from configuration
            var optionsBuilder = services.AddOptions<TransactionOptions>()
                .Bind(configurationSection);

            // Add options validation
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TransactionOptions>, TransactionOptionsValidator>());

            // Register all transaction infrastructure services
            services.AddTransactionInfrastructure();

            // Register the new TransactionBehavior as a pipeline behavior
            services.RegisterCoreServices(svc =>
            {
                ServiceRegistrationHelper.TryAddEnumerable(svc, typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            });

            return services;
        }
        /// <summary>
        /// Adds a transaction event handler to the service collection.
        /// </summary>
        /// <typeparam name="THandler">The type of event handler to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="lifetime">The service lifetime for the event handler (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// Event handlers are registered as <see cref="ITransactionEventHandler"/> and will be invoked
        /// during transaction lifecycle events. Multiple handlers can be registered and they will all
        /// be invoked in parallel when events occur.
        /// 
        /// <para><strong>Example:</strong></para>
        /// <code>
        /// services.AddTransactionEventHandler&lt;AuditEventHandler&gt;();
        /// services.AddTransactionEventHandler&lt;CacheInvalidationHandler&gt;(ServiceLifetime.Singleton);
        /// </code>
        /// 
        /// <para><strong>Handler Execution:</strong></para>
        /// <list type="bullet">
        /// <item><description>Handlers are executed in parallel for better performance</description></item>
        /// <item><description>BeforeCommit handler failures cause transaction rollback</description></item>
        /// <item><description>AfterCommit/AfterRollback handler failures are logged but don't affect transaction</description></item>
        /// </list>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        public static IServiceCollection AddTransactionEventHandler<THandler>(
            this IServiceCollection services,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, ITransactionEventHandler
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the handler as ITransactionEventHandler
            // Using Add instead of TryAdd to allow multiple handlers
            var descriptor = new ServiceDescriptor(
                typeof(ITransactionEventHandler),
                typeof(THandler),
                lifetime);

            services.Add(descriptor);

            return services;
        }

        /// <summary>
        /// Adds a transaction event handler instance to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="handler">The event handler instance to register.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers a pre-configured event handler instance. The instance will be
        /// registered as a singleton and the same instance will be used for all transaction events.
        /// 
        /// <para><strong>Example:</strong></para>
        /// <code>
        /// var auditHandler = new AuditEventHandler(auditLog);
        /// services.AddTransactionEventHandler(auditHandler);
        /// </code>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services or handler is null.</exception>
        public static IServiceCollection AddTransactionEventHandler(
            this IServiceCollection services,
            ITransactionEventHandler handler)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            services.Add(new ServiceDescriptor(
                typeof(ITransactionEventHandler),
                handler));

            return services;
        }

        /// <summary>
        /// Adds a transaction event handler using a factory function.
        /// </summary>
        /// <typeparam name="THandler">The type of event handler to register.</typeparam>
        /// <param name="services">The service collection.</param>
        /// <param name="factory">The factory function to create the event handler.</param>
        /// <param name="lifetime">The service lifetime for the event handler (default: Transient).</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method allows registering an event handler with a custom factory function that
        /// can resolve dependencies from the service provider.
        /// 
        /// <para><strong>Example:</strong></para>
        /// <code>
        /// services.AddTransactionEventHandler&lt;AuditEventHandler&gt;(sp =>
        /// {
        ///     var auditLog = sp.GetRequiredService&lt;IAuditLog&gt;();
        ///     var logger = sp.GetRequiredService&lt;ILogger&lt;AuditEventHandler&gt;&gt;();
        ///     return new AuditEventHandler(auditLog, logger);
        /// });
        /// </code>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services or factory is null.</exception>
        public static IServiceCollection AddTransactionEventHandler<THandler>(
            this IServiceCollection services,
            Func<IServiceProvider, THandler> factory,
            ServiceLifetime lifetime = ServiceLifetime.Transient)
            where THandler : class, ITransactionEventHandler
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var descriptor = new ServiceDescriptor(
                typeof(ITransactionEventHandler),
                sp => factory(sp),
                lifetime);

            services.Add(descriptor);

            return services;
        }

        /// <summary>
        /// Adds the transaction event publisher to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers the <see cref="TransactionEventPublisher"/> as a singleton service.
        /// The publisher is responsible for invoking all registered event handlers during transaction
        /// lifecycle events.
        /// 
        /// This method is typically called internally by the transaction infrastructure and does not
        /// need to be called explicitly by application code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        internal static IServiceCollection AddTransactionEventPublisher(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the event publisher as a singleton
            services.TryAddSingleton<TransactionEventPublisher>();
            services.TryAddSingleton<ITransactionEventPublisher, TransactionEventPublisher>();

            return services;
        }

        /// <summary>
        /// Adds the transaction coordinator to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers the <see cref="TransactionCoordinator"/> as a transient service.
        /// The coordinator is responsible for managing transaction lifecycle operations including
        /// timeout enforcement and nested transaction management.
        /// 
        /// This method is typically called internally by the transaction infrastructure and does not
        /// need to be called explicitly by application code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        internal static IServiceCollection AddTransactionCoordinator(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the coordinator as transient since it's used per-request
            services.TryAddTransient<TransactionCoordinator>();
            services.TryAddTransient<ITransactionCoordinator, TransactionCoordinator>();

            return services;
        }

        /// <summary>
        /// Adds the distributed transaction coordinator to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers the <see cref="DistributedTransactionCoordinator"/> as a transient service.
        /// The coordinator is responsible for managing distributed transaction operations using TransactionScope
        /// for operations that span multiple databases or resources.
        /// 
        /// This method is typically called internally by the transaction infrastructure and does not
        /// need to be called explicitly by application code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        internal static IServiceCollection AddDistributedTransactionCoordinator(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the distributed coordinator as transient since it's used per-request
            services.TryAddTransient<DistributedTransactionCoordinator>();

            return services;
        }

        /// <summary>
        /// Adds core transaction infrastructure services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        /// <remarks>
        /// This method registers all core transaction services including:
        /// <list type="bullet">
        /// <item><description>Transaction coordinator</description></item>
        /// <item><description>Transaction event publisher</description></item>
        /// <item><description>Transaction context accessor</description></item>
        /// <item><description>Transaction configuration resolver</description></item>
        /// <item><description>Transaction retry handler</description></item>
        /// <item><description>Nested transaction manager</description></item>
        /// <item><description>Transaction metrics collector</description></item>
        /// <item><description>Transaction health check</description></item>
        /// </list>
        /// 
        /// This method is typically called internally by AddRelayTransactions and does not
        /// need to be called explicitly by application code.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when services is null.</exception>
        internal static IServiceCollection AddTransactionInfrastructure(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register core transaction services
            services.AddTransactionCoordinator();
            services.AddDistributedTransactionCoordinator();
            services.AddTransactionEventPublisher();
            
            // Note: TransactionContextAccessor is a static class and doesn't need DI registration
            
            // Register configuration resolver as transient
            services.TryAddTransient<TransactionConfigurationResolver>();
            services.TryAddTransient<ITransactionConfigurationResolver, TransactionConfigurationResolver>();
            
            // Register retry handler as transient
            services.TryAddTransient<TransactionRetryHandler>();
            services.TryAddTransient<ITransactionRetryHandler, TransactionRetryHandler>();
            
            // Register nested transaction manager as transient
            services.TryAddTransient<NestedTransactionManager>();
            services.TryAddTransient<INestedTransactionManager, NestedTransactionManager>();
            
            // Register telemetry and metrics services as singletons
            services.TryAddSingleton<TransactionMetricsCollector>();
            services.TryAddSingleton<ITransactionMetricsCollector, TransactionMetricsCollector>();
            services.TryAddSingleton<TransactionHealthCheck>();
            services.TryAddSingleton<TransactionActivitySource>();
            
            // Register transaction logger as transient (it wraps ILogger which is per-scope)
            services.TryAddTransient(sp =>
            {
                var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TransactionLogger>>();
                return new TransactionLogger(logger);
            });

            return services;
        }
    }
}
