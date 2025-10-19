using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for string validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class StringValidationBenchmarks
{
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
    private readonly Relay.Core.Validation.Rules.LengthValidationRule _lengthRule = ValidationRuleFields.LengthRule;

    [Benchmark]
    public async Task IsEmptyValidation_Valid()
    {
        await _isEmptyRule.ValidateAsync(TestDataConstants.ValidIsEmpty);
    }

    [Benchmark]
    public async Task IsEmptyValidation_Invalid()
    {
        await _isEmptyRule.ValidateAsync(TestDataConstants.InvalidIsEmpty);
    }

    [Benchmark]
    public async Task HasDigitsValidation_Valid()
    {
        await _hasDigitsRule.ValidateAsync(TestDataConstants.ValidHasDigits);
    }

    [Benchmark]
    public async Task HasDigitsValidation_Invalid()
    {
        await _hasDigitsRule.ValidateAsync(TestDataConstants.InvalidHasDigits);
    }

    [Benchmark]
    public async Task HasLettersValidation_Valid()
    {
        await _hasLettersRule.ValidateAsync(TestDataConstants.ValidHasLetters);
    }

    [Benchmark]
    public async Task HasLettersValidation_Invalid()
    {
        await _hasLettersRule.ValidateAsync(TestDataConstants.InvalidHasLetters);
    }

    [Benchmark]
    public async Task IsLowerCaseValidation_Valid()
    {
        await _isLowerCaseRule.ValidateAsync(TestDataConstants.ValidIsLowerCase);
    }

    [Benchmark]
    public async Task IsLowerCaseValidation_Invalid()
    {
        await _isLowerCaseRule.ValidateAsync(TestDataConstants.InvalidIsLowerCase);
    }

    [Benchmark]
    public async Task IsUpperCaseValidation_Valid()
    {
        await _isUpperCaseRule.ValidateAsync(TestDataConstants.ValidIsUpperCase);
    }

    [Benchmark]
    public async Task IsUpperCaseValidation_Invalid()
    {
        await _isUpperCaseRule.ValidateAsync(TestDataConstants.InvalidIsUpperCase);
    }

    [Benchmark]
    public async Task StartsWithValidation_Valid()
    {
        await _startsWithRule.ValidateAsync(TestDataConstants.ValidStartsWith);
    }

    [Benchmark]
    public async Task StartsWithValidation_Invalid()
    {
        await _startsWithRule.ValidateAsync(TestDataConstants.InvalidStartsWith);
    }

    [Benchmark]
    public async Task EndsWithValidation_Valid()
    {
        await _endsWithRule.ValidateAsync(TestDataConstants.ValidEndsWith);
    }

    [Benchmark]
    public async Task EndsWithValidation_Invalid()
    {
        await _endsWithRule.ValidateAsync(TestDataConstants.InvalidEndsWith);
    }

    [Benchmark]
    public async Task ContainsValidation_Valid()
    {
        await _containsRule.ValidateAsync(TestDataConstants.ValidContains);
    }

    [Benchmark]
    public async Task ContainsValidation_Invalid()
    {
        await _containsRule.ValidateAsync(TestDataConstants.InvalidContains);
    }

    [Benchmark]
    public async Task IsEqualValidation_Valid()
    {
        await _equalRule.ValidateAsync(TestDataConstants.ValidEqual);
    }

    [Benchmark]
    public async Task IsEqualValidation_Invalid()
    {
        await _equalRule.ValidateAsync(TestDataConstants.InvalidEqual);
    }

    [Benchmark]
    public async Task NotEqualValidation_Valid()
    {
        await _notEqualRule.ValidateAsync(TestDataConstants.ValidNotEqual);
    }

    [Benchmark]
    public async Task NotEqualValidation_Invalid()
    {
        await _notEqualRule.ValidateAsync(TestDataConstants.InvalidNotEqual);
    }

    [Benchmark]
    public async Task IsInValidation_Valid()
    {
        await _isInRule.ValidateAsync(TestDataConstants.ValidIsIn);
    }

    [Benchmark]
    public async Task IsInValidation_Invalid()
    {
        await _isInRule.ValidateAsync(TestDataConstants.InvalidIsIn);
    }

    [Benchmark]
    public async Task NotInValidation_Valid()
    {
        await _notInRule.ValidateAsync(TestDataConstants.ValidNotIn);
    }

    [Benchmark]
    public async Task NotInValidation_Invalid()
    {
        await _notInRule.ValidateAsync(TestDataConstants.InvalidNotIn);
    }

    [Benchmark]
    public async Task LengthValidation_Valid()
    {
        await _lengthRule.ValidateAsync(TestDataConstants.ValidLength);
    }

    [Benchmark]
    public async Task LengthValidation_Invalid()
    {
        await _lengthRule.ValidateAsync(TestDataConstants.InvalidLength);
    }
}