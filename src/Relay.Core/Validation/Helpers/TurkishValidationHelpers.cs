using System.Linq;

namespace Relay.Core.Validation.Helpers
{
    /// <summary>
    /// Helper methods for Turkish data validation.
    /// </summary>
    public static class TurkishValidationHelpers
    {
        /// <summary>
        /// Validates if a string is a valid Turkish ID number (TC Kimlik No).
        /// </summary>
        public static bool IsValidTurkishId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 11 || !id.All(char.IsDigit))
            {
                return false;
            }

            var digits = id.Select(c => c - '0').ToArray();

            // First digit cannot be 0
            if (digits[0] == 0)
            {
                return false;
            }

            // Calculate checksums
            int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
            int evenSum = digits[1] + digits[3] + digits[5] + digits[7];

            int digit10 = ((oddSum * 7) - evenSum) % 10;
            int digit11 = (oddSum + evenSum + digit10) % 10;

            return digits[9] == digit10 && digits[10] == digit11;
        }

        /// <summary>
        /// Validates if a string is a valid Turkish foreigner ID number.
        /// </summary>
        public static bool IsValidTurkishForeignerId(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length != 11 || !id.All(char.IsDigit))
            {
                return false;
            }

            var digits = id.Select(c => c - '0').ToArray();

            // Must start with 99
            if (digits[0] != 9 || digits[1] != 9)
            {
                return false;
            }

            // Calculate checksums (same as citizen ID)
            int oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
            int evenSum = digits[1] + digits[3] + digits[5] + digits[7];

            int digit10 = ((oddSum * 7) - evenSum) % 10;
            if (digit10 < 0) digit10 += 10; // Handle negative modulo
            int digit11 = (oddSum + evenSum + digit10) % 10;

            return digits[9] == digit10 && digits[10] == digit11;
        }

        /// <summary>
        /// Validates if a string is a valid Turkish phone number.
        /// </summary>
        public static bool IsValidTurkishPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            // Allow only digits, +, and spaces
            if (!phone.All(c => char.IsDigit(c) || c == '+' || c == ' '))
            {
                return false;
            }

            // Remove all non-digit characters
            var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

            // Check for +90 prefix
            if (phone.StartsWith("+90"))
            {
                digitsOnly = digitsOnly.Substring(2);
            }
            else if (phone.StartsWith("90") && digitsOnly.Length == 12)
            {
                digitsOnly = digitsOnly.Substring(2);
            }

            // Should be 10 digits
            if (digitsOnly.Length != 10)
            {
                return false;
            }

            // Mobile numbers start with 5
            return digitsOnly.StartsWith("5");
        }

        /// <summary>
        /// Validates if a string is a valid Turkish postal code.
        /// </summary>
        public static bool IsValidTurkishPostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode))
            {
                return false;
            }

            // Remove spaces and check if it's exactly 5 digits
            var cleanCode = postalCode.Replace(" ", "");
            return cleanCode.Length == 5 && cleanCode.All(char.IsDigit);
        }

        /// <summary>
        /// Validates if a string is a valid Turkish IBAN.
        /// </summary>
        public static bool IsValidTurkishIban(string iban)
        {
            if (string.IsNullOrWhiteSpace(iban))
            {
                return false;
            }

            // Remove spaces
            var cleanIban = iban.Replace(" ", "").ToUpper();

            // Turkish IBAN should start with TR and be 26 characters long
            if (!cleanIban.StartsWith("TR") || cleanIban.Length != 26)
            {
                return false;
            }

            // Check if all characters after TR are digits
            return cleanIban.Substring(2).All(char.IsDigit);
        }

        /// <summary>
        /// Validates if a string is a valid Turkish tax number (Vergi No).
        /// </summary>
        public static bool IsValidTurkishTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                return false;
            }

            // Remove spaces
            var cleanNumber = taxNumber.Replace(" ", "");

            // Should be exactly 10 digits
            if (cleanNumber.Length != 10 || !cleanNumber.All(char.IsDigit))
            {
                return false;
            }

            // Basic checksum: sum of first 9 digits % 10 should equal 10th digit
            var digits = cleanNumber.Select(c => c - '0').ToArray();
            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += digits[i];
            }

            return sum % 10 == digits[9];
        }
    }
}