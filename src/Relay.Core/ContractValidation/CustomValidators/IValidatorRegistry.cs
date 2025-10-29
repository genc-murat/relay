using System;
using System.Collections.Generic;

namespace Relay.Core.ContractValidation.CustomValidators;

/// <summary>
/// Defines a contract for registering and discovering custom validators.
/// </summary>
public interface IValidatorRegistry
{
    /// <summary>
    /// Registers a custom validator.
    /// </summary>
    /// <param name="validator">The validator to register.</param>
    void Register(ICustomValidator validator);

    /// <summary>
    /// Registers a custom validator of the specified type.
    /// </summary>
    /// <typeparam name="TValidator">The type of validator to register.</typeparam>
    void Register<TValidator>() where TValidator : ICustomValidator, new();

    /// <summary>
    /// Unregisters a custom validator.
    /// </summary>
    /// <param name="validator">The validator to unregister.</param>
    /// <returns>True if the validator was unregistered; otherwise, false.</returns>
    bool Unregister(ICustomValidator validator);

    /// <summary>
    /// Unregisters all validators of the specified type.
    /// </summary>
    /// <typeparam name="TValidator">The type of validators to unregister.</typeparam>
    /// <returns>The number of validators unregistered.</returns>
    int UnregisterAll<TValidator>() where TValidator : ICustomValidator;

    /// <summary>
    /// Gets all registered validators.
    /// </summary>
    /// <returns>A collection of all registered validators.</returns>
    IEnumerable<ICustomValidator> GetAll();

    /// <summary>
    /// Gets all validators that apply to the specified type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>A collection of validators that apply to the type.</returns>
    IEnumerable<ICustomValidator> GetValidatorsFor(Type type);

    /// <summary>
    /// Clears all registered validators.
    /// </summary>
    void Clear();
}
