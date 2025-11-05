using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for format validation rules (email, URL, phone, etc.)
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class FormatValidationBenchmarks
{
    private readonly EmailValidationRule _emailRule = ValidationRuleFields.EmailRule;
    private readonly UrlValidationRule _urlRule = ValidationRuleFields.UrlRule;
    private readonly PhoneNumberValidationRule _phoneRule = ValidationRuleFields.PhoneRule;
    private readonly CreditCardValidationRule _creditCardRule = ValidationRuleFields.CreditCardRule;
    private readonly IsbnValidationRule _isbnRule = ValidationRuleFields.IsbnRule;
    private readonly VinValidationRule _vinRule = ValidationRuleFields.VinRule;
    private readonly JwtValidationRule _jwtRule = ValidationRuleFields.JwtRule;
    private readonly JsonValidationRule _jsonRule = ValidationRuleFields.JsonRule;
    private readonly XmlValidationRule _xmlRule = ValidationRuleFields.XmlRule;
    private readonly Base64ValidationRule _base64Rule = ValidationRuleFields.Base64Rule;
    private readonly HexColorValidationRule _hexColorRule = ValidationRuleFields.HexColorRule;
    private readonly MacAddressValidationRule _macRule = ValidationRuleFields.MacRule;
    private readonly TimeZoneValidationRule _timeZoneRule = ValidationRuleFields.TimeZoneRule;
    private readonly CurrencyCodeValidationRule _currencyRule = ValidationRuleFields.CurrencyRule;
    private readonly LanguageCodeValidationRule _languageRule = ValidationRuleFields.LanguageRule;
    private readonly CountryCodeValidationRule _countryRule = ValidationRuleFields.CountryRule;

    [Benchmark(Baseline = true)]
    public async Task EmailValidation_Valid()
    {
        await _emailRule.ValidateAsync(TestDataConstants.ValidEmail);
    }

    [Benchmark]
    public async Task EmailValidation_Invalid()
    {
        await _emailRule.ValidateAsync(TestDataConstants.InvalidEmail);
    }

    [Benchmark]
    public async Task UrlValidation_Valid()
    {
        await _urlRule.ValidateAsync(TestDataConstants.ValidUrl);
    }

    [Benchmark]
    public async Task UrlValidation_Invalid()
    {
        await _urlRule.ValidateAsync(TestDataConstants.InvalidUrl);
    }

    [Benchmark]
    public async Task PhoneValidation_Valid()
    {
        await _phoneRule.ValidateAsync(TestDataConstants.ValidPhone);
    }

    [Benchmark]
    public async Task PhoneValidation_Invalid()
    {
        await _phoneRule.ValidateAsync(TestDataConstants.InvalidPhone);
    }

    [Benchmark]
    public async Task CreditCardValidation_Valid()
    {
        await _creditCardRule.ValidateAsync(TestDataConstants.ValidCreditCard);
    }

    [Benchmark]
    public async Task CreditCardValidation_Invalid()
    {
        await _creditCardRule.ValidateAsync(TestDataConstants.InvalidCreditCard);
    }

    [Benchmark]
    public async Task IsbnValidation_Valid()
    {
        await _isbnRule.ValidateAsync(TestDataConstants.ValidIsbn);
    }

    [Benchmark]
    public async Task IsbnValidation_Invalid()
    {
        await _isbnRule.ValidateAsync(TestDataConstants.InvalidIsbn);
    }

    [Benchmark]
    public async Task VinValidation_Valid()
    {
        await _vinRule.ValidateAsync(TestDataConstants.ValidVin);
    }

    [Benchmark]
    public async Task VinValidation_Invalid()
    {
        await _vinRule.ValidateAsync(TestDataConstants.InvalidVin);
    }

    [Benchmark]
    public async Task JwtValidation_Valid()
    {
        await _jwtRule.ValidateAsync(TestDataConstants.ValidJwt);
    }

    [Benchmark]
    public async Task JwtValidation_Invalid()
    {
        await _jwtRule.ValidateAsync(TestDataConstants.InvalidJwt);
    }

    [Benchmark]
    public async Task JsonValidation_Valid()
    {
        await _jsonRule.ValidateAsync(TestDataConstants.ValidJson);
    }

    [Benchmark]
    public async Task JsonValidation_Invalid()
    {
        await _jsonRule.ValidateAsync(TestDataConstants.InvalidJson);
    }

    [Benchmark]
    public async Task XmlValidation_Valid()
    {
        await _xmlRule.ValidateAsync(TestDataConstants.ValidXml);
    }

    [Benchmark]
    public async Task XmlValidation_Invalid()
    {
        await _xmlRule.ValidateAsync(TestDataConstants.InvalidXml);
    }

    [Benchmark]
    public async Task Base64Validation_Valid()
    {
        await _base64Rule.ValidateAsync(TestDataConstants.ValidBase64);
    }

    [Benchmark]
    public async Task Base64Validation_Invalid()
    {
        await _base64Rule.ValidateAsync(TestDataConstants.InvalidBase64);
    }

    [Benchmark]
    public async Task HexColorValidation_Valid()
    {
        await _hexColorRule.ValidateAsync(TestDataConstants.ValidHexColor);
    }

    [Benchmark]
    public async Task HexColorValidation_Invalid()
    {
        await _hexColorRule.ValidateAsync(TestDataConstants.InvalidHexColor);
    }

    [Benchmark]
    public async Task MacAddressValidation_Valid()
    {
        await _macRule.ValidateAsync(TestDataConstants.ValidMac);
    }

    [Benchmark]
    public async Task MacAddressValidation_Invalid()
    {
        await _macRule.ValidateAsync(TestDataConstants.InvalidMac);
    }

    [Benchmark]
    public async Task TimeZoneValidation_Valid()
    {
        await _timeZoneRule.ValidateAsync(TestDataConstants.ValidTimeZone);
    }

    [Benchmark]
    public async Task TimeZoneValidation_Invalid()
    {
        await _timeZoneRule.ValidateAsync(TestDataConstants.InvalidTimeZone);
    }

    [Benchmark]
    public async Task CurrencyCodeValidation_Valid()
    {
        await _currencyRule.ValidateAsync(TestDataConstants.ValidCurrency);
    }

    [Benchmark]
    public async Task CurrencyCodeValidation_Invalid()
    {
        await _currencyRule.ValidateAsync(TestDataConstants.InvalidCurrency);
    }

    [Benchmark]
    public async Task LanguageCodeValidation_Valid()
    {
        await _languageRule.ValidateAsync(TestDataConstants.ValidLanguage);
    }

    [Benchmark]
    public async Task LanguageCodeValidation_Invalid()
    {
        await _languageRule.ValidateAsync(TestDataConstants.InvalidLanguage);
    }

    [Benchmark]
    public async Task CountryCodeValidation_Valid()
    {
        await _countryRule.ValidateAsync(TestDataConstants.ValidCountry);
    }

    [Benchmark]
    public async Task CountryCodeValidation_Invalid()
    {
        await _countryRule.ValidateAsync(TestDataConstants.InvalidCountry);
    }
}
