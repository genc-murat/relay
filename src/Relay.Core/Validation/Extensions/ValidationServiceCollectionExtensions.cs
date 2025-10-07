using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Pipeline;

namespace Relay.Core.Validation.Extensions
{
    /// <summary>
    /// Extension methods for configuring validation services.
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds validation services to the service collection.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayValidation(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register the validation pipeline behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
            services.AddTransient(typeof(IStreamPipelineBehavior<,>), typeof(StreamValidationPipelineBehavior<,>));

            return services;
        }

        /// <summary>
        /// Automatically registers validation rules from the specified assembly.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assembly">The assembly to scan for validation rules.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationRulesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            // Find all types that implement IValidationRule<>
            var validationRuleTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>)));

            // Register each validation rule
            foreach (var ruleType in validationRuleTypes)
            {
                var interfaces = ruleType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>));

                foreach (var @interface in interfaces)
                {
                    services.AddTransient(@interface, ruleType);
                }
            }

            // Register validators for each unique request type
            var requestTypes = validationRuleTypes
                .SelectMany(t => t.GetInterfaces())
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>))
                .Select(i => i.GetGenericArguments()[0])
                .Distinct();

            foreach (var requestType in requestTypes)
            {
                var validatorType = typeof(DefaultValidator<>).MakeGenericType(requestType);
                var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
                services.AddTransient(validatorInterface, validatorType);
            }

            return services;
        }

        /// <summary>
        /// Automatically registers validation rules from the calling assembly.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationRulesFromCallingAssembly(this IServiceCollection services)
        {
            return AddValidationRulesFromAssembly(services, Assembly.GetCallingAssembly());
        }
    }
}
