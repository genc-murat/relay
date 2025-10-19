using System;
using System.Collections.Generic;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Test data constants for validation benchmarks
/// </summary>
internal static class TestDataConstants
{
    // Valid test data
    internal const string ValidEmail = "user@example.com";
    internal const string ValidUrl = "https://www.example.com/path?query=value";
    internal const string ValidPhone = "+1-555-123-4567";
    internal const string ValidCreditCard = "4111111111111111";
    internal const string ValidIsbn = "978-0-123456-78-9";
    internal const string ValidVin = "1HGCM82633A123456";
    internal const string ValidJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";
    internal const string ValidJson = "{\"name\": \"John\", \"age\": 30}";
    internal const string ValidXml = "<user><name>John</name><age>30</age></user>";
    internal const string ValidBase64 = "SGVsbG8gV29ybGQ=";
    internal const string ValidHexColor = "#FF0000";
    internal const string ValidMac = "00:11:22:33:44:55";
    internal const string ValidTimeZone = "America/New_York";
    internal const string ValidCurrency = "USD";
    internal const string ValidLanguage = "en";
    internal const string ValidCountry = "US";

    // Additional valid test data for new validation rules
    internal const int ValidAge = 25;
    internal const double ValidPercentage = 85.5;
    internal const long ValidFileSize = 1024000; // 1MB
    internal const string ValidFileExtension = ".jpg";
    internal const string ValidIban = "GB29 NWBK 6016 1331 9268 19";
    internal const string ValidCurrencyAmount = "$1,234.56";
    internal const string ValidCronExpression = "0 0 * * *";
    internal const string ValidSemVer = "1.2.3-alpha.1";
    internal const string ValidTime = "14:30:00";
    internal const string ValidDuration = "PT1H30M";
    internal const string ValidMimeType = "application/json";
    internal const string ValidColor = "#FF5733";
    internal const string ValidDomain = "example.com";
    internal const string ValidUsername = "john_doe123";
    internal const string ValidCoordinate = "40.7128,-74.0060";
    internal const string ValidIpAddress = "192.168.1.1";
    internal const string ValidIpv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
    internal const string ValidPasswordStrength = "SecurePass123!";
    internal const string ValidDate = "2023-12-25";
    internal const string ValidGuid = "12345678-1234-1234-1234-123456789012";
    internal const string ValidRequired = "not empty";
    internal const string ValidNotEmpty = "not empty";
    internal const int ValidRange = 50;
    internal const string ValidMinLength = "hello world";
    internal const string ValidMaxLength = "short";
    internal const string ValidExactLength = "hello";
    internal const string ValidRegex = "test123";
    internal const int ValidPositive = 42;
    internal const int ValidNegative = -5;
    internal const int ValidNonZero = 7;
    internal const int ValidEven = 8;
    internal const int ValidOdd = 9;
    internal const string ValidIsEmpty = "";
    internal const string ValidHasDigits = "abc123";
    internal const string ValidHasLetters = "123abc";
    internal const string ValidIsLowerCase = "hello";
    internal const string ValidIsUpperCase = "HELLO";
    internal const string ValidStartsWith = "prefix_data";
    internal const string ValidEndsWith = "file.txt";
    internal const string ValidContains = "hello world";
    internal const string ValidEqual = "constant";
    internal const string ValidNotEqual = "allowed";
    internal const string ValidIsIn = "option1";
    internal const string ValidNotIn = "allowed";
    internal readonly static List<string> ValidUniqueList = new() { "a", "b", "c" };
    internal readonly static List<string> ValidMinCountList = new() { "a", "b", "c" };
    internal readonly static List<string> ValidMaxCountList = new() { "a", "b" };
    internal readonly static List<string> ValidEmptyList = new();
    internal readonly static DateTime ValidFutureDate = DateTime.UtcNow.AddDays(1);
    internal readonly static DateTime ValidPastDate = DateTime.UtcNow.AddDays(-1);
    internal readonly static DateTime ValidTodayDate = DateTime.Today;
    internal const int ValidBetween = 75;
    internal const string ValidLength = "hello";
    internal const int ValidZero = 0;
    internal const TestEnum ValidEnum = TestEnum.Option1;
    internal const string ValidPostalCode = "12345";

    // Invalid test data
    internal const string InvalidEmail = "invalid-email";
    internal const string InvalidUrl = "not-a-url";
    internal const string InvalidPhone = "invalid-phone";
    internal const string InvalidCreditCard = "1234567890123456";
    internal const string InvalidIsbn = "123-4-56789-01-2";
    internal const string InvalidVin = "INVALIDVIN123";
    internal const string InvalidJwt = "invalid.jwt.token";
    internal const string InvalidJson = "{\"name\": \"John\", \"age\": }";
    internal const string InvalidXml = "<user><name>John<name></user>";
    internal const string InvalidBase64 = "Invalid@Base64!";
    internal const string InvalidHexColor = "#GGG";
    internal const string InvalidMac = "invalid-mac";
    internal const string InvalidTimeZone = "Invalid/Timezone";
    internal const string InvalidCurrency = "INVALID";
    internal const string InvalidLanguage = "invalid-lang";
    internal const string InvalidCountry = "INVALID";

    // Additional invalid test data for new validation rules
    internal const int InvalidAge = -5;
    internal const double InvalidPercentage = -10.0;
    internal const long InvalidFileSize = -1000;
    internal const string InvalidFileExtension = ".exe";
    internal const string InvalidIban = "INVALID_IBAN";
    internal const string InvalidCurrencyAmount = "invalid-amount";
    internal const string InvalidCronExpression = "invalid cron";
    internal const string InvalidSemVer = "invalid.version";
    internal const string InvalidTime = "25:00:00";
    internal const string InvalidDuration = "invalid-duration";
    internal const string InvalidMimeType = "invalid/mime";
    internal const string InvalidColor = "invalid-color";
    internal const string InvalidDomain = "invalid..domain";
    internal const string InvalidUsername = "user@domain";
    internal const string InvalidCoordinate = "91.0000,0.0000";
    internal const string InvalidIpAddress = "256.1.1.1";
    internal const string InvalidIpv6 = "invalid-ipv6";
    internal const string InvalidPasswordStrength = "weak";
    internal const string InvalidDate = "invalid-date";
    internal const string InvalidGuid = "invalid-guid";
    internal const string InvalidNotEmpty = "";
    internal const int InvalidRange = 150;
    internal const string InvalidMinLength = "hi";
    internal const string InvalidMaxLength = "this is a very long string that exceeds maximum length";
    internal const string InvalidExactLength = "hi";
    internal const string InvalidRegex = "invalid";
    internal const int InvalidPositive = -10;
    internal const int InvalidNegative = 10;
    internal const int InvalidNonZero = 0;
    internal const int InvalidEven = 7;
    internal const int InvalidOdd = 10;
    internal const string InvalidIsEmpty = "not empty";
    internal const string InvalidHasDigits = "abcdef";
    internal const string InvalidHasLetters = "123456";
    internal const string InvalidIsLowerCase = "Hello";
    internal const string InvalidIsUpperCase = "Hello";
    internal const string InvalidStartsWith = "data_suffix";
    internal const string InvalidEndsWith = "file.pdf";
    internal const string InvalidContains = "goodbye";
    internal const string InvalidEqual = "different";
    internal const string InvalidIsIn = "invalid";
    internal const string InvalidNotIn = "banned";
    internal const string InvalidNotEqual = "banned";
    internal readonly static List<string> InvalidUniqueList = new() { "a", "b", "a" };
    internal readonly static List<string> InvalidMinCountList = new();
    internal readonly static List<string> InvalidMaxCountList = new() { "a", "b", "c", "d", "e", "f" };
    internal readonly static List<string> InvalidEmptyList = new() { "not", "empty" };
    internal readonly static DateTime InvalidFutureDate = DateTime.UtcNow.AddDays(-1);
    internal readonly static DateTime InvalidPastDate = DateTime.UtcNow.AddDays(1);
    internal readonly static DateTime InvalidTodayDate = DateTime.Today.AddDays(1);
    internal const int InvalidBetween = 150;
    internal const string InvalidLength = "this is too long";
    internal const int InvalidZero = 5;
    internal const string InvalidPostalCode = "invalid";

    internal enum TestEnum { Option1, Option2, Option3 }
}