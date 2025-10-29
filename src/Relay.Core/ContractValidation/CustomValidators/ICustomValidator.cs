using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;

namespace Relay.Core.ContractValidation.CustomValidators;

/// <summary>
/// Defines a contract for custom validators that can be integrated with the contract validation pipeline.
/// </summary>
public interface ICustomValidator
{
    /// <summary>
    /// Gets the priority of this validator. Higher values execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Determines if this validator applies to the given type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if this validator applies to the type; otherwise, false.</returns>
    bool AppliesTo(Type type);

    /// <summary>
    /// Validates the object and returns validation errors.
    /// </summary>
    /// <param name="obj">The object to validate.</param>
    /// <param name="context">The validation context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of validation errors, or an empty collection if validation succeeds.</returns>
    ValueTask<IEnumerable<ValidationError>> ValidateAsync(
        object obj,
        ValidationContext context,
        CancellationToken cancellationToken = default);
}
