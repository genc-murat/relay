using System;
using System.Linq;

namespace Relay.Core.Validation.Helpers
{
    /// <summary>
    /// Helper methods for general data validation.
    /// </summary>
    public static class GeneralValidationHelpers
    {
        /// <summary>
        /// Validates if a string is a valid numeric value (integer, decimal, or scientific notation).
        /// </summary>
        public static bool IsValidNumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return double.TryParse(value, out _);
        }

        /// <summary>
        /// Validates if a string contains only letters.
        /// </summary>
        public static bool IsValidAlpha(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.All(char.IsLetter);
        }

        /// <summary>
        /// Validates if a string contains only letters and digits.
        /// </summary>
        public static bool IsValidAlphanumeric(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.All(c => char.IsLetter(c) || char.IsDigit(c));
        }

        /// <summary>
        /// Validates if a string contains only digits.
        /// </summary>
        public static bool IsValidDigitsOnly(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.All(char.IsDigit);
        }

        /// <summary>
        /// Validates if a string has no whitespace characters.
        /// </summary>
        public static bool HasNoWhitespace(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !value.Any(char.IsWhiteSpace);
        }

        /// <summary>
        /// Validates if a comparable value is greater than the specified minimum.
        /// </summary>
        public static bool IsGreaterThan<T>(T value, T min) where T : IComparable<T>
        {
            return value.CompareTo(min) > 0;
        }

        /// <summary>
        /// Validates if a comparable value is less than the specified maximum.
        /// </summary>
        public static bool IsLessThan<T>(T value, T max) where T : IComparable<T>
        {
            return value.CompareTo(max) < 0;
        }
    }
}