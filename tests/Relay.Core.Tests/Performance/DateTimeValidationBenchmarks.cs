using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for date/time validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class DateTimeValidationBenchmarks
{
    private readonly Relay.Core.Validation.Rules.FutureValidationRule _futureRule = ValidationRuleFields.FutureRule;
    private readonly Relay.Core.Validation.Rules.PastValidationRule _pastRule = ValidationRuleFields.PastRule;
    private readonly Relay.Core.Validation.Rules.TodayValidationRule _todayRule = ValidationRuleFields.TodayRule;

    [Benchmark]
    public async Task FutureValidation_Valid()
    {
        await _futureRule.ValidateAsync(TestDataConstants.ValidFutureDate);
    }

    [Benchmark]
    public async Task FutureValidation_Invalid()
    {
        await _futureRule.ValidateAsync(TestDataConstants.InvalidFutureDate);
    }

    [Benchmark]
    public async Task PastValidation_Valid()
    {
        await _pastRule.ValidateAsync(TestDataConstants.ValidPastDate);
    }

    [Benchmark]
    public async Task PastValidation_Invalid()
    {
        await _pastRule.ValidateAsync(TestDataConstants.InvalidPastDate);
    }

    [Benchmark]
    public async Task TodayValidation_Valid()
    {
        await _todayRule.ValidateAsync(TestDataConstants.ValidTodayDate);
    }

    [Benchmark]
    public async Task TodayValidation_Invalid()
    {
        await _todayRule.ValidateAsync(TestDataConstants.InvalidTodayDate);
    }
}