using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Relay.Core.Validation.Rules;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Simple test logger for benchmarks
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

/// <summary>
/// Benchmarks for validation rule performance
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ValidationBenchmarks
{
    private readonly EmailValidationRule _emailRule = new();
    private readonly UrlValidationRule _urlRule = new();
    private readonly PhoneNumberValidationRule _phoneRule = new();
    private readonly CreditCardValidationRule _creditCardRule = new();
    private readonly IsbnValidationRule _isbnRule = new();
    private readonly VinValidationRule _vinRule = new();
    private readonly JwtValidationRule _jwtRule = new();
    private readonly JsonValidationRule _jsonRule = new();
    private readonly XmlValidationRule _xmlRule = new();
    private readonly Base64ValidationRule _base64Rule = new();
    private readonly HexColorValidationRule _hexColorRule = new();
    private readonly MacAddressValidationRule _macRule = new();
    private readonly TimeZoneValidationRule _timeZoneRule = new();
    private readonly CurrencyCodeValidationRule _currencyRule = new();
    private readonly LanguageCodeValidationRule _languageRule = new();
    private readonly CountryCodeValidationRule _countryRule = new();
    private readonly AgeValidationRule _ageRule = new();
    private readonly PercentageValidationRule _percentageRule = new();
    private readonly FileSizeValidationRule _fileSizeRule = new(10485760); // 10MB
    private readonly FileExtensionValidationRule _fileExtensionRule = new(new[] { ".txt", ".jpg", ".png" });
    private readonly IbanValidationRule _ibanRule = new();
    private readonly CurrencyAmountValidationRule _currencyAmountRule = new();
    private readonly CronExpressionValidationRule _cronRule = new();
    private readonly SemVerValidationRule _semVerRule = new();
    private readonly TimeValidationRule _timeRule = new();
    private readonly DurationValidationRule _durationRule = new();
    private readonly MimeTypeValidationRule _mimeTypeRule = new();
    private readonly ColorValidationRule _colorRule = new();
    private readonly DomainValidationRule _domainRule = new();
    private readonly UsernameValidationRule _usernameRule = new();
    private readonly CoordinateValidationRule _coordinateRule = new();
    private readonly IpAddressValidationRule _ipAddressRule = new();
    private readonly Ipv6ValidationRule _ipv6Rule = new();
    private readonly PasswordStrengthValidationRule _passwordStrengthRule = new();
    private readonly DateValidationRule _dateRule = new();
    private readonly GuidValidationRule _guidRule = new();
    private readonly RequiredValidationRule<string> _requiredRule = new();
    private readonly NotEmptyValidationRule _notEmptyRule = new();
    private readonly RangeValidationRule<int> _rangeRule = new(0, 100);
    private readonly MinLengthValidationRule _minLengthRule = new();
    private readonly MaxLengthValidationRule _maxLengthRule = new();
    private readonly ExactLengthValidationRule _exactLengthRule = new(5);
    private readonly RegexValidationRule _regexRule = new(@"^\w+$");
    private readonly PositiveValidationRule<int> _positiveRule = new();
    private readonly NegativeValidationRule<int> _negativeRule = new();
    private readonly NonZeroValidationRule<int> _nonZeroRule = new();
    private readonly EvenValidationRule _evenRule = new();
    private readonly OddValidationRule _oddRule = new();
    private readonly IsEmptyValidationRule _isEmptyRule = new();
    private readonly HasDigitsValidationRule _hasDigitsRule = new();
    private readonly HasLettersValidationRule _hasLettersRule = new();
    private readonly IsLowerCaseValidationRule _isLowerCaseRule = new();
    private readonly IsUpperCaseValidationRule _isUpperCaseRule = new();
    private readonly StartsWithValidationRule _startsWithRule = new("prefix");
    private readonly EndsWithValidationRule _endsWithRule = new(".txt");
    private readonly ContainsValidationRuleString _containsRule = new("test");
    private readonly IsEqualValidationRule<string> _equalRule = new("constant");
    private readonly NotEqualValidationRule<string> _notEqualRule = new("banned");
    private readonly IsInValidationRule<string> _isInRule = new(new[] { "option1", "option2" });
    private readonly NotInValidationRule<string> _notInRule = new(new[] { "banned1", "banned2" });
    private readonly UniqueValidationRule<string> _uniqueRule = new();
    private readonly MinCountValidationRule<string> _minCountRule = new(1);
    private readonly MaxCountValidationRule<string> _maxCountRule = new(5);
    private readonly EmptyValidationRule<string> _emptyRule = new();
    private readonly FutureValidationRule _futureRule = new();
    private readonly PastValidationRule _pastRule = new();
    private readonly TodayValidationRule _todayRule = new();
    private readonly BetweenValidationRule<int> _betweenRule = new(0, 100);
    private readonly LengthValidationRule _lengthRule = new();
    private readonly ZeroValidationRule<int> _zeroRule = new();
    private readonly EnumValidationRule<TestEnum> _enumRule = new();
    private readonly PostalCodeValidationRule _postalCodeRule = new();

    // Business validation
    private readonly DefaultBusinessRulesEngine _businessRulesEngine = new(new TestLogger<DefaultBusinessRulesEngine>());
    private readonly BusinessValidationRequest _validBusinessRequest = new()
    {
        Amount = 1500m,
        PaymentMethod = "credit_card",
        StartDate = DateTime.UtcNow.AddDays(1),
        EndDate = DateTime.UtcNow.AddDays(30),
        IsRecurring = false,
        UserType = UserType.Regular,
        CountryCode = "US",
        BusinessCategory = "retail",
        UserTransactionCount = 5
    };
    private readonly BusinessValidationRequest _invalidBusinessRequest = new()
    {
        Amount = 15000m, // Too high for regular user
        // Missing PaymentMethod
        StartDate = DateTime.UtcNow.AddDays(-10), // Too old
        EndDate = DateTime.UtcNow.AddDays(30),
        IsRecurring = false,
        UserType = UserType.Regular,
        CountryCode = "US",
        BusinessCategory = "retail",
        UserTransactionCount = 5
    };

    // Test data
    private const string ValidEmail = "user@example.com";
    private const string ValidUrl = "https://www.example.com/path?query=value";
    private const string ValidPhone = "+1-555-123-4567";
    private const string ValidCreditCard = "4111111111111111";
    private const string ValidIsbn = "978-0-123456-78-9";
    private const string ValidVin = "1HGCM82633A123456";
    private const string ValidJwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.TJVA95OrM7E2cBab30RMHrHDcEfxjoYZgeFONFh7HgQ";
    private const string ValidJson = "{\"name\": \"John\", \"age\": 30}";
    private const string ValidXml = "<user><name>John</name><age>30</age></user>";
    private const string ValidBase64 = "SGVsbG8gV29ybGQ=";
    private const string ValidHexColor = "#FF0000";
    private const string ValidMac = "00:11:22:33:44:55";
    private const string ValidTimeZone = "America/New_York";
    private const string ValidCurrency = "USD";
    private const string ValidLanguage = "en";
    private const string ValidCountry = "US";

    private const string InvalidEmail = "invalid-email";
    private const string InvalidUrl = "not-a-url";
    private const string InvalidPhone = "invalid-phone";
    private const string InvalidCreditCard = "1234567890123456";
    private const string InvalidIsbn = "123-4-56789-01-2";
    private const string InvalidVin = "INVALIDVIN123";
    private const string InvalidJwt = "invalid.jwt.token";
    private const string InvalidJson = "{\"name\": \"John\", \"age\": }";
    private const string InvalidXml = "<user><name>John<name></user>";
    private const string InvalidBase64 = "Invalid@Base64!";
    private const string InvalidHexColor = "#GGG";
    private const string InvalidMac = "invalid-mac";
    private const string InvalidTimeZone = "Invalid/Timezone";
    private const string InvalidCurrency = "INVALID";
    private const string InvalidLanguage = "invalid-lang";
    private const string InvalidCountry = "INVALID";

    // Additional test data for new validation rules
    private const int ValidAge = 25;
    private const int InvalidAge = -5;
    private const double ValidPercentage = 85.5;
    private const double InvalidPercentage = -10.0;
    private const long ValidFileSize = 1024000; // 1MB
    private const long InvalidFileSize = -1000;
    private const string ValidFileExtension = ".jpg";
    private const string InvalidFileExtension = ".exe";
    private const string ValidIban = "GB29 NWBK 6016 1331 9268 19";
    private const string InvalidIban = "INVALID_IBAN";
    private const string ValidCurrencyAmount = "$1,234.56";
    private const string InvalidCurrencyAmount = "invalid-amount";
    private const string ValidCronExpression = "0 0 * * *";
    private const string InvalidCronExpression = "invalid cron";
    private const string ValidSemVer = "1.2.3-alpha.1";
    private const string InvalidSemVer = "invalid.version";
    private const string ValidTime = "14:30:00";
    private const string InvalidTime = "25:00:00";
    private const string ValidDuration = "PT1H30M";
    private const string InvalidDuration = "invalid-duration";
    private const string ValidMimeType = "application/json";
    private const string InvalidMimeType = "invalid/mime";
    private const string ValidColor = "#FF5733";
    private const string InvalidColor = "invalid-color";
    private const string ValidDomain = "example.com";
    private const string InvalidDomain = "invalid..domain";
    private const string ValidUsername = "john_doe123";
    private const string InvalidUsername = "user@domain";
    private const string ValidCoordinate = "40.7128,-74.0060";
    private const string InvalidCoordinate = "91.0000,0.0000";
    private const string ValidIpAddress = "192.168.1.1";
    private const string InvalidIpAddress = "256.1.1.1";
    private const string ValidIpv6 = "2001:0db8:85a3:0000:0000:8a2e:0370:7334";
    private const string InvalidIpv6 = "invalid-ipv6";
    private const string ValidPasswordStrength = "SecurePass123!";
    private const string InvalidPasswordStrength = "weak";
    private const string ValidDate = "2023-12-25";
    private const string InvalidDate = "invalid-date";
    private const string ValidGuid = "12345678-1234-1234-1234-123456789012";
    private const string InvalidGuid = "invalid-guid";
    private const string ValidRequired = "not empty";
    private const string ValidNotEmpty = "not empty";
    private const string InvalidNotEmpty = "";
    private const int ValidRange = 50;
    private const int InvalidRange = 150;
    private const string ValidMinLength = "hello world";
    private const string InvalidMinLength = "hi";
    private const string ValidMaxLength = "short";
    private const string InvalidMaxLength = "this is a very long string that exceeds maximum length";
    private const string ValidExactLength = "hello";
    private const string InvalidExactLength = "hi";
    private const string ValidRegex = "test123";
    private const string InvalidRegex = "invalid";
    private const int ValidPositive = 42;
    private const int InvalidPositive = -10;
    private const int ValidNegative = -5;
    private const int InvalidNegative = 10;
    private const int ValidNonZero = 7;
    private const int InvalidNonZero = 0;
    private const int ValidEven = 8;
    private const int InvalidEven = 7;
    private const int ValidOdd = 9;
    private const int InvalidOdd = 10;
    private const string ValidIsEmpty = "";
    private const string InvalidIsEmpty = "not empty";
    private const string ValidHasDigits = "abc123";
    private const string InvalidHasDigits = "abcdef";
    private const string ValidHasLetters = "123abc";
    private const string InvalidHasLetters = "123456";
    private const string ValidIsLowerCase = "hello";
    private const string InvalidIsLowerCase = "Hello";
    private const string ValidIsUpperCase = "HELLO";
    private const string InvalidIsUpperCase = "Hello";
    private const string ValidStartsWith = "prefix_data";
    private const string InvalidStartsWith = "data_suffix";
    private const string ValidEndsWith = "file.txt";
    private const string InvalidEndsWith = "file.pdf";
    private const string ValidContains = "hello world";
    private const string InvalidContains = "goodbye";
    private const string ValidEqual = "constant";
    private const string InvalidEqual = "different";
    private const string ValidIsIn = "option1";
    private const string InvalidIsIn = "invalid";
    private const string ValidNotIn = "allowed";
    private const string InvalidNotIn = "banned";
    private const string ValidNotEqual = "allowed";
    private const string InvalidNotEqual = "banned";
    private readonly List<string> ValidUniqueList = new() { "a", "b", "c" };
    private readonly List<string> InvalidUniqueList = new() { "a", "b", "a" };
    private readonly List<string> ValidMinCountList = new() { "a", "b", "c" };
    private readonly List<string> InvalidMinCountList = new();
    private readonly List<string> ValidMaxCountList = new() { "a", "b" };
    private readonly List<string> InvalidMaxCountList = new() { "a", "b", "c", "d", "e", "f" };
    private readonly List<string> ValidEmptyList = new();
    private readonly List<string> InvalidEmptyList = new() { "not", "empty" };
    private readonly DateTime ValidFutureDate = DateTime.UtcNow.AddDays(1);
    private readonly DateTime InvalidFutureDate = DateTime.UtcNow.AddDays(-1);
    private readonly DateTime ValidPastDate = DateTime.UtcNow.AddDays(-1);
    private readonly DateTime InvalidPastDate = DateTime.UtcNow.AddDays(1);
    private readonly DateTime ValidTodayDate = DateTime.Today;
    private readonly DateTime InvalidTodayDate = DateTime.Today.AddDays(1);
    private const int ValidBetween = 75;
    private const int InvalidBetween = 150;
    private const string ValidLength = "hello";
    private const string InvalidLength = "this is too long";
    private const int ValidZero = 0;
    private const int InvalidZero = 5;
    private const TestEnum ValidEnum = TestEnum.Option1;
    private const string ValidPostalCode = "12345";
    private const string InvalidPostalCode = "invalid";

    private enum TestEnum { Option1, Option2, Option3 }

    [Benchmark(Baseline = true)]
    public async Task EmailValidation_Valid()
    {
        await _emailRule.ValidateAsync(ValidEmail);
    }

    [Benchmark]
    public async Task EmailValidation_Invalid()
    {
        await _emailRule.ValidateAsync(InvalidEmail);
    }

    [Benchmark]
    public async Task UrlValidation_Valid()
    {
        await _urlRule.ValidateAsync(ValidUrl);
    }

    [Benchmark]
    public async Task UrlValidation_Invalid()
    {
        await _urlRule.ValidateAsync(InvalidUrl);
    }

    [Benchmark]
    public async Task PhoneValidation_Valid()
    {
        await _phoneRule.ValidateAsync(ValidPhone);
    }

    [Benchmark]
    public async Task PhoneValidation_Invalid()
    {
        await _phoneRule.ValidateAsync(InvalidPhone);
    }

    [Benchmark]
    public async Task CreditCardValidation_Valid()
    {
        await _creditCardRule.ValidateAsync(ValidCreditCard);
    }

    [Benchmark]
    public async Task CreditCardValidation_Invalid()
    {
        await _creditCardRule.ValidateAsync(InvalidCreditCard);
    }

    [Benchmark]
    public async Task IsbnValidation_Valid()
    {
        await _isbnRule.ValidateAsync(ValidIsbn);
    }

    [Benchmark]
    public async Task IsbnValidation_Invalid()
    {
        await _isbnRule.ValidateAsync(InvalidIsbn);
    }

    [Benchmark]
    public async Task VinValidation_Valid()
    {
        await _vinRule.ValidateAsync(ValidVin);
    }

    [Benchmark]
    public async Task VinValidation_Invalid()
    {
        await _vinRule.ValidateAsync(InvalidVin);
    }

    [Benchmark]
    public async Task JwtValidation_Valid()
    {
        await _jwtRule.ValidateAsync(ValidJwt);
    }

    [Benchmark]
    public async Task JwtValidation_Invalid()
    {
        await _jwtRule.ValidateAsync(InvalidJwt);
    }

    [Benchmark]
    public async Task JsonValidation_Valid()
    {
        await _jsonRule.ValidateAsync(ValidJson);
    }

    [Benchmark]
    public async Task JsonValidation_Invalid()
    {
        await _jsonRule.ValidateAsync(InvalidJson);
    }

    [Benchmark]
    public async Task XmlValidation_Valid()
    {
        await _xmlRule.ValidateAsync(ValidXml);
    }

    [Benchmark]
    public async Task XmlValidation_Invalid()
    {
        await _xmlRule.ValidateAsync(InvalidXml);
    }

    [Benchmark]
    public async Task Base64Validation_Valid()
    {
        await _base64Rule.ValidateAsync(ValidBase64);
    }

    [Benchmark]
    public async Task Base64Validation_Invalid()
    {
        await _base64Rule.ValidateAsync(InvalidBase64);
    }

    [Benchmark]
    public async Task HexColorValidation_Valid()
    {
        await _hexColorRule.ValidateAsync(ValidHexColor);
    }

    [Benchmark]
    public async Task HexColorValidation_Invalid()
    {
        await _hexColorRule.ValidateAsync(InvalidHexColor);
    }

    [Benchmark]
    public async Task MacAddressValidation_Valid()
    {
        await _macRule.ValidateAsync(ValidMac);
    }

    [Benchmark]
    public async Task MacAddressValidation_Invalid()
    {
        await _macRule.ValidateAsync(InvalidMac);
    }

    [Benchmark]
    public async Task TimeZoneValidation_Valid()
    {
        await _timeZoneRule.ValidateAsync(ValidTimeZone);
    }

    [Benchmark]
    public async Task TimeZoneValidation_Invalid()
    {
        await _timeZoneRule.ValidateAsync(InvalidTimeZone);
    }

    [Benchmark]
    public async Task CurrencyCodeValidation_Valid()
    {
        await _currencyRule.ValidateAsync(ValidCurrency);
    }

    [Benchmark]
    public async Task CurrencyCodeValidation_Invalid()
    {
        await _currencyRule.ValidateAsync(InvalidCurrency);
    }

    [Benchmark]
    public async Task LanguageCodeValidation_Valid()
    {
        await _languageRule.ValidateAsync(ValidLanguage);
    }

    [Benchmark]
    public async Task LanguageCodeValidation_Invalid()
    {
        await _languageRule.ValidateAsync(InvalidLanguage);
    }

    [Benchmark]
    public async Task CountryCodeValidation_Valid()
    {
        await _countryRule.ValidateAsync(ValidCountry);
    }

    [Benchmark]
    public async Task CountryCodeValidation_Invalid()
    {
        await _countryRule.ValidateAsync(InvalidCountry);
    }

    [Benchmark]
    public async Task AgeValidation_Valid()
    {
        await _ageRule.ValidateAsync(ValidAge);
    }

    [Benchmark]
    public async Task AgeValidation_Invalid()
    {
        await _ageRule.ValidateAsync(InvalidAge);
    }

    [Benchmark]
    public async Task PercentageValidation_Valid()
    {
        await _percentageRule.ValidateAsync(ValidPercentage);
    }

    [Benchmark]
    public async Task PercentageValidation_Invalid()
    {
        await _percentageRule.ValidateAsync(InvalidPercentage);
    }

    [Benchmark]
    public async Task FileSizeValidation_Valid()
    {
        await _fileSizeRule.ValidateAsync(ValidFileSize);
    }

    [Benchmark]
    public async Task FileSizeValidation_Invalid()
    {
        await _fileSizeRule.ValidateAsync(InvalidFileSize);
    }

    [Benchmark]
    public async Task FileExtensionValidation_Valid()
    {
        await _fileExtensionRule.ValidateAsync(ValidFileExtension);
    }

    [Benchmark]
    public async Task FileExtensionValidation_Invalid()
    {
        await _fileExtensionRule.ValidateAsync(InvalidFileExtension);
    }

    [Benchmark]
    public async Task IbanValidation_Valid()
    {
        await _ibanRule.ValidateAsync(ValidIban);
    }

    [Benchmark]
    public async Task IbanValidation_Invalid()
    {
        await _ibanRule.ValidateAsync(InvalidIban);
    }

    [Benchmark]
    public async Task CurrencyAmountValidation_Valid()
    {
        await _currencyAmountRule.ValidateAsync(ValidCurrencyAmount);
    }

    [Benchmark]
    public async Task CurrencyAmountValidation_Invalid()
    {
        await _currencyAmountRule.ValidateAsync(InvalidCurrencyAmount);
    }

    [Benchmark]
    public async Task CronExpressionValidation_Valid()
    {
        await _cronRule.ValidateAsync(ValidCronExpression);
    }

    [Benchmark]
    public async Task CronExpressionValidation_Invalid()
    {
        await _cronRule.ValidateAsync(InvalidCronExpression);
    }

    [Benchmark]
    public async Task SemVerValidation_Valid()
    {
        await _semVerRule.ValidateAsync(ValidSemVer);
    }

    [Benchmark]
    public async Task SemVerValidation_Invalid()
    {
        await _semVerRule.ValidateAsync(InvalidSemVer);
    }

    [Benchmark]
    public async Task TimeValidation_Valid()
    {
        await _timeRule.ValidateAsync(ValidTime);
    }

    [Benchmark]
    public async Task TimeValidation_Invalid()
    {
        await _timeRule.ValidateAsync(InvalidTime);
    }

    [Benchmark]
    public async Task DurationValidation_Valid()
    {
        await _durationRule.ValidateAsync(ValidDuration);
    }

    [Benchmark]
    public async Task DurationValidation_Invalid()
    {
        await _durationRule.ValidateAsync(InvalidDuration);
    }

    [Benchmark]
    public async Task MimeTypeValidation_Valid()
    {
        await _mimeTypeRule.ValidateAsync(ValidMimeType);
    }

    [Benchmark]
    public async Task MimeTypeValidation_Invalid()
    {
        await _mimeTypeRule.ValidateAsync(InvalidMimeType);
    }

    [Benchmark]
    public async Task ColorValidation_Valid()
    {
        await _colorRule.ValidateAsync(ValidColor);
    }

    [Benchmark]
    public async Task ColorValidation_Invalid()
    {
        await _colorRule.ValidateAsync(InvalidColor);
    }

    [Benchmark]
    public async Task DomainValidation_Valid()
    {
        await _domainRule.ValidateAsync(ValidDomain);
    }

    [Benchmark]
    public async Task DomainValidation_Invalid()
    {
        await _domainRule.ValidateAsync(InvalidDomain);
    }

    [Benchmark]
    public async Task UsernameValidation_Valid()
    {
        await _usernameRule.ValidateAsync(ValidUsername);
    }

    [Benchmark]
    public async Task UsernameValidation_Invalid()
    {
        await _usernameRule.ValidateAsync(InvalidUsername);
    }

    [Benchmark]
    public async Task CoordinateValidation_Valid()
    {
        await _coordinateRule.ValidateAsync(ValidCoordinate);
    }

    [Benchmark]
    public async Task CoordinateValidation_Invalid()
    {
        await _coordinateRule.ValidateAsync(InvalidCoordinate);
    }

    [Benchmark]
    public async Task IpAddressValidation_Valid()
    {
        await _ipAddressRule.ValidateAsync(ValidIpAddress);
    }

    [Benchmark]
    public async Task IpAddressValidation_Invalid()
    {
        await _ipAddressRule.ValidateAsync(InvalidIpAddress);
    }

    [Benchmark]
    public async Task Ipv6Validation_Valid()
    {
        await _ipv6Rule.ValidateAsync(ValidIpv6);
    }

    [Benchmark]
    public async Task Ipv6Validation_Invalid()
    {
        await _ipv6Rule.ValidateAsync(InvalidIpv6);
    }

    [Benchmark]
    public async Task PasswordStrengthValidation_Valid()
    {
        await _passwordStrengthRule.ValidateAsync(ValidPasswordStrength);
    }

    [Benchmark]
    public async Task PasswordStrengthValidation_Invalid()
    {
        await _passwordStrengthRule.ValidateAsync(InvalidPasswordStrength);
    }

    [Benchmark]
    public async Task DateValidation_Valid()
    {
        await _dateRule.ValidateAsync(ValidDate);
    }

    [Benchmark]
    public async Task DateValidation_Invalid()
    {
        await _dateRule.ValidateAsync(InvalidDate);
    }

    [Benchmark]
    public async Task GuidValidation_Valid()
    {
        await _guidRule.ValidateAsync(ValidGuid);
    }

    [Benchmark]
    public async Task GuidValidation_Invalid()
    {
        await _guidRule.ValidateAsync(InvalidGuid);
    }

    [Benchmark]
    public async Task RequiredValidation_Valid()
    {
        await _requiredRule.ValidateAsync(ValidRequired);
    }

    [Benchmark]
    public async Task NotEmptyValidation_Valid()
    {
        await _notEmptyRule.ValidateAsync(ValidNotEmpty);
    }

    [Benchmark]
    public async Task NotEmptyValidation_Invalid()
    {
        await _notEmptyRule.ValidateAsync(InvalidNotEmpty);
    }

    [Benchmark]
    public async Task RangeValidation_Valid()
    {
        await _rangeRule.ValidateAsync(ValidRange);
    }

    [Benchmark]
    public async Task RangeValidation_Invalid()
    {
        await _rangeRule.ValidateAsync(InvalidRange);
    }

    [Benchmark]
    public async Task MinLengthValidation_Valid()
    {
        await _minLengthRule.ValidateAsync(ValidMinLength);
    }

    [Benchmark]
    public async Task MinLengthValidation_Invalid()
    {
        await _minLengthRule.ValidateAsync(InvalidMinLength);
    }

    [Benchmark]
    public async Task MaxLengthValidation_Valid()
    {
        await _maxLengthRule.ValidateAsync(ValidMaxLength);
    }

    [Benchmark]
    public async Task MaxLengthValidation_Invalid()
    {
        await _maxLengthRule.ValidateAsync(InvalidMaxLength);
    }

    [Benchmark]
    public async Task ExactLengthValidation_Valid()
    {
        await _exactLengthRule.ValidateAsync(ValidExactLength);
    }

    [Benchmark]
    public async Task ExactLengthValidation_Invalid()
    {
        await _exactLengthRule.ValidateAsync(InvalidExactLength);
    }

    [Benchmark]
    public async Task RegexValidation_Valid()
    {
        await _regexRule.ValidateAsync(ValidRegex);
    }

    [Benchmark]
    public async Task RegexValidation_Invalid()
    {
        await _regexRule.ValidateAsync(InvalidRegex);
    }

    [Benchmark]
    public async Task PositiveValidation_Valid()
    {
        await _positiveRule.ValidateAsync(ValidPositive);
    }

    [Benchmark]
    public async Task PositiveValidation_Invalid()
    {
        await _positiveRule.ValidateAsync(InvalidPositive);
    }

    [Benchmark]
    public async Task NegativeValidation_Valid()
    {
        await _negativeRule.ValidateAsync(ValidNegative);
    }

    [Benchmark]
    public async Task NegativeValidation_Invalid()
    {
        await _negativeRule.ValidateAsync(InvalidNegative);
    }

    [Benchmark]
    public async Task NonZeroValidation_Valid()
    {
        await _nonZeroRule.ValidateAsync(ValidNonZero);
    }

    [Benchmark]
    public async Task NonZeroValidation_Invalid()
    {
        await _nonZeroRule.ValidateAsync(InvalidNonZero);
    }

    [Benchmark]
    public async Task EvenValidation_Valid()
    {
        await _evenRule.ValidateAsync(ValidEven);
    }

    [Benchmark]
    public async Task EvenValidation_Invalid()
    {
        await _evenRule.ValidateAsync(InvalidEven);
    }

    [Benchmark]
    public async Task OddValidation_Valid()
    {
        await _oddRule.ValidateAsync(ValidOdd);
    }

    [Benchmark]
    public async Task OddValidation_Invalid()
    {
        await _oddRule.ValidateAsync(InvalidOdd);
    }

    [Benchmark]
    public async Task IsEmptyValidation_Valid()
    {
        await _isEmptyRule.ValidateAsync(ValidIsEmpty);
    }

    [Benchmark]
    public async Task IsEmptyValidation_Invalid()
    {
        await _isEmptyRule.ValidateAsync(InvalidIsEmpty);
    }

    [Benchmark]
    public async Task HasDigitsValidation_Valid()
    {
        await _hasDigitsRule.ValidateAsync(ValidHasDigits);
    }

    [Benchmark]
    public async Task HasDigitsValidation_Invalid()
    {
        await _hasDigitsRule.ValidateAsync(InvalidHasDigits);
    }

    [Benchmark]
    public async Task HasLettersValidation_Valid()
    {
        await _hasLettersRule.ValidateAsync(ValidHasLetters);
    }

    [Benchmark]
    public async Task HasLettersValidation_Invalid()
    {
        await _hasLettersRule.ValidateAsync(InvalidHasLetters);
    }

    [Benchmark]
    public async Task IsLowerCaseValidation_Valid()
    {
        await _isLowerCaseRule.ValidateAsync(ValidIsLowerCase);
    }

    [Benchmark]
    public async Task IsLowerCaseValidation_Invalid()
    {
        await _isLowerCaseRule.ValidateAsync(InvalidIsLowerCase);
    }

    [Benchmark]
    public async Task IsUpperCaseValidation_Valid()
    {
        await _isUpperCaseRule.ValidateAsync(ValidIsUpperCase);
    }

    [Benchmark]
    public async Task IsUpperCaseValidation_Invalid()
    {
        await _isUpperCaseRule.ValidateAsync(InvalidIsUpperCase);
    }

    [Benchmark]
    public async Task StartsWithValidation_Valid()
    {
        await _startsWithRule.ValidateAsync(ValidStartsWith);
    }

    [Benchmark]
    public async Task StartsWithValidation_Invalid()
    {
        await _startsWithRule.ValidateAsync(InvalidStartsWith);
    }

    [Benchmark]
    public async Task EndsWithValidation_Valid()
    {
        await _endsWithRule.ValidateAsync(ValidEndsWith);
    }

    [Benchmark]
    public async Task EndsWithValidation_Invalid()
    {
        await _endsWithRule.ValidateAsync(InvalidEndsWith);
    }

    [Benchmark]
    public async Task ContainsValidation_Valid()
    {
        await _containsRule.ValidateAsync(ValidContains);
    }

    [Benchmark]
    public async Task ContainsValidation_Invalid()
    {
        await _containsRule.ValidateAsync(InvalidContains);
    }

    [Benchmark]
    public async Task IsEqualValidation_Valid()
    {
        await _equalRule.ValidateAsync(ValidEqual);
    }

    [Benchmark]
    public async Task IsEqualValidation_Invalid()
    {
        await _equalRule.ValidateAsync(InvalidEqual);
    }

    [Benchmark]
    public async Task NotEqualValidation_Valid()
    {
        await _notEqualRule.ValidateAsync(ValidNotEqual);
    }

    [Benchmark]
    public async Task NotEqualValidation_Invalid()
    {
        await _notEqualRule.ValidateAsync(InvalidNotEqual);
    }

    [Benchmark]
    public async Task IsInValidation_Valid()
    {
        await _isInRule.ValidateAsync(ValidIsIn);
    }

    [Benchmark]
    public async Task IsInValidation_Invalid()
    {
        await _isInRule.ValidateAsync(InvalidIsIn);
    }

    [Benchmark]
    public async Task NotInValidation_Valid()
    {
        await _notInRule.ValidateAsync(ValidNotIn);
    }

    [Benchmark]
    public async Task NotInValidation_Invalid()
    {
        await _notInRule.ValidateAsync(InvalidNotIn);
    }

    [Benchmark]
    public async Task UniqueValidation_Valid()
    {
        await _uniqueRule.ValidateAsync(ValidUniqueList);
    }

    [Benchmark]
    public async Task UniqueValidation_Invalid()
    {
        await _uniqueRule.ValidateAsync(InvalidUniqueList);
    }

    [Benchmark]
    public async Task MinCountValidation_Valid()
    {
        await _minCountRule.ValidateAsync(ValidMinCountList);
    }

    [Benchmark]
    public async Task MinCountValidation_Invalid()
    {
        await _minCountRule.ValidateAsync(InvalidMinCountList);
    }

    [Benchmark]
    public async Task MaxCountValidation_Valid()
    {
        await _maxCountRule.ValidateAsync(ValidMaxCountList);
    }

    [Benchmark]
    public async Task MaxCountValidation_Invalid()
    {
        await _maxCountRule.ValidateAsync(InvalidMaxCountList);
    }

    [Benchmark]
    public async Task EmptyValidation_Valid()
    {
        await _emptyRule.ValidateAsync(ValidEmptyList);
    }

    [Benchmark]
    public async Task EmptyValidation_Invalid()
    {
        await _emptyRule.ValidateAsync(InvalidEmptyList);
    }

    [Benchmark]
    public async Task FutureValidation_Valid()
    {
        await _futureRule.ValidateAsync(ValidFutureDate);
    }

    [Benchmark]
    public async Task FutureValidation_Invalid()
    {
        await _futureRule.ValidateAsync(InvalidFutureDate);
    }

    [Benchmark]
    public async Task PastValidation_Valid()
    {
        await _pastRule.ValidateAsync(ValidPastDate);
    }

    [Benchmark]
    public async Task PastValidation_Invalid()
    {
        await _pastRule.ValidateAsync(InvalidPastDate);
    }

    [Benchmark]
    public async Task TodayValidation_Valid()
    {
        await _todayRule.ValidateAsync(ValidTodayDate);
    }

    [Benchmark]
    public async Task TodayValidation_Invalid()
    {
        await _todayRule.ValidateAsync(InvalidTodayDate);
    }

    [Benchmark]
    public async Task BetweenValidation_Valid()
    {
        await _betweenRule.ValidateAsync(ValidBetween);
    }

    [Benchmark]
    public async Task BetweenValidation_Invalid()
    {
        await _betweenRule.ValidateAsync(InvalidBetween);
    }

    [Benchmark]
    public async Task LengthValidation_Valid()
    {
        await _lengthRule.ValidateAsync(ValidLength);
    }

    [Benchmark]
    public async Task LengthValidation_Invalid()
    {
        await _lengthRule.ValidateAsync(InvalidLength);
    }

    [Benchmark]
    public async Task ZeroValidation_Valid()
    {
        await _zeroRule.ValidateAsync(ValidZero);
    }

    [Benchmark]
    public async Task ZeroValidation_Invalid()
    {
        await _zeroRule.ValidateAsync(InvalidZero);
    }

    [Benchmark]
    public async Task EnumValidation_Valid()
    {
        await _enumRule.ValidateAsync(ValidEnum.ToString());
    }

    [Benchmark]
    public async Task EnumValidation_Invalid()
    {
        await _enumRule.ValidateAsync("InvalidEnum");
    }

    [Benchmark]
    public async Task PostalCodeValidation_Valid()
    {
        await _postalCodeRule.ValidateAsync(ValidPostalCode);
    }

    [Benchmark]
    public async Task PostalCodeValidation_Invalid()
    {
        await _postalCodeRule.ValidateAsync(InvalidPostalCode);
    }

    [Benchmark]
    public async Task ValidationPipeline_ComplexObject()
    {
        // Benchmark validating a complex object with multiple rules (78+ validation rules)
        var complexObject = new
        {
            // Original 16 rules
            Email = ValidEmail,
            Url = ValidUrl,
            Phone = ValidPhone,
            CreditCard = ValidCreditCard,
            Isbn = ValidIsbn,
            Vin = ValidVin,
            Jwt = ValidJwt,
            Json = ValidJson,
            Xml = ValidXml,
            Base64 = ValidBase64,
            HexColor = ValidHexColor,
            Mac = ValidMac,
            TimeZone = ValidTimeZone,
            Currency = ValidCurrency,
            Language = ValidLanguage,
            Country = ValidCountry,

            // New validation rules (62+ additional)
            Age = ValidAge,
            Percentage = ValidPercentage,
            FileSize = ValidFileSize,
            FileExtension = ValidFileExtension,
            Iban = ValidIban,
            CurrencyAmount = ValidCurrencyAmount,
            CronExpression = ValidCronExpression,
            SemVer = ValidSemVer,
            Time = ValidTime,
            Duration = ValidDuration,
            MimeType = ValidMimeType,
            Color = ValidColor,
            Domain = ValidDomain,
            Username = ValidUsername,
            Coordinate = ValidCoordinate,
            IpAddress = ValidIpAddress,
            Ipv6 = ValidIpv6,
            PasswordStrength = ValidPasswordStrength,
            Date = ValidDate,
            Guid = ValidGuid,
            Required = ValidRequired,
            NotEmpty = ValidNotEmpty,
            Range = ValidRange,
            MinLength = ValidMinLength,
            MaxLength = ValidMaxLength,
            ExactLength = ValidExactLength,
            Regex = ValidRegex,
            Positive = ValidPositive,
            Negative = ValidNegative,
            NonZero = ValidNonZero,
            Even = ValidEven,
            Odd = ValidOdd,
            IsEmpty = ValidIsEmpty,
            HasDigits = ValidHasDigits,
            HasLetters = ValidHasLetters,
            IsLowerCase = ValidIsLowerCase,
            IsUpperCase = ValidIsUpperCase,
            StartsWith = ValidStartsWith,
            EndsWith = ValidEndsWith,
            Contains = ValidContains,
            Equal = ValidEqual,
            NotEqual = ValidNotEqual,
            IsIn = ValidIsIn,
            NotIn = ValidNotIn,
            UniqueList = ValidUniqueList,
            MinCountList = ValidMinCountList,
            MaxCountList = ValidMaxCountList,
            EmptyList = ValidEmptyList,
            FutureDate = ValidFutureDate,
            PastDate = ValidPastDate,
            TodayDate = ValidTodayDate,
            Between = ValidBetween,
            Length = ValidLength,
            Zero = ValidZero,
            Enum = ValidEnum.ToString(),
            PostalCode = ValidPostalCode
        };

        // Simulate validating each field sequentially (more realistic for validation pipeline)
        // Original 16 rules
        await _emailRule.ValidateAsync(complexObject.Email);
        await _urlRule.ValidateAsync(complexObject.Url);
        await _phoneRule.ValidateAsync(complexObject.Phone);
        await _creditCardRule.ValidateAsync(complexObject.CreditCard);
        await _isbnRule.ValidateAsync(complexObject.Isbn);
        await _vinRule.ValidateAsync(complexObject.Vin);
        await _jwtRule.ValidateAsync(complexObject.Jwt);
        await _jsonRule.ValidateAsync(complexObject.Json);
        await _xmlRule.ValidateAsync(complexObject.Xml);
        await _base64Rule.ValidateAsync(complexObject.Base64);
        await _hexColorRule.ValidateAsync(complexObject.HexColor);
        await _macRule.ValidateAsync(complexObject.Mac);
        await _timeZoneRule.ValidateAsync(complexObject.TimeZone);
        await _currencyRule.ValidateAsync(complexObject.Currency);
        await _languageRule.ValidateAsync(complexObject.Language);
        await _countryRule.ValidateAsync(complexObject.Country);

        // New validation rules
        await _ageRule.ValidateAsync(complexObject.Age);
        await _percentageRule.ValidateAsync(complexObject.Percentage);
        await _fileSizeRule.ValidateAsync(complexObject.FileSize);
        await _fileExtensionRule.ValidateAsync(complexObject.FileExtension);
        await _ibanRule.ValidateAsync(complexObject.Iban);
        await _currencyAmountRule.ValidateAsync(complexObject.CurrencyAmount);
        await _cronRule.ValidateAsync(complexObject.CronExpression);
        await _semVerRule.ValidateAsync(complexObject.SemVer);
        await _timeRule.ValidateAsync(complexObject.Time);
        await _durationRule.ValidateAsync(complexObject.Duration);
        await _mimeTypeRule.ValidateAsync(complexObject.MimeType);
        await _colorRule.ValidateAsync(complexObject.Color);
        await _domainRule.ValidateAsync(complexObject.Domain);
        await _usernameRule.ValidateAsync(complexObject.Username);
        await _coordinateRule.ValidateAsync(complexObject.Coordinate);
        await _ipAddressRule.ValidateAsync(complexObject.IpAddress);
        await _ipv6Rule.ValidateAsync(complexObject.Ipv6);
        await _passwordStrengthRule.ValidateAsync(complexObject.PasswordStrength);
        await _dateRule.ValidateAsync(complexObject.Date);
        await _guidRule.ValidateAsync(complexObject.Guid);
        await _requiredRule.ValidateAsync(complexObject.Required);
        await _notEmptyRule.ValidateAsync(complexObject.NotEmpty);
        await _rangeRule.ValidateAsync(complexObject.Range);
        await _minLengthRule.ValidateAsync(complexObject.MinLength);
        await _maxLengthRule.ValidateAsync(complexObject.MaxLength);
        await _exactLengthRule.ValidateAsync(complexObject.ExactLength);
        await _regexRule.ValidateAsync(complexObject.Regex);
        await _positiveRule.ValidateAsync(complexObject.Positive);
        await _negativeRule.ValidateAsync(complexObject.Negative);
        await _nonZeroRule.ValidateAsync(complexObject.NonZero);
        await _evenRule.ValidateAsync(complexObject.Even);
        await _oddRule.ValidateAsync(complexObject.Odd);
        await _isEmptyRule.ValidateAsync(complexObject.IsEmpty);
        await _hasDigitsRule.ValidateAsync(complexObject.HasDigits);
        await _hasLettersRule.ValidateAsync(complexObject.HasLetters);
        await _isLowerCaseRule.ValidateAsync(complexObject.IsLowerCase);
        await _isUpperCaseRule.ValidateAsync(complexObject.IsUpperCase);
        await _startsWithRule.ValidateAsync(complexObject.StartsWith);
        await _endsWithRule.ValidateAsync(complexObject.EndsWith);
        await _containsRule.ValidateAsync(complexObject.Contains);
        await _equalRule.ValidateAsync(complexObject.Equal);
        await _notEqualRule.ValidateAsync(complexObject.NotEqual);
        await _isInRule.ValidateAsync(complexObject.IsIn);
        await _notInRule.ValidateAsync(complexObject.NotIn);
        await _uniqueRule.ValidateAsync(complexObject.UniqueList);
        await _minCountRule.ValidateAsync(complexObject.MinCountList);
        await _maxCountRule.ValidateAsync(complexObject.MaxCountList);
        await _emptyRule.ValidateAsync(complexObject.EmptyList);
        await _futureRule.ValidateAsync(complexObject.FutureDate);
        await _pastRule.ValidateAsync(complexObject.PastDate);
        await _todayRule.ValidateAsync(complexObject.TodayDate);
        await _betweenRule.ValidateAsync(complexObject.Between);
        await _lengthRule.ValidateAsync(complexObject.Length);
        await _zeroRule.ValidateAsync(complexObject.Zero);
        await _enumRule.ValidateAsync(complexObject.Enum);
        await _postalCodeRule.ValidateAsync(complexObject.PostalCode);
    }

    // Business Validation Benchmarks
    [Benchmark]
    public async Task BusinessValidation_Valid()
    {
        await _businessRulesEngine.ValidateBusinessRulesAsync(_validBusinessRequest);
    }

    [Benchmark]
    public async Task BusinessValidation_Invalid()
    {
        await _businessRulesEngine.ValidateBusinessRulesAsync(_invalidBusinessRequest);
    }

    [Benchmark]
    public async Task BusinessValidation_CustomRule_Valid()
    {
        var rule = new CustomBusinessValidationRule(_businessRulesEngine);
        await rule.ValidateAsync(_validBusinessRequest);
    }

    [Benchmark]
    public async Task BusinessValidation_CustomRule_Invalid()
    {
        var rule = new CustomBusinessValidationRule(_businessRulesEngine);
        await rule.ValidateAsync(_invalidBusinessRequest);
    }
}