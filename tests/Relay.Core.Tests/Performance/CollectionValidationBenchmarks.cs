using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for collection validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class CollectionValidationBenchmarks
{
    private readonly Relay.Core.Validation.Rules.UniqueValidationRule<string> _uniqueRule = ValidationRuleFields.UniqueRule;
    private readonly Relay.Core.Validation.Rules.MinCountValidationRule<string> _minCountRule = ValidationRuleFields.MinCountRule;
    private readonly Relay.Core.Validation.Rules.MaxCountValidationRule<string> _maxCountRule = ValidationRuleFields.MaxCountRule;
    private readonly Relay.Core.Validation.Rules.EmptyValidationRule<string> _emptyRule = ValidationRuleFields.EmptyRule;
    private readonly Relay.Core.Validation.Rules.EnumValidationRule<TestDataConstants.TestEnum> _enumRule = ValidationRuleFields.EnumRule;

    [Benchmark]
    public async Task UniqueValidation_Valid()
    {
        await _uniqueRule.ValidateAsync(TestDataConstants.ValidUniqueList);
    }

    [Benchmark]
    public async Task UniqueValidation_Invalid()
    {
        await _uniqueRule.ValidateAsync(TestDataConstants.InvalidUniqueList);
    }

    [Benchmark]
    public async Task MinCountValidation_Valid()
    {
        await _minCountRule.ValidateAsync(TestDataConstants.ValidMinCountList);
    }

    [Benchmark]
    public async Task MinCountValidation_Invalid()
    {
        await _minCountRule.ValidateAsync(TestDataConstants.InvalidMinCountList);
    }

    [Benchmark]
    public async Task MaxCountValidation_Valid()
    {
        await _maxCountRule.ValidateAsync(TestDataConstants.ValidMaxCountList);
    }

    [Benchmark]
    public async Task MaxCountValidation_Invalid()
    {
        await _maxCountRule.ValidateAsync(TestDataConstants.InvalidMaxCountList);
    }

    [Benchmark]
    public async Task EmptyValidation_Valid()
    {
        await _emptyRule.ValidateAsync(TestDataConstants.ValidEmptyList);
    }

    [Benchmark]
    public async Task EmptyValidation_Invalid()
    {
        await _emptyRule.ValidateAsync(TestDataConstants.InvalidEmptyList);
    }

    [Benchmark]
    public async Task EnumValidation_Valid()
    {
        await _enumRule.ValidateAsync(TestDataConstants.ValidEnum.ToString());
    }

    [Benchmark]
    public async Task EnumValidation_Invalid()
    {
        await _enumRule.ValidateAsync("InvalidEnum");
    }
}
