using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for complex object validation
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class ComplexValidationBenchmarks
{
    private readonly Relay.Core.Validation.Rules.EmailValidationRule _emailRule = ValidationRuleFields.EmailRule;
    private readonly Relay.Core.Validation.Rules.UrlValidationRule _urlRule = ValidationRuleFields.UrlRule;
    private readonly Relay.Core.Validation.Rules.PhoneNumberValidationRule _phoneRule = ValidationRuleFields.PhoneRule;
    private readonly Relay.Core.Validation.Rules.CreditCardValidationRule _creditCardRule = ValidationRuleFields.CreditCardRule;
    private readonly Relay.Core.Validation.Rules.IsbnValidationRule _isbnRule = ValidationRuleFields.IsbnRule;
    private readonly Relay.Core.Validation.Rules.VinValidationRule _vinRule = ValidationRuleFields.VinRule;
    private readonly Relay.Core.Validation.Rules.JwtValidationRule _jwtRule = ValidationRuleFields.JwtRule;
    private readonly Relay.Core.Validation.Rules.JsonValidationRule _jsonRule = ValidationRuleFields.JsonRule;
    private readonly Relay.Core.Validation.Rules.XmlValidationRule _xmlRule = ValidationRuleFields.XmlRule;
    private readonly Relay.Core.Validation.Rules.Base64ValidationRule _base64Rule = ValidationRuleFields.Base64Rule;
    private readonly Relay.Core.Validation.Rules.HexColorValidationRule _hexColorRule = ValidationRuleFields.HexColorRule;
    private readonly Relay.Core.Validation.Rules.MacAddressValidationRule _macRule = ValidationRuleFields.MacRule;
    private readonly Relay.Core.Validation.Rules.TimeZoneValidationRule _timeZoneRule = ValidationRuleFields.TimeZoneRule;
    private readonly Relay.Core.Validation.Rules.CurrencyCodeValidationRule _currencyRule = ValidationRuleFields.CurrencyRule;
    private readonly Relay.Core.Validation.Rules.LanguageCodeValidationRule _languageRule = ValidationRuleFields.LanguageRule;
    private readonly Relay.Core.Validation.Rules.CountryCodeValidationRule _countryRule = ValidationRuleFields.CountryRule;
    private readonly Relay.Core.Validation.Rules.AgeValidationRule _ageRule = ValidationRuleFields.AgeRule;
    private readonly Relay.Core.Validation.Rules.PercentageValidationRule _percentageRule = ValidationRuleFields.PercentageRule;
    private readonly Relay.Core.Validation.Rules.FileSizeValidationRule _fileSizeRule = ValidationRuleFields.FileSizeRule;
    private readonly Relay.Core.Validation.Rules.FileExtensionValidationRule _fileExtensionRule = ValidationRuleFields.FileExtensionRule;
    private readonly Relay.Core.Validation.Rules.IbanValidationRule _ibanRule = ValidationRuleFields.IbanRule;
    private readonly Relay.Core.Validation.Rules.CurrencyAmountValidationRule _currencyAmountRule = ValidationRuleFields.CurrencyAmountRule;
    private readonly Relay.Core.Validation.Rules.CronExpressionValidationRule _cronRule = ValidationRuleFields.CronRule;
    private readonly Relay.Core.Validation.Rules.SemVerValidationRule _semVerRule = ValidationRuleFields.SemVerRule;
    private readonly Relay.Core.Validation.Rules.TimeValidationRule _timeRule = ValidationRuleFields.TimeRule;
    private readonly Relay.Core.Validation.Rules.DurationValidationRule _durationRule = ValidationRuleFields.DurationRule;
    private readonly Relay.Core.Validation.Rules.MimeTypeValidationRule _mimeTypeRule = ValidationRuleFields.MimeTypeRule;
    private readonly Relay.Core.Validation.Rules.ColorValidationRule _colorRule = ValidationRuleFields.ColorRule;
    private readonly Relay.Core.Validation.Rules.DomainValidationRule _domainRule = ValidationRuleFields.DomainRule;
    private readonly Relay.Core.Validation.Rules.UsernameValidationRule _usernameRule = ValidationRuleFields.UsernameRule;
    private readonly Relay.Core.Validation.Rules.CoordinateValidationRule _coordinateRule = ValidationRuleFields.CoordinateRule;
    private readonly Relay.Core.Validation.Rules.IpAddressValidationRule _ipAddressRule = ValidationRuleFields.IpAddressRule;
    private readonly Relay.Core.Validation.Rules.Ipv6ValidationRule _ipv6Rule = ValidationRuleFields.Ipv6Rule;
    private readonly Relay.Core.Validation.Rules.PasswordStrengthValidationRule _passwordStrengthRule = ValidationRuleFields.PasswordStrengthRule;
    private readonly Relay.Core.Validation.Rules.DateValidationRule _dateRule = ValidationRuleFields.DateRule;
    private readonly Relay.Core.Validation.Rules.GuidValidationRule _guidRule = ValidationRuleFields.GuidRule;
    private readonly Relay.Core.Validation.Rules.RequiredValidationRule<string> _requiredRule = ValidationRuleFields.RequiredRule;
    private readonly Relay.Core.Validation.Rules.NotEmptyValidationRule _notEmptyRule = ValidationRuleFields.NotEmptyRule;
    private readonly Relay.Core.Validation.Rules.RangeValidationRule<int> _rangeRule = ValidationRuleFields.RangeRule;
    private readonly Relay.Core.Validation.Rules.MinLengthValidationRule _minLengthRule = ValidationRuleFields.MinLengthRule;
    private readonly Relay.Core.Validation.Rules.MaxLengthValidationRule _maxLengthRule = ValidationRuleFields.MaxLengthRule;
    private readonly Relay.Core.Validation.Rules.ExactLengthValidationRule _exactLengthRule = ValidationRuleFields.ExactLengthRule;
    private readonly Relay.Core.Validation.Rules.RegexValidationRule _regexRule = ValidationRuleFields.RegexRule;
    private readonly Relay.Core.Validation.Rules.PositiveValidationRule<int> _positiveRule = ValidationRuleFields.PositiveRule;
    private readonly Relay.Core.Validation.Rules.NegativeValidationRule<int> _negativeRule = ValidationRuleFields.NegativeRule;
    private readonly Relay.Core.Validation.Rules.NonZeroValidationRule<int> _nonZeroRule = ValidationRuleFields.NonZeroRule;
    private readonly Relay.Core.Validation.Rules.EvenValidationRule _evenRule = ValidationRuleFields.EvenRule;
    private readonly Relay.Core.Validation.Rules.OddValidationRule _oddRule = ValidationRuleFields.OddRule;
    private readonly Relay.Core.Validation.Rules.IsEmptyValidationRule _isEmptyRule = ValidationRuleFields.IsEmptyRule;
    private readonly Relay.Core.Validation.Rules.HasDigitsValidationRule _hasDigitsRule = ValidationRuleFields.HasDigitsRule;
    private readonly Relay.Core.Validation.Rules.HasLettersValidationRule _hasLettersRule = ValidationRuleFields.HasLettersRule;
    private readonly Relay.Core.Validation.Rules.IsLowerCaseValidationRule _isLowerCaseRule = ValidationRuleFields.IsLowerCaseRule;
    private readonly Relay.Core.Validation.Rules.IsUpperCaseValidationRule _isUpperCaseRule = ValidationRuleFields.IsUpperCaseRule;
    private readonly Relay.Core.Validation.Rules.StartsWithValidationRule _startsWithRule = ValidationRuleFields.StartsWithRule;
    private readonly Relay.Core.Validation.Rules.EndsWithValidationRule _endsWithRule = ValidationRuleFields.EndsWithRule;
    private readonly Relay.Core.Validation.Rules.ContainsValidationRuleString _containsRule = ValidationRuleFields.ContainsRule;
    private readonly Relay.Core.Validation.Rules.IsEqualValidationRule<string> _equalRule = ValidationRuleFields.EqualRule;
    private readonly Relay.Core.Validation.Rules.NotEqualValidationRule<string> _notEqualRule = ValidationRuleFields.NotEqualRule;
    private readonly Relay.Core.Validation.Rules.IsInValidationRule<string> _isInRule = ValidationRuleFields.IsInRule;
    private readonly Relay.Core.Validation.Rules.NotInValidationRule<string> _notInRule = ValidationRuleFields.NotInRule;
    private readonly Relay.Core.Validation.Rules.UniqueValidationRule<string> _uniqueRule = ValidationRuleFields.UniqueRule;
    private readonly Relay.Core.Validation.Rules.MinCountValidationRule<string> _minCountRule = ValidationRuleFields.MinCountRule;
    private readonly Relay.Core.Validation.Rules.MaxCountValidationRule<string> _maxCountRule = ValidationRuleFields.MaxCountRule;
    private readonly Relay.Core.Validation.Rules.EmptyValidationRule<string> _emptyRule = ValidationRuleFields.EmptyRule;
    private readonly Relay.Core.Validation.Rules.FutureValidationRule _futureRule = ValidationRuleFields.FutureRule;
    private readonly Relay.Core.Validation.Rules.PastValidationRule _pastRule = ValidationRuleFields.PastRule;
    private readonly Relay.Core.Validation.Rules.TodayValidationRule _todayRule = ValidationRuleFields.TodayRule;
    private readonly Relay.Core.Validation.Rules.BetweenValidationRule<int> _betweenRule = ValidationRuleFields.BetweenRule;
    private readonly Relay.Core.Validation.Rules.LengthValidationRule _lengthRule = ValidationRuleFields.LengthRule;
    private readonly Relay.Core.Validation.Rules.ZeroValidationRule<int> _zeroRule = ValidationRuleFields.ZeroRule;
    private readonly Relay.Core.Validation.Rules.EnumValidationRule<TestDataConstants.TestEnum> _enumRule = ValidationRuleFields.EnumRule;
    private readonly Relay.Core.Validation.Rules.PostalCodeValidationRule _postalCodeRule = ValidationRuleFields.PostalCodeRule;

    [Benchmark]
    public async Task ValidationPipeline_ComplexObject()
    {
        // Benchmark validating a complex object with multiple rules (78+ validation rules)
        var complexObject = new
        {
            // Original 16 rules
            Email = TestDataConstants.ValidEmail,
            Url = TestDataConstants.ValidUrl,
            Phone = TestDataConstants.ValidPhone,
            CreditCard = TestDataConstants.ValidCreditCard,
            Isbn = TestDataConstants.ValidIsbn,
            Vin = TestDataConstants.ValidVin,
            Jwt = TestDataConstants.ValidJwt,
            Json = TestDataConstants.ValidJson,
            Xml = TestDataConstants.ValidXml,
            Base64 = TestDataConstants.ValidBase64,
            HexColor = TestDataConstants.ValidHexColor,
            Mac = TestDataConstants.ValidMac,
            TimeZone = TestDataConstants.ValidTimeZone,
            Currency = TestDataConstants.ValidCurrency,
            Language = TestDataConstants.ValidLanguage,
            Country = TestDataConstants.ValidCountry,

            // New validation rules (62+ additional)
            Age = TestDataConstants.ValidAge,
            Percentage = TestDataConstants.ValidPercentage,
            FileSize = TestDataConstants.ValidFileSize,
            FileExtension = TestDataConstants.ValidFileExtension,
            Iban = TestDataConstants.ValidIban,
            CurrencyAmount = TestDataConstants.ValidCurrencyAmount,
            CronExpression = TestDataConstants.ValidCronExpression,
            SemVer = TestDataConstants.ValidSemVer,
            Time = TestDataConstants.ValidTime,
            Duration = TestDataConstants.ValidDuration,
            MimeType = TestDataConstants.ValidMimeType,
            Color = TestDataConstants.ValidColor,
            Domain = TestDataConstants.ValidDomain,
            Username = TestDataConstants.ValidUsername,
            Coordinate = TestDataConstants.ValidCoordinate,
            IpAddress = TestDataConstants.ValidIpAddress,
            Ipv6 = TestDataConstants.ValidIpv6,
            PasswordStrength = TestDataConstants.ValidPasswordStrength,
            Date = TestDataConstants.ValidDate,
            Guid = TestDataConstants.ValidGuid,
            Required = TestDataConstants.ValidRequired,
            NotEmpty = TestDataConstants.ValidNotEmpty,
            Range = TestDataConstants.ValidRange,
            MinLength = TestDataConstants.ValidMinLength,
            MaxLength = TestDataConstants.ValidMaxLength,
            ExactLength = TestDataConstants.ValidExactLength,
            Regex = TestDataConstants.ValidRegex,
            Positive = TestDataConstants.ValidPositive,
            Negative = TestDataConstants.ValidNegative,
            NonZero = TestDataConstants.ValidNonZero,
            Even = TestDataConstants.ValidEven,
            Odd = TestDataConstants.ValidOdd,
            IsEmpty = TestDataConstants.ValidIsEmpty,
            HasDigits = TestDataConstants.ValidHasDigits,
            HasLetters = TestDataConstants.ValidHasLetters,
            IsLowerCase = TestDataConstants.ValidIsLowerCase,
            IsUpperCase = TestDataConstants.ValidIsUpperCase,
            StartsWith = TestDataConstants.ValidStartsWith,
            EndsWith = TestDataConstants.ValidEndsWith,
            Contains = TestDataConstants.ValidContains,
            Equal = TestDataConstants.ValidEqual,
            NotEqual = TestDataConstants.ValidNotEqual,
            IsIn = TestDataConstants.ValidIsIn,
            NotIn = TestDataConstants.ValidNotIn,
            UniqueList = TestDataConstants.ValidUniqueList,
            MinCountList = TestDataConstants.ValidMinCountList,
            MaxCountList = TestDataConstants.ValidMaxCountList,
            EmptyList = TestDataConstants.ValidEmptyList,
            FutureDate = TestDataConstants.ValidFutureDate,
            PastDate = TestDataConstants.ValidPastDate,
            TodayDate = TestDataConstants.ValidTodayDate,
            Between = TestDataConstants.ValidBetween,
            Length = TestDataConstants.ValidLength,
            Zero = TestDataConstants.ValidZero,
            Enum = TestDataConstants.ValidEnum.ToString(),
            PostalCode = TestDataConstants.ValidPostalCode
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
}