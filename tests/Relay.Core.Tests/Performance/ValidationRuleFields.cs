using System.Collections.Generic;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Validation rule fields for benchmarks
/// </summary>
internal static class ValidationRuleFields
{
    // Format validation rules
    internal static readonly EmailValidationRule EmailRule = new();
    internal static readonly UrlValidationRule UrlRule = new();
    internal static readonly PhoneNumberValidationRule PhoneRule = new();
    internal static readonly CreditCardValidationRule CreditCardRule = new();
    internal static readonly IsbnValidationRule IsbnRule = new();
    internal static readonly VinValidationRule VinRule = new();
    internal static readonly JwtValidationRule JwtRule = new();
    internal static readonly JsonValidationRule JsonRule = new();
    internal static readonly XmlValidationRule XmlRule = new();
    internal static readonly Base64ValidationRule Base64Rule = new();
    internal static readonly HexColorValidationRule HexColorRule = new();
    internal static readonly MacAddressValidationRule MacRule = new();
    internal static readonly TimeZoneValidationRule TimeZoneRule = new();
    internal static readonly CurrencyCodeValidationRule CurrencyRule = new();
    internal static readonly LanguageCodeValidationRule LanguageRule = new();
    internal static readonly CountryCodeValidationRule CountryRule = new();

    // Additional validation rules
    internal static readonly AgeValidationRule AgeRule = new();
    internal static readonly PercentageValidationRule PercentageRule = new();
    internal static readonly FileSizeValidationRule FileSizeRule = new(10485760); // 10MB
    internal static readonly FileExtensionValidationRule FileExtensionRule = new(new[] { ".txt", ".jpg", ".png" });
    internal static readonly IbanValidationRule IbanRule = new();
    internal static readonly CurrencyAmountValidationRule CurrencyAmountRule = new();
    internal static readonly CronExpressionValidationRule CronRule = new();
    internal static readonly SemVerValidationRule SemVerRule = new();
    internal static readonly TimeValidationRule TimeRule = new();
    internal static readonly DurationValidationRule DurationRule = new();
    internal static readonly MimeTypeValidationRule MimeTypeRule = new();
    internal static readonly ColorValidationRule ColorRule = new();
    internal static readonly DomainValidationRule DomainRule = new();
    internal static readonly UsernameValidationRule UsernameRule = new();
    internal static readonly CoordinateValidationRule CoordinateRule = new();
    internal static readonly IpAddressValidationRule IpAddressRule = new();
    internal static readonly Ipv6ValidationRule Ipv6Rule = new();
    internal static readonly PasswordStrengthValidationRule PasswordStrengthRule = new();
    internal static readonly DateValidationRule DateRule = new();
    internal static readonly GuidValidationRule GuidRule = new();
    internal static readonly RequiredValidationRule<string> RequiredRule = new();
    internal static readonly NotEmptyValidationRule NotEmptyRule = new();
    internal static readonly RangeValidationRule<int> RangeRule = new(0, 100);
    internal static readonly MinLengthValidationRule MinLengthRule = new();
    internal static readonly MaxLengthValidationRule MaxLengthRule = new();
    internal static readonly ExactLengthValidationRule ExactLengthRule = new(5);
    internal static readonly RegexValidationRule RegexRule = new(@"^\w+$");
    internal static readonly PositiveValidationRule<int> PositiveRule = new();
    internal static readonly NegativeValidationRule<int> NegativeRule = new();
    internal static readonly NonZeroValidationRule<int> NonZeroRule = new();
    internal static readonly EvenValidationRule EvenRule = new();
    internal static readonly OddValidationRule OddRule = new();
    internal static readonly IsEmptyValidationRule IsEmptyRule = new();
    internal static readonly HasDigitsValidationRule HasDigitsRule = new();
    internal static readonly HasLettersValidationRule HasLettersRule = new();
    internal static readonly IsLowerCaseValidationRule IsLowerCaseRule = new();
    internal static readonly IsUpperCaseValidationRule IsUpperCaseRule = new();
    internal static readonly StartsWithValidationRule StartsWithRule = new("prefix");
    internal static readonly EndsWithValidationRule EndsWithRule = new(".txt");
    internal static readonly ContainsValidationRuleString ContainsRule = new("test");
    internal static readonly IsEqualValidationRule<string> EqualRule = new("constant");
    internal static readonly NotEqualValidationRule<string> NotEqualRule = new("banned");
    internal static readonly IsInValidationRule<string> IsInRule = new(new[] { "option1", "option2" });
    internal static readonly NotInValidationRule<string> NotInRule = new(new[] { "banned1", "banned2" });
    internal static readonly UniqueValidationRule<string> UniqueRule = new();
    internal static readonly MinCountValidationRule<string> MinCountRule = new(1);
    internal static readonly MaxCountValidationRule<string> MaxCountRule = new(5);
    internal static readonly EmptyValidationRule<string> EmptyRule = new();
    internal static readonly FutureValidationRule FutureRule = new();
    internal static readonly PastValidationRule PastRule = new();
    internal static readonly TodayValidationRule TodayRule = new();
    internal static readonly BetweenValidationRule<int> BetweenRule = new(0, 100);
    internal static readonly LengthValidationRule LengthRule = new();
    internal static readonly ZeroValidationRule<int> ZeroRule = new();
    internal static readonly EnumValidationRule<TestDataConstants.TestEnum> EnumRule = new();
    internal static readonly PostalCodeValidationRule PostalCodeRule = new();
}
