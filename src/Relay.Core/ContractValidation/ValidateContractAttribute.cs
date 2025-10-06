using System;

namespace Relay.Core.ContractValidation;

/// <summary>
/// Attribute to mark handlers or requests that should have contract validation applied.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
public sealed class ValidateContractAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to validate the request contract.
    /// </summary>
    public bool ValidateRequest { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to validate the response contract.
    /// </summary>
    public bool ValidateResponse { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to throw an exception when validation fails.
    /// </summary>
    public bool ThrowOnValidationFailure { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateContractAttribute"/> class.
    /// </summary>
    public ValidateContractAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateContractAttribute"/> class.
    /// </summary>
    /// <param name="validateRequest">Whether to validate the request contract.</param>
    /// <param name="validateResponse">Whether to validate the response contract.</param>
    public ValidateContractAttribute(bool validateRequest, bool validateResponse)
    {
        ValidateRequest = validateRequest;
        ValidateResponse = validateResponse;
    }
}