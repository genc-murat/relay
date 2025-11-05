using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a string is a valid Cardano address.
/// Supports Bech32 addresses for Shelley and Byron eras with network-specific validation.
/// </summary>
public class CardanoAddressValidationRule : IValidationRule<string>, IBlockchainAddressValidator
{
    private readonly string _errorMessage;
    private readonly bool _allowTestnet;

    /// <summary>
    /// Initializes a new instance of the <see cref="CardanoAddressValidationRule"/> class.
    /// </summary>
    /// <param name="allowTestnet">Whether to allow testnet addresses.</param>
    /// <param name="errorMessage">The error message to return when validation fails.</param>
    public CardanoAddressValidationRule(bool allowTestnet = true, string? errorMessage = null)
    {
        _allowTestnet = allowTestnet;
        _errorMessage = errorMessage ?? "Invalid Cardano address.";
    }

    /// <inheritdoc />
    public string BlockchainName => "Cardano";

    /// <inheritdoc />
    public string[] SupportedFormats => new[] { "Bech32", "Byron" };

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateAsync(string request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        var address = request.Trim();

        if (IsValidCardanoAddress(address))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsValidAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return IsValidCardanoAddress(address?.Trim());
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
            if (IsShelleyAddress(trimmedAddress))
            {
                var result = ValidateShelleyAddress(trimmedAddress);
                return result;
            }
            else if (IsByronAddress(trimmedAddress))
            {
                var result = ValidateByronAddress(trimmedAddress);
                return result;
            }
            else
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    Errors = new[] { "Address does not match Cardano address format." }
                };
            }
        }
        catch (Exception ex)
        {
            return new AddressValidationResult
            {
                IsValid = false,
                Errors = new[] { $"Validation error: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Validates a Cardano address (Shelley or Byron era).
    /// </summary>
    private static bool IsValidCardanoAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        return IsShelleyAddress(address) || IsByronAddress(address);
    }

    /// <summary>
    /// Checks if address is a Shelley-era Bech32 address.
    /// </summary>
    private static bool IsShelleyAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // Shelley addresses start with "addr1" for mainnet or "addr_test1" for testnet
        return address.StartsWith("addr1", StringComparison.OrdinalIgnoreCase) ||
               address.StartsWith("addr_test1", StringComparison.OrdinalIgnoreCase) ||
               address.StartsWith("stake1", StringComparison.OrdinalIgnoreCase) ||
               address.StartsWith("stake_test1", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if address is a Byron-era address.
    /// </summary>
    private static bool IsByronAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
            return false;

        // Byron addresses are base58-encoded and start with specific prefixes
        try
        {
            // Basic length check for Byron addresses
            return address.Length >= 26 && address.Length <= 98 &&
                   IsValidBase58ForByron(address);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a Shelley-era Bech32 address.
    /// </summary>
    private AddressValidationResult ValidateShelleyAddress(string address)
    {
        try
        {
            var isTestnet = address.StartsWith("addr_test1", StringComparison.OrdinalIgnoreCase) ||
                           address.StartsWith("stake_test1", StringComparison.OrdinalIgnoreCase);

            if (!_allowTestnet && isTestnet)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "Bech32",
                    Network = isTestnet ? "Testnet" : "Mainnet",
                    Errors = new[] { "Testnet addresses are not allowed." }
                };
            }

            // Decode Bech32 to validate structure
            var (hrp, data) = DecodeBech32(address);
            
            if (data.Length < 1)
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "Bech32",
                    Errors = new[] { "Invalid Bech32 encoding." }
                };
            }

            // Shelley addresses should have specific data structure
            var addressType = data[0] >> 4;
            var networkId = data[0] & 0x0F;

            return new AddressValidationResult
            {
                IsValid = true,
                FormatType = "Bech32",
                Network = isTestnet ? "Testnet" : "Mainnet",
                Metadata = new AddressMetadata
                {
                    ByteLength = data.Length,
                    AddressType = addressType,
                    IsTestnet = isTestnet,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["HumanReadablePart"] = hrp,
                        ["NetworkId"] = networkId
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new AddressValidationResult
            {
                IsValid = false,
                FormatType = "Bech32",
                Errors = new[] { $"Bech32 validation failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Validates a Byron-era address.
    /// </summary>
    private AddressValidationResult ValidateByronAddress(string address)
    {
        try
        {
            // Basic Byron address validation
            if (!IsValidBase58ForByron(address))
            {
                return new AddressValidationResult
                {
                    IsValid = false,
                    FormatType = "Byron",
                    Errors = new[] { "Invalid Byron Base58 encoding." }
                };
            }

            // Decode to get basic structure info
            var decoded = DecodeBase58ForByron(address);
            
            return new AddressValidationResult
            {
                IsValid = true,
                FormatType = "Byron",
                Network = "Mainnet", // Byron addresses are typically mainnet
                Metadata = new AddressMetadata
                {
                    ByteLength = decoded.Length,
                    IsTestnet = false,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["Era"] = "Byron"
                    }
                }
            };
        }
        catch (Exception ex)
        {
            return new AddressValidationResult
            {
                IsValid = false,
                FormatType = "Byron",
                Errors = new[] { $"Byron address validation failed: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Decodes a Bech32 string.
    /// </summary>
    private static (string hrp, byte[] data) DecodeBech32(string bech32)
    {
        const string charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        
        bech32 = bech32.ToLowerInvariant();
        
        // Find separator
        var separatorIndex = bech32.LastIndexOf('1');
        if (separatorIndex == -1)
            throw new ArgumentException("Invalid Bech32: no separator");

        var hrp = bech32.Substring(0, separatorIndex);
        var dataPart = bech32.Substring(separatorIndex + 1);

        // Convert to bytes
        var data = new List<byte>();
        for (var i = 0; i < dataPart.Length - 6; i += 8)
        {
            var value = 0;
            for (var j = 0; j < 8 && i + j < dataPart.Length - 6; j++)
            {
                var charIndex = charset.IndexOf(dataPart[i + j]);
                if (charIndex == -1)
                    throw new ArgumentException($"Invalid character: {dataPart[i + j]}");
                
                value = (value << 5) | charIndex;
            }
            
            if (i + 8 <= dataPart.Length - 6)
            {
                data.Add((byte)((value >> (8 - ((i + 8) % 5))) & 0xFF));
            }
        }

        return (hrp, data.ToArray());
    }

    /// <summary>
    /// Validates Base58 characters for Byron addresses.
    /// </summary>
    private static bool IsValidBase58ForByron(string address)
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return address.All(c => base58Chars.Contains(c));
    }

    /// <summary>
    /// Decodes Base58 string for Byron addresses.
    /// </summary>
    private static byte[] DecodeBase58ForByron(string input)
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        var num = BigInteger.Zero;
        foreach (var c in input)
        {
            var digit = base58Chars.IndexOf(c);
            if (digit == -1)
                throw new ArgumentException($"Invalid Base58 character: {c}");
            
            num = num * 58 + digit;
        }

        var bytes = num.ToByteArray();
        var trimmedBytes = bytes.SkipWhile(b => b == 0).ToArray();
        
        // Add leading zero bytes for each leading '1'
        var leadingZeros = 0;
        foreach (var c in input)
        {
            if (c == '1')
                leadingZeros++;
            else
                break;
        }

        var result = new byte[leadingZeros + trimmedBytes.Length];
        Array.Copy(trimmedBytes, 0, result, leadingZeros, trimmedBytes.Length);
        
        return result;
    }
}