using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for basic validation rules (required, not empty, range, etc.)
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class BasicValidationBenchmarks
{
    private readonly Relay.Core.Validation.Rules.RequiredValidationRule<string> _requiredRule = ValidationRuleFields.RequiredRule;
    private readonly Relay.Core.Validation.Rules.NotEmptyValidationRule _notEmptyRule = ValidationRuleFields.NotEmptyRule;
    private readonly Relay.Core.Validation.Rules.RangeValidationRule<int> _rangeRule = ValidationRuleFields.RangeRule;
    private readonly Relay.Core.Validation.Rules.MinLengthValidationRule _minLengthRule = ValidationRuleFields.MinLengthRule;
    private readonly Relay.Core.Validation.Rules.MaxLengthValidationRule _maxLengthRule = ValidationRuleFields.MaxLengthRule;
    private readonly Relay.Core.Validation.Rules.ExactLengthValidationRule _exactLengthRule = ValidationRuleFields.ExactLengthRule;
    private readonly Relay.Core.Validation.Rules.RegexValidationRule _regexRule = ValidationRuleFields.RegexRule;

    [Benchmark]
    public async Task RequiredValidation_Valid()
    {
        await _requiredRule.ValidateAsync(TestDataConstants.ValidRequired);
    }

    [Benchmark]
    public async Task NotEmptyValidation_Valid()
    {
        await _notEmptyRule.ValidateAsync(TestDataConstants.ValidNotEmpty);
    }

    [Benchmark]
    public async Task NotEmptyValidation_Invalid()
    {
        await _notEmptyRule.ValidateAsync(TestDataConstants.InvalidNotEmpty);
    }

    [Benchmark]
    public async Task RangeValidation_Valid()
    {
        await _rangeRule.ValidateAsync(TestDataConstants.ValidRange);
    }

    [Benchmark]
    public async Task RangeValidation_Invalid()
    {
        await _rangeRule.ValidateAsync(TestDataConstants.InvalidRange);
    }

    [Benchmark]
    public async Task MinLengthValidation_Valid()
    {
        await _minLengthRule.ValidateAsync(TestDataConstants.ValidMinLength);
    }

    [Benchmark]
    public async Task MinLengthValidation_Invalid()
    {
        await _minLengthRule.ValidateAsync(TestDataConstants.InvalidMinLength);
    }

    [Benchmark]
    public async Task MaxLengthValidation_Valid()
    {
        await _maxLengthRule.ValidateAsync(TestDataConstants.ValidMaxLength);
    }

    [Benchmark]
    public async Task MaxLengthValidation_Invalid()
    {
        await _maxLengthRule.ValidateAsync(TestDataConstants.InvalidMaxLength);
    }

    [Benchmark]
    public async Task ExactLengthValidation_Valid()
    {
        await _exactLengthRule.ValidateAsync(TestDataConstants.ValidExactLength);
    }

    [Benchmark]
    public async Task ExactLengthValidation_Invalid()
    {
        await _exactLengthRule.ValidateAsync(TestDataConstants.InvalidExactLength);
    }

    [Benchmark]
    public async Task RegexValidation_Valid()
    {
        await _regexRule.ValidateAsync(TestDataConstants.ValidRegex);
    }

    [Benchmark]
    public async Task RegexValidation_Invalid()
    {
        await _regexRule.ValidateAsync(TestDataConstants.InvalidRegex);
    }
}