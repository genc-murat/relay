using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a string is a valid Polkadot/Substrate address.
/// Supports SS58-encoded addresses with network-specific validation.
/// </summary>
public class PolkadotAddressValidationRule : IValidationRule<string>, IBlockchainAddressValidator
{
    private readonly string _errorMessage;
    private readonly bool _allowTestnet;
    private readonly int[] _allowedPrefixes;

    // SS58 prefix registry for common networks
    private static readonly Dictionary<int, string> Ss58Prefixes = new()
    {
        { 0, "Polkadot" },
        { 2, "Kusama" },
        { 42, "Substrate" }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="PolkadotAddressValidationRule"/> class.
    /// </summary>
    /// <param name="allowTestnet">Whether to allow testnet addresses.</param>
    /// <param name="allowedPrefixes">Allowed SS58 prefixes (null for all).</param>
    /// <param name="errorMessage">The error message to return when validation fails.</param>
    public PolkadotAddressValidationRule(bool allowTestnet = true, int[]? allowedPrefixes = null, string? errorMessage = null)
    {
        _allowTestnet = allowTestnet;
        _allowedPrefixes = allowedPrefixes ?? Ss58Prefixes.Keys.ToArray();
        _errorMessage = errorMessage ?? "Invalid Polkadot address.";
    }

    /// <inheritdoc />
    public string BlockchainName => "Polkadot";

    /// <inheritdoc />
    public string[] SupportedFormats => new[] { "SS58" };

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        var address = request.Trim();

        if (IsValidPolkadotAddress(address))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsValidAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return IsValidPolkadotAddress(address?.Trim());
    }

    /// <inheritdoc />
    public async ValueTask<AddressValidationResult> GetValidationResultAsync(string address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(address))
        {
            return new AddressValidationResult
            {
                IsValid = false,
                Errors = new[] { "Address cannot be null or empty." }
            };
        }

        var trimmedAddress = address.Trim();

        try
        {
            var result = ValidateSs58Address(trimmedAddress);
            return result;
        }
        catch (Exception ex)
        {
            return new AddressValidationResult
            {
                IsValid = false,
                FormatType = "SS58",
                Errors = new[] { $"Validation error: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Validates a Polkadot/Substrate SS58 address.
    /// </summary>
    private static bool IsValidPolkadotAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        try
        {
            var result = DecodeSs58(address);
            return result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates an SS58 address with detailed result.
    /// </summary>
    private AddressValidationResult ValidateSs58Address(string address)
    {
        try
        {
            var decoded = DecodeSs58(address);
            
            if (!decoded.IsValid)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "SS58",
                    Errors = new[] { "Invalid SS58 format." }
                };
            }

            // Check if prefix is allowed
            if (!_allowedPrefixes.Contains(decoded.Prefix))
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "SS58",
                    Errors = new[] { $"SS58 prefix {decoded.Prefix} is not allowed." }
                };
            }

            // Check testnet restrictions
            var isTestnet = IsTestnetPrefix(decoded.Prefix);
            if (!_allowTestnet && isTestnet)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "SS58",
                    Network = GetNetworkName(decoded.Prefix),
                    Errors = new[] { "Testnet addresses are not allowed." }
                };
            }

            var networkName = GetNetworkName(decoded.Prefix);
            var addressType = DetermineAddressType(decoded.Bytes);

            return new AddressValidationResult
            {
                IsValid = true,
                FormatType = "SS58",
                Network = networkName,
                Metadata = new AddressMetadata
                {
                    ByteLength = decoded.Bytes.Length,
                    AddressType = addressType,
                    IsTestnet = isTestnet,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["Ss58Prefix"] = decoded.Prefix,
                        ["Checksum"] = decoded.Checksum
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new AddressValidationResult
            {
                IsValid = false,
                FormatType = "SS58",
                Errors = new[] { $"SS58 validation failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Decodes an SS58 address.
    /// </summary>
    private static Ss58Decoded DecodeSs58(string address)
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        if (string.IsNullOrEmpty(address) || address.Length < 3 || address.Length > 47)
            return new Ss58Decoded { IsValid = false };

        // Check if all characters are valid Base58
        if (!address.All(c => base58Chars.Contains(c)))
            return new Ss58Decoded { IsValid = false };

        // Decode Base58
        var num = BigInteger.Zero;
        foreach (var c in address)
        {
            var digit = base58Chars.IndexOf(c);
            if (digit == -1)
                return new Ss58Decoded { IsValid = false };
            
            num = num * 58 + digit;
        }

        var bytes = num.ToByteArray();
        var trimmedBytes = bytes.SkipWhile(b => b == 0).ToArray();
        
        // Add leading zero bytes for each leading '1'
        var leadingZeros = 0;
        foreach (var c in address)
        {
            if (c == '1')
                leadingZeros++;
            else
                break;
        }

        var result = new byte[leadingZeros + trimmedBytes.Length];
        Array.Copy(trimmedBytes, 0, result, leadingZeros, trimmedBytes.Length);

        if (result.Length < 2)
            return new Ss58Decoded { IsValid = false };

        // Extract prefix and checksum
        var prefix = result[0];
        var checksum = result[^1];
        var data = result[1..^1];

        // Verify checksum (simplified version)
        var expectedChecksum = CalculateChecksum(prefix, data);
        if (checksum != expectedChecksum)
            return new Ss58Decoded { IsValid = false };

        return new Ss58Decoded
        {
            IsValid = true,
            Prefix = prefix,
            Bytes = data,
            Checksum = checksum
        };
    }

    /// <summary>
    /// Calculates SS58 checksum (simplified implementation).
    /// </summary>
    private static byte CalculateChecksum(byte prefix, byte[] data)
    {
        // This is a simplified checksum calculation
        // In a production implementation, you would use the proper SS58 checksum algorithm
        var combined = new byte[data.Length + 1];
        combined[0] = prefix;
        Array.Copy(data, 0, combined, 1, data.Length);
        
        // Simple checksum for validation
        byte checksum = 0;
        foreach (var b in combined)
        {
            checksum ^= b;
        }
        
        return checksum;
    }

    /// <summary>
    /// Determines if a prefix is for testnet.
    /// </summary>
    private static bool IsTestnetPrefix(int prefix)
    {
        // Common testnet prefixes (simplified)
        return prefix switch
        {
            42 => true, // Substrate testnet
            _ => false
        };
    }

    /// <summary>
    /// Gets the network name for a given prefix.
    /// </summary>
    private static string GetNetworkName(int prefix)
    {
        return Ss58Prefixes.TryGetValue(prefix, out var name) ? name : $"Unknown({prefix})";
    }

    /// <summary>
    /// Determines the address type from the decoded bytes.
    /// </summary>
    private static int DetermineAddressType(byte[] bytes)
    {
        if (bytes.Length == 0)
            return 0;

        // Simplified address type determination
        return bytes[0] switch
        {
            0 => 0, // Account ID
            1 => 1, // Account index
            2 => 2, // Account public key
            _ => 99 // Unknown
        };
    }

    /// <summary>
    /// Represents a decoded SS58 address.
    /// </summary>
    private class Ss58Decoded
    {
        public bool IsValid { get; init; }
        public int Prefix { get; init; }
        public byte[] Bytes { get; init; } = Array.Empty<byte>();
        public byte Checksum { get; init; }
    }
}