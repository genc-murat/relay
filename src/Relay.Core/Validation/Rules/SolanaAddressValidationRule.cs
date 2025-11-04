using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Validation rule that checks if a string is a valid Solana address.
/// Supports Base58-encoded 32-byte public keys with proper validation.
/// </summary>
public class SolanaAddressValidationRule : IValidationRule<string>
{
    private readonly string _errorMessage;

    /// <summary>
    /// Initializes a new instance of the <see cref="SolanaAddressValidationRule"/> class.
    /// </summary>
    /// <param name="errorMessage">The error message to return when validation fails.</param>
    public SolanaAddressValidationRule(string? errorMessage = null)
    {
        _errorMessage = errorMessage ?? "Invalid Solana address.";
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

        if (IsValidSolanaAddress(address))
        {
            return new ValueTask<IEnumerable<string>>(Array.Empty<string>());
        }

        return new ValueTask<IEnumerable<string>>(new[] { _errorMessage });
    }

    /// <summary>
    /// Validates a Solana address (Base58-encoded 32-byte public key).
    /// </summary>
    private static bool IsValidSolanaAddress(string address)
    {
        try
        {
            // Check basic Base58 character set
            if (!IsValidBase58(address))
                return false;

            // Decode Base58 to verify it results in exactly 32 bytes
            var decodedBytes = DecodeBase58(address);
            return decodedBytes.Length == 32;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates Base58 string using Solana's character set.
    /// </summary>
    private static bool IsValidBase58(string address)
    {
        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return !string.IsNullOrEmpty(address) && address.All(c => base58Chars.Contains(c));
    }

    /// <summary>
    /// Decodes a Base58 string to byte array.
    /// </summary>
    private static byte[] DecodeBase58(string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty", nameof(input));

        const string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        // Convert Base58 string to BigInteger
        var num = BigInteger.Zero;
        foreach (var c in input)
        {
            var digit = base58Chars.IndexOf(c);
            if (digit == -1)
                throw new ArgumentException($"Invalid Base58 character: {c}");
            
            num = num * 58 + digit;
        }

        // Convert BigInteger to byte array
        var bytes = num.ToByteArray();
        
        // Remove leading zero bytes from BigInteger's representation
        // Also remove potential extra sign bit byte at the end if present
        var trimmedBytes = bytes.SkipWhile(b => b == 0).ToArray();
        if (trimmedBytes.Length > 0 && trimmedBytes[trimmedBytes.Length - 1] == 0)
        {
            trimmedBytes = trimmedBytes.Take(trimmedBytes.Length - 1).ToArray();
        }
        
        // Add leading zero bytes for each leading '1' (zero) in the input
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