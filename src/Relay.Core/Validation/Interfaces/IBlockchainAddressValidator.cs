using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Interfaces;

/// <summary>
/// Common interface for blockchain address validation rules.
/// Provides standardized validation for different blockchain address formats.
/// </summary>
public interface IBlockchainAddressValidator
{
    /// <summary>
    /// Gets the name of the blockchain network this validator supports.
    /// </summary>
    string BlockchainName { get; }

    /// <summary>
    /// Gets the supported address formats for this blockchain.
    /// </summary>
    string[] SupportedFormats { get; }

    /// <summary>
    /// Validates whether the given address is a valid blockchain address.
    /// </summary>
    /// <param name="address">The address to validate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the validation operation.</returns>
    ValueTask<bool> IsValidAddressAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed validation information for the address.
    /// </summary>
    /// <param name="address">The address to analyze.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the validation analysis operation.</returns>
    ValueTask<AddressValidationResult> GetValidationResultAsync(string address, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of address validation.
/// </summary>
public class AddressValidationResult
{
    /// <summary>
    /// Gets whether the address is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the address format type.
    /// </summary>
    public string? FormatType { get; init; }

    /// <summary>
    /// Gets the network or chain identifier.
    /// </summary>
    public string? Network { get; init; }

    /// <summary>
    /// Gets validation error messages, if any.
    /// </summary>
    public string[] Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Gets additional metadata about the address.
    /// </summary>
    public AddressMetadata? Metadata { get; init; }
}

/// <summary>
/// Additional metadata about a validated address.
/// </summary>
public class AddressMetadata
{
    /// <summary>
    /// Gets the decoded address length in bytes.
    /// </summary>
    public int? ByteLength { get; init; }

    /// <summary>
    /// Gets the address type identifier.
    /// </summary>
    public int? AddressType { get; init; }

    /// <summary>
    /// Gets whether this is a testnet address.
    /// </summary>
    public bool? IsTestnet { get; init; }

    /// <summary>
    /// Gets additional properties specific to the blockchain.
    /// </summary>
    public Dictionary<string, object> AdditionalProperties { get; init; } = new();
}