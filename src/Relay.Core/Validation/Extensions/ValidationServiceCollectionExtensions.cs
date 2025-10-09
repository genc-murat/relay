using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Relay.Core.Validation.Pipeline;
using Relay.Core.Extensions;

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
            return services.RegisterCoreServices(svc =>
            {
                // Register the validation pipeline behaviors
                ServiceRegistrationHelper.TryAddTransient(svc, typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
                ServiceRegistrationHelper.TryAddTransient(svc, typeof(IStreamPipelineBehavior<,>), typeof(StreamValidationPipelineBehavior<,>));
            });
        }

        /// <summary>
        /// Automatically registers validation rules from the specified assembly.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <param name="assembly">The assembly to scan for validation rules.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationRulesFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            ServiceRegistrationHelper.ValidateServices(services);
            ArgumentNullException.ThrowIfNull(assembly);

            return services.RegisterFromAssembly(assembly, 
                interfaceType => interfaceType.IsGenericType && 
                                interfaceType.GetGenericTypeDefinition() == typeof(IValidationRule<>),
                ServiceLifetime.Transient)
                .RegisterCoreServices(svc =>
                {
                    // Register validators for each unique request type
                    var validationRuleTypes = assembly.GetTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces()
                            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>)));

                    var requestTypes = validationRuleTypes
                        .SelectMany(t => t.GetInterfaces())
                        .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidationRule<>))
                        .Select(i => i.GetGenericArguments()[0])
                        .Distinct();

                    foreach (var requestType in requestTypes)
                    {
                        var validatorType = typeof(DefaultValidator<>).MakeGenericType(requestType);
                        var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
                        ServiceRegistrationHelper.TryAddTransient(svc, validatorInterface, validatorType);
                    }
                });
        }

        /// <summary>
        /// Automatically registers validation rules from the calling assembly.
        /// </summary>
        /// <param name="services">The service collection to add services to.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddValidationRulesFromCallingAssembly(this IServiceCollection services)
        {
            return services.AddValidationRulesFromAssembly(Assembly.GetCallingAssembly());
        }
    }
}
