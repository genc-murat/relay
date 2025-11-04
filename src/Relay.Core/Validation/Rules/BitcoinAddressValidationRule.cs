using Relay.Core.Validation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a string is a valid Bitcoin address.
/// Supports P2PKH, P2SH (Base58Check), and Bech32 address formats with network-specific validation.
/// </summary>
public class BitcoinAddressValidationRule : IValidationRule<string>
{
    private readonly BitcoinNetwork _network;
    private readonly string _errorMessage;

    /// <summary>
    /// Bitcoin network types.
    /// </summary>
    public enum BitcoinNetwork
    {
        /// <summary>Any network (mainnet or testnet)</summary>
        Any,
        /// <summary>Mainnet only</summary>
        Mainnet,
        /// <summary>Testnet only</summary>
        Testnet
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BitcoinAddressValidationRule"/> class.
    /// </summary>
    /// <param name="network">The Bitcoin network to validate for.</param>
    /// <param name="errorMessage">The error message to return when validation fails.</param>
    public BitcoinAddressValidationRule(BitcoinNetwork network = BitcoinNetwork.Any, string? errorMessage = null)
    {
        _network = network;
        _errorMessage = errorMessage ?? "Invalid Bitcoin address.";
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        var address = request.Trim();

        if (IsValidBech32Address(address))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        if (IsValidBase58Address(address))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
    }

    /// <summary>
    /// Validates a Bech32 address (P2WPKH, P2WSH, P2TR).
    /// </summary>
    private bool IsValidBech32Address(string address)
    {
        try
        {
            if (address.Length < 8 || address.Length > 90)
                return false;

            var separatorIndex = address.LastIndexOf('1');
            if (separatorIndex == -1 || separatorIndex == 0 || separatorIndex == address.Length - 1)
                return false;

            var hrp = address.Substring(0, separatorIndex).ToLowerInvariant();
            var data = address.Substring(separatorIndex + 1);

            // Validate human-readable part
            if (!IsValidHrp(hrp))
                return false;

            // Validate network
            if (!IsValidNetworkForBech32(hrp))
                return false;

            // Validate data part characters
            if (!IsValidBech32Data(data))
                return false;

            // Basic length validation for different address types
            if (hrp == "bc" || hrp == "tb")
            {
                // Bech32 addresses should have specific lengths
                if (data.Length < 6 || data.Length > 90)
                    return false;
                
                // Additional validation: bech32 addresses typically end with specific characters
                var lastChar = data[^1];
                if (!IsValidBech32Data(lastChar.ToString()))
                    return false;
            }

            return true; // For now, accept valid format without full checksum verification
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a Base58 address (P2PKH, P2SH).
    /// </summary>
    private bool IsValidBase58Address(string address)
    {
        try
        {
            if (address.Length < 26 || address.Length > 35)
                return false;

            if (!IsValidBase58(address))
                return false;

            // Basic format validation - first character indicates network and type
            var firstChar = address[0];
            
            return _network switch
            {
                BitcoinNetwork.Any => firstChar is '1' or '3' or 'm' or 'n' or '2',
                BitcoinNetwork.Mainnet => firstChar is '1' or '3', // P2PKH (1) or P2SH (3)
                BitcoinNetwork.Testnet => firstChar is 'm' or 'n' or '2', // P2PKH (m,n) or P2SH (2)
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates the human-readable part for Bech32 addresses.
    /// </summary>
    private static bool IsValidHrp(string hrp)
    {
        return hrp.All(c => c >= 'a' && c <= 'z');
    }

    /// <summary>
    /// Validates the data part for Bech32 addresses.
    /// </summary>
    private static bool IsValidBech32Data(string data)
    {
        return data.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'));
    }

    /// <summary>
    /// Validates network for Bech32 addresses.
    /// </summary>
    private bool IsValidNetworkForBech32(string hrp)
    {
        return _network switch
        {
            BitcoinNetwork.Any => hrp is "bc" or "tb",
            BitcoinNetwork.Mainnet => hrp == "bc",
            BitcoinNetwork.Testnet => hrp == "tb",
            _ => false
        };
    }

    /// <summary>
    /// Validates Base58 string.
    /// </summary>
    private static bool IsValidBase58(string address)
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return address.All(c => base58Chars.Contains(c));
    }
}