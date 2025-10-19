using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for advanced format validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class AdvancedFormatValidationBenchmarks
{
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
    private readonly Relay.Core.Validation.Rules.PostalCodeValidationRule _postalCodeRule = ValidationRuleFields.PostalCodeRule;

    [Benchmark]
    public async Task AgeValidation_Valid()
    {
        await _ageRule.ValidateAsync(TestDataConstants.ValidAge);
    }

    [Benchmark]
    public async Task AgeValidation_Invalid()
    {
        await _ageRule.ValidateAsync(TestDataConstants.InvalidAge);
    }

    [Benchmark]
    public async Task PercentageValidation_Valid()
    {
        await _percentageRule.ValidateAsync(TestDataConstants.ValidPercentage);
    }

    [Benchmark]
    public async Task PercentageValidation_Invalid()
    {
        await _percentageRule.ValidateAsync(TestDataConstants.InvalidPercentage);
    }

    [Benchmark]
    public async Task FileSizeValidation_Valid()
    {
        await _fileSizeRule.ValidateAsync(TestDataConstants.ValidFileSize);
    }

    [Benchmark]
    public async Task FileSizeValidation_Invalid()
    {
        await _fileSizeRule.ValidateAsync(TestDataConstants.InvalidFileSize);
    }

    [Benchmark]
    public async Task FileExtensionValidation_Valid()
    {
        await _fileExtensionRule.ValidateAsync(TestDataConstants.ValidFileExtension);
    }

    [Benchmark]
    public async Task FileExtensionValidation_Invalid()
    {
        await _fileExtensionRule.ValidateAsync(TestDataConstants.InvalidFileExtension);
    }

    [Benchmark]
    public async Task IbanValidation_Valid()
    {
        await _ibanRule.ValidateAsync(TestDataConstants.ValidIban);
    }

    [Benchmark]
    public async Task IbanValidation_Invalid()
    {
        await _ibanRule.ValidateAsync(TestDataConstants.InvalidIban);
    }

    [Benchmark]
    public async Task CurrencyAmountValidation_Valid()
    {
        await _currencyAmountRule.ValidateAsync(TestDataConstants.ValidCurrencyAmount);
    }

    [Benchmark]
    public async Task CurrencyAmountValidation_Invalid()
    {
        await _currencyAmountRule.ValidateAsync(TestDataConstants.InvalidCurrencyAmount);
    }

    [Benchmark]
    public async Task CronExpressionValidation_Valid()
    {
        await _cronRule.ValidateAsync(TestDataConstants.ValidCronExpression);
    }

    [Benchmark]
    public async Task CronExpressionValidation_Invalid()
    {
        await _cronRule.ValidateAsync(TestDataConstants.InvalidCronExpression);
    }

    [Benchmark]
    public async Task SemVerValidation_Valid()
    {
        await _semVerRule.ValidateAsync(TestDataConstants.ValidSemVer);
    }

    [Benchmark]
    public async Task SemVerValidation_Invalid()
    {
        await _semVerRule.ValidateAsync(TestDataConstants.InvalidSemVer);
    }

    [Benchmark]
    public async Task TimeValidation_Valid()
    {
        await _timeRule.ValidateAsync(TestDataConstants.ValidTime);
    }

    [Benchmark]
    public async Task TimeValidation_Invalid()
    {
        await _timeRule.ValidateAsync(TestDataConstants.InvalidTime);
    }

    [Benchmark]
    public async Task DurationValidation_Valid()
    {
        await _durationRule.ValidateAsync(TestDataConstants.ValidDuration);
    }

    [Benchmark]
    public async Task DurationValidation_Invalid()
    {
        await _durationRule.ValidateAsync(TestDataConstants.InvalidDuration);
    }

    [Benchmark]
    public async Task MimeTypeValidation_Valid()
    {
        await _mimeTypeRule.ValidateAsync(TestDataConstants.ValidMimeType);
    }

    [Benchmark]
    public async Task MimeTypeValidation_Invalid()
    {
        await _mimeTypeRule.ValidateAsync(TestDataConstants.InvalidMimeType);
    }

    [Benchmark]
    public async Task ColorValidation_Valid()
    {
        await _colorRule.ValidateAsync(TestDataConstants.ValidColor);
    }

    [Benchmark]
    public async Task ColorValidation_Invalid()
    {
        await _colorRule.ValidateAsync(TestDataConstants.InvalidColor);
    }

    [Benchmark]
    public async Task DomainValidation_Valid()
    {
        await _domainRule.ValidateAsync(TestDataConstants.ValidDomain);
    }

    [Benchmark]
    public async Task DomainValidation_Invalid()
    {
        await _domainRule.ValidateAsync(TestDataConstants.InvalidDomain);
    }

    [Benchmark]
    public async Task UsernameValidation_Valid()
    {
        await _usernameRule.ValidateAsync(TestDataConstants.ValidUsername);
    }

    [Benchmark]
    public async Task UsernameValidation_Invalid()
    {
        await _usernameRule.ValidateAsync(TestDataConstants.InvalidUsername);
    }

    [Benchmark]
    public async Task CoordinateValidation_Valid()
    {
        await _coordinateRule.ValidateAsync(TestDataConstants.ValidCoordinate);
    }

    [Benchmark]
    public async Task CoordinateValidation_Invalid()
    {
        await _coordinateRule.ValidateAsync(TestDataConstants.InvalidCoordinate);
    }

    [Benchmark]
    public async Task IpAddressValidation_Valid()
    {
        await _ipAddressRule.ValidateAsync(TestDataConstants.ValidIpAddress);
    }

    [Benchmark]
    public async Task IpAddressValidation_Invalid()
    {
        await _ipAddressRule.ValidateAsync(TestDataConstants.InvalidIpAddress);
    }

    [Benchmark]
    public async Task Ipv6Validation_Valid()
    {
        await _ipv6Rule.ValidateAsync(TestDataConstants.ValidIpv6);
    }

    [Benchmark]
    public async Task Ipv6Validation_Invalid()
    {
        await _ipv6Rule.ValidateAsync(TestDataConstants.InvalidIpv6);
    }

    [Benchmark]
    public async Task PasswordStrengthValidation_Valid()
    {
        await _passwordStrengthRule.ValidateAsync(TestDataConstants.ValidPasswordStrength);
    }

    [Benchmark]
    public async Task PasswordStrengthValidation_Invalid()
    {
        await _passwordStrengthRule.ValidateAsync(TestDataConstants.InvalidPasswordStrength);
    }

    [Benchmark]
    public async Task DateValidation_Valid()
    {
        await _dateRule.ValidateAsync(TestDataConstants.ValidDate);
    }

    [Benchmark]
    public async Task DateValidation_Invalid()
    {
        await _dateRule.ValidateAsync(TestDataConstants.InvalidDate);
    }

    [Benchmark]
    public async Task GuidValidation_Valid()
    {
        await _guidRule.ValidateAsync(TestDataConstants.ValidGuid);
    }

    [Benchmark]
    public async Task GuidValidation_Invalid()
    {
        await _guidRule.ValidateAsync(TestDataConstants.InvalidGuid);
    }

    [Benchmark]
    public async Task PostalCodeValidation_Valid()
    {
        await _postalCodeRule.ValidateAsync(TestDataConstants.ValidPostalCode);
    }

    [Benchmark]
    public async Task PostalCodeValidation_Invalid()
    {
        await _postalCodeRule.ValidateAsync(TestDataConstants.InvalidPostalCode);
    }
}