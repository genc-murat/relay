using System;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.Core.ContractValidation.Configuration;

/// <summary>
/// Extension methods for registering contract validation startup validation.
/// </summary>
public static class ContractValidationStartupValidatorExtensions
{
    /// <summary>
    /// Validates contract validation configuration during application startup.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    public static IServiceProvider ValidateContractValidationConfiguration(this IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetService<ContractValidationStartupValidator>();
        if (validator != null)
        {
            validator.Validate();
        }

        return serviceProvider;
    }
}
