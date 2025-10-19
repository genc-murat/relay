using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using Relay.Core.Validation.Rules;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for business validation rules
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
public class BusinessValidationBenchmarks
{
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