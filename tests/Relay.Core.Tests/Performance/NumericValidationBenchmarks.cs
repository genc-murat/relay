using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for numeric validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class NumericValidationBenchmarks
{
    private readonly Relay.Core.Validation.Rules.PositiveValidationRule<int> _positiveRule = ValidationRuleFields.PositiveRule;
    private readonly Relay.Core.Validation.Rules.NegativeValidationRule<int> _negativeRule = ValidationRuleFields.NegativeRule;
    private readonly Relay.Core.Validation.Rules.NonZeroValidationRule<int> _nonZeroRule = ValidationRuleFields.NonZeroRule;
    private readonly Relay.Core.Validation.Rules.EvenValidationRule _evenRule = ValidationRuleFields.EvenRule;
    private readonly Relay.Core.Validation.Rules.OddValidationRule _oddRule = ValidationRuleFields.OddRule;
    private readonly Relay.Core.Validation.Rules.ZeroValidationRule<int> _zeroRule = ValidationRuleFields.ZeroRule;
    private readonly Relay.Core.Validation.Rules.BetweenValidationRule<int> _betweenRule = ValidationRuleFields.BetweenRule;

    [Benchmark]
    public async Task PositiveValidation_Valid()
    {
        await _positiveRule.ValidateAsync(TestDataConstants.ValidPositive);
    }

    [Benchmark]
    public async Task PositiveValidation_Invalid()
    {
        await _positiveRule.ValidateAsync(TestDataConstants.InvalidPositive);
    }

    [Benchmark]
    public async Task NegativeValidation_Valid()
    {
        await _negativeRule.ValidateAsync(TestDataConstants.ValidNegative);
    }

    [Benchmark]
    public async Task NegativeValidation_Invalid()
    {
        await _negativeRule.ValidateAsync(TestDataConstants.InvalidNegative);
    }

    [Benchmark]
    public async Task NonZeroValidation_Valid()
    {
        await _nonZeroRule.ValidateAsync(TestDataConstants.ValidNonZero);
    }

    [Benchmark]
    public async Task NonZeroValidation_Invalid()
    {
        await _nonZeroRule.ValidateAsync(TestDataConstants.InvalidNonZero);
    }

    [Benchmark]
    public async Task EvenValidation_Valid()
    {
        await _evenRule.ValidateAsync(TestDataConstants.ValidEven);
    }

    [Benchmark]
    public async Task EvenValidation_Invalid()
    {
        await _evenRule.ValidateAsync(TestDataConstants.InvalidEven);
    }

    [Benchmark]
    public async Task OddValidation_Valid()
    {
        await _oddRule.ValidateAsync(TestDataConstants.ValidOdd);
    }

    [Benchmark]
    public async Task OddValidation_Invalid()
    {
        await _oddRule.ValidateAsync(TestDataConstants.InvalidOdd);
    }

    [Benchmark]
    public async Task ZeroValidation_Valid()
    {
        await _zeroRule.ValidateAsync(TestDataConstants.ValidZero);
    }

    [Benchmark]
    public async Task ZeroValidation_Invalid()
    {
        await _zeroRule.ValidateAsync(TestDataConstants.InvalidZero);
    }

    [Benchmark]
    public async Task BetweenValidation_Valid()
    {
        await _betweenRule.ValidateAsync(TestDataConstants.ValidBetween);
    }

    [Benchmark]
    public async Task BetweenValidation_Invalid()
    {
        await _betweenRule.ValidateAsync(TestDataConstants.InvalidBetween);
    }
}