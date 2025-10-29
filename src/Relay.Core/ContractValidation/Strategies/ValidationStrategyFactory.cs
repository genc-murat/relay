using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.Core.ContractValidation.Strategies;

/// <summary>
/// Factory for creating validation strategy instances based on configuration.
/// </summary>
public sealed class ValidationStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationStrategyFactory"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    public ValidationStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Creates a validation strategy based on the specified strategy name.
    /// </summary>
    /// <param name="strategyName">The name of the strategy to create (e.g., "Strict", "Lenient").</param>
    /// <returns>An instance of the requested validation strategy.</returns>
    /// <exception cref="ArgumentException">Thrown when the strategy name is invalid or unknown.</exception>
    public IValidationStrategy CreateStrategy(string strategyName)
    {
        if (string.IsNullOrWhiteSpace(strategyName))
        {
            throw new ArgumentException("Strategy name cannot be null or empty.", nameof(strategyName));
        }

        return strategyName.ToLowerInvariant() switch
        {
            "strict" => new StrictValidationStrategy(),
            "lenient" => CreateLenientStrategy(),
            _ => throw new ArgumentException(
                $"Unknown validation strategy: '{strategyName}'. Supported strategies are: 'Strict', 'Lenient'.",
                nameof(strategyName))
        };
    }

    /// <summary>
    /// Creates a strict validation strategy instance.
    /// </summary>
    /// <returns>A new instance of <see cref="StrictValidationStrategy"/>.</returns>
    public IValidationStrategy CreateStrictStrategy()
    {
        return new StrictValidationStrategy();
    }

    /// <summary>
    /// Creates a lenient validation strategy instance.
    /// </summary>
    /// <returns>A new instance of <see cref="LenientValidationStrategy"/>.</returns>
    public IValidationStrategy CreateLenientStrategy()
    {
        var logger = _serviceProvider.GetRequiredService<ILogger<LenientValidationStrategy>>();
        return new LenientValidationStrategy(logger);
    }

    /// <summary>
    /// Gets the default validation strategy (Strict).
    /// </summary>
    /// <returns>The default validation strategy instance.</returns>
    public IValidationStrategy GetDefaultStrategy()
    {
        return CreateStrictStrategy();
    }
}
