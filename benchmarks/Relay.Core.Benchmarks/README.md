# Relay.Core Transaction Benchmarks

This project contains performance benchmarks for the Relay.Core transaction system using BenchmarkDotNet.

## Running Benchmarks

### Run All Benchmarks
```bash
dotnet run -c Release --project benchmarks/Relay.Core.Benchmarks
```

### Run Specific Benchmark
```bash
dotnet run -c Release --project benchmarks/Relay.Core.Benchmarks --filter "*SimpleTransactionBenchmarks*"
```

### Run with Memory Profiler
```bash
dotnet run -c Release --project benchmarks/Relay.Core.Benchmarks --filter "*SimpleTransactionBenchmarks*" --memory
```

## Benchmark Categories

### SimpleTransactionBenchmarks
Measures performance of basic transaction scenarios:
- Simple transaction with ReadCommitted isolation
- Simple transaction with Serializable isolation
- Read-only transactions
- Multiple sequential transactions

### NestedTransactionBenchmarks
Measures performance of nested transaction scenarios:
- Two-level nesting
- Three-level nesting
- Five-level nesting

### SavepointBenchmarks
Measures performance of savepoint operations:
- Creating single savepoint
- Creating multiple savepoints
- Creating and rolling back to savepoint
- Creating multiple savepoints and rolling back

### RetryBenchmarks
Measures performance of transaction retry scenarios:
- No retry (success on first attempt)
- Linear retry with one failure
- Exponential backoff retry with one failure
- Linear retry with two failures
- Exponential backoff retry with two failures

## Interpreting Results

BenchmarkDotNet will output:
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Gen0/Gen1/Gen2**: Garbage collection counts per 1000 operations
- **Allocated**: Total memory allocated per operation

## Baseline Comparisons

Each benchmark category has a baseline marked with `[Benchmark(Baseline = true)]`. Other benchmarks in the same category show their performance relative to the baseline.

## Output

Results are exported to:
- Console output
- Markdown files in `BenchmarkDotNet.Artifacts/results/`
- HTML reports in `BenchmarkDotNet.Artifacts/results/`

## Notes

- Benchmarks use a lightweight `BenchmarkUnitOfWork` to minimize database overhead
- All benchmarks run in Release configuration for accurate performance measurement
- Memory diagnostics are enabled to track allocations
- Results may vary based on hardware and system load
