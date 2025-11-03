# Transaction System Benchmarks Summary

## Overview

This benchmark suite provides comprehensive performance testing for the Relay.Core transaction system. The benchmarks are designed to measure performance across different transaction scenarios and identify optimization opportunities.

## Benchmark Categories

### 1. SimpleTransactionBenchmarks

**Purpose**: Measure baseline transaction performance for common scenarios.

**Scenarios**:
- `SimpleTransaction_ReadCommitted` (Baseline) - Standard transaction with ReadCommitted isolation
- `SimpleTransaction_Serializable` - Transaction with Serializable isolation (highest isolation level)
- `SimpleTransaction_ReadOnly` - Read-only transaction optimization
- `MultipleSequentialTransactions` - 10 sequential transactions to measure throughput

**Expected Results**:
- ReadCommitted should be the fastest (baseline)
- Serializable may be slightly slower due to stricter locking
- ReadOnly should be comparable or faster than ReadCommitted
- Sequential transactions should scale linearly

**Key Metrics**:
- Mean execution time
- Memory allocations per operation
- GC collections

### 2. NestedTransactionBenchmarks

**Purpose**: Measure performance impact of transaction nesting.

**Scenarios**:
- `NestedTransaction_TwoLevels` (Baseline) - Outer + 1 inner transaction
- `NestedTransaction_ThreeLevels` - Outer + 2 nested transactions
- `NestedTransaction_FiveLevels` - Outer + 4 nested transactions

**Expected Results**:
- Performance should degrade linearly with nesting depth
- Context propagation overhead should be minimal
- Memory allocations should increase with nesting level

**Key Metrics**:
- Overhead per nesting level
- AsyncLocal access cost
- Context creation overhead

### 3. SavepointBenchmarks

**Purpose**: Measure savepoint operation performance.

**Scenarios**:
- `CreateSingleSavepoint` (Baseline) - Create one savepoint
- `CreateMultipleSavepoints` - Create 5 savepoints
- `CreateAndRollbackSavepoint` - Create and rollback to savepoint
- `CreateMultipleAndRollback` - Create 5 savepoints and rollback to first

**Expected Results**:
- Savepoint creation should be fast (< 1ms)
- Multiple savepoints should scale linearly
- Rollback should have minimal overhead
- Dictionary operations should be efficient

**Key Metrics**:
- Savepoint creation time
- Rollback operation time
- Memory overhead per savepoint

### 4. RetryBenchmarks

**Purpose**: Measure retry logic performance and overhead.

**Scenarios**:
- `NoRetry_Success` (Baseline) - Successful operation without retry
- `LinearRetry_OneFailure` - One retry with linear backoff
- `ExponentialRetry_OneFailure` - One retry with exponential backoff
- `LinearRetry_TwoFailures` - Two retries with linear backoff
- `ExponentialRetry_TwoFailures` - Two retries with exponential backoff

**Expected Results**:
- No retry should be fastest (baseline)
- Linear retry should be faster than exponential for few retries
- Exponential backoff overhead should be minimal
- Retry count should correlate with execution time

**Key Metrics**:
- Retry overhead per attempt
- Delay calculation cost
- Exception handling overhead

## Running the Benchmarks

### Full Suite

```bash
cd benchmarks/Relay.Core.Benchmarks
dotnet run -c Release
```

### Specific Category

```bash
dotnet run -c Release --filter "*SimpleTransactionBenchmarks*"
```

### With Memory Profiling

```bash
dotnet run -c Release --filter "*" --memory
```

### Export Results

```bash
dotnet run -c Release --exporters json markdown html
```

## Interpreting Results

### BenchmarkDotNet Output

```
| Method                          | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------- |----------:|---------:|---------:|-------:|----------:|
| SimpleTransaction_ReadCommitted | 1.234 ms  | 0.012 ms | 0.011 ms | 0.0020 |     128 B |
```

**Columns**:
- **Method**: Benchmark name
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: GC collections per 1000 operations
- **Allocated**: Total memory allocated per operation

### Performance Targets

Based on design requirements:

| Operation | Target | Acceptable | Needs Optimization |
|-----------|--------|------------|-------------------|
| Simple Transaction | < 1ms | 1-2ms | > 2ms |
| Nested Transaction (2 levels) | < 2ms | 2-4ms | > 4ms |
| Savepoint Creation | < 0.5ms | 0.5-1ms | > 1ms |
| Retry Attempt | < 10ms | 10-20ms | > 20ms |

### Memory Targets

| Operation | Target | Acceptable | Needs Optimization |
|-----------|--------|------------|-------------------|
| Simple Transaction | < 200B | 200-500B | > 500B |
| Nested Transaction | < 500B | 500-1KB | > 1KB |
| Savepoint | < 100B | 100-200B | > 200B |

## Optimization Workflow

1. **Establish Baseline**
   ```bash
   dotnet run -c Release > baseline.txt
   ```

2. **Identify Bottlenecks**
   - Review benchmark results
   - Check memory allocations
   - Look for GC pressure (Gen2 collections)

3. **Apply Optimizations**
   - See OPTIMIZATION_GUIDE.md for strategies
   - Focus on hot paths first
   - Measure each change

4. **Verify Improvements**
   ```bash
   dotnet run -c Release > optimized.txt
   # Compare with baseline
   ```

5. **Regression Testing**
   - Ensure no performance regression in other scenarios
   - Verify all tests still pass
   - Check memory usage hasn't increased

## Continuous Performance Monitoring

### CI/CD Integration

Add benchmark runs to CI pipeline:

```yaml
- name: Run Benchmarks
  run: |
    cd benchmarks/Relay.Core.Benchmarks
    dotnet run -c Release --filter "*" --exporters json
    
- name: Compare with Baseline
  run: |
    # Compare current results with baseline
    # Fail if regression > 10%
```

### Performance Regression Detection

Set up automated alerts for:
- Mean execution time increase > 10%
- Memory allocation increase > 20%
- New Gen2 collections appearing

## Benchmark Maintenance

### When to Update Benchmarks

- After major feature additions
- When changing transaction core logic
- After performance optimizations
- When adding new transaction features

### Benchmark Best Practices

1. **Isolation**: Each benchmark should test one specific scenario
2. **Repeatability**: Results should be consistent across runs
3. **Realistic**: Use realistic data and scenarios
4. **Documented**: Clearly document what each benchmark measures
5. **Maintained**: Keep benchmarks up-to-date with code changes

## Known Limitations

### BenchmarkUnitOfWork

The benchmarks use a lightweight `BenchmarkUnitOfWork` that:
- Minimizes database overhead
- Doesn't perform actual I/O
- Focuses on transaction system overhead

This means:
- Results don't include database latency
- Actual production performance will be slower
- Relative comparisons are still valid

### Environment Factors

Benchmark results can vary based on:
- CPU speed and cores
- Available memory
- Background processes
- .NET runtime version
- Operating system

Always run benchmarks on the same machine for comparison.

## Troubleshooting

### Inconsistent Results

If results vary significantly between runs:
- Close other applications
- Disable CPU throttling
- Run multiple iterations
- Check for background processes

### High Memory Allocations

If allocations are higher than expected:
- Check for boxing
- Look for string allocations
- Review LINQ usage
- Profile with dotMemory

### Slow Benchmarks

If benchmarks take too long:
- Reduce iteration count (for development)
- Run specific categories only
- Use `--filter` to target specific benchmarks

## Resources

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Best Practices](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [Optimization Guide](./OPTIMIZATION_GUIDE.md)
- [Transaction System Documentation](../../.kiro/specs/relay-transactions-enhancement/USAGE_GUIDE.md)

## Contributing

When adding new benchmarks:

1. Follow existing naming conventions
2. Add clear documentation
3. Include expected results
4. Update this summary document
5. Verify benchmarks run successfully
6. Add to appropriate category

## Conclusion

These benchmarks provide a comprehensive view of transaction system performance. Use them to:
- Establish performance baselines
- Identify optimization opportunities
- Prevent performance regressions
- Validate optimization efforts

Regular benchmark runs help maintain high performance as the codebase evolves.
