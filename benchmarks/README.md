# SBE Code Generator Benchmarks

This directory contains performance benchmarks for the SBE Code Generator using BenchmarkDotNet.

## Structure

- **SbeCodeGenerator.Benchmarks/** - BenchmarkDotNet project with comprehensive performance tests
- **Schemas/** - SBE XML schemas used for benchmark tests

## Running Benchmarks

### Current Status

⚠️ **Note**: Some benchmarks are temporarily disabled due to a pre-existing code generation bug with `GroupSizeEncoding`. See [KNOWN_ISSUES.md](./KNOWN_ISSUES.md) for details.

The benchmark infrastructure is complete and ready. Once the code generation bug is fixed, the full benchmark suite can be executed.

### Run All Benchmarks

```bash
cd benchmarks/SbeCodeGenerator.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmark

```bash
cd benchmarks/SbeCodeGenerator.Benchmarks
dotnet run -c Release -- --filter *SimpleMessage*
```

### Generate Reports

Benchmarks automatically generate detailed reports in `BenchmarkDotNet.Artifacts/results/`:
- HTML reports
- CSV data
- Markdown summaries

## Available Benchmarks

### SimpleMessageBenchmarks
Tests baseline performance for simple message encoding/decoding.
- `EncodeSimpleMessage` - Encoding performance
- `DecodeSimpleMessage` - Decoding performance  
- `RoundTripSimpleMessage` - Full encode/decode cycle with verification

### OptionalFieldBenchmarks
Tests performance impact of optional field handling.
- `EncodeWithOptionals` - Encoding with optional fields set
- `EncodeWithoutOptionals` - Encoding with optional fields null
- `DecodeWithOptionals` - Decoding with optional fields set
- `DecodeWithoutOptionals` - Decoding with optional fields null

### RepeatingGroupBenchmarks
Tests performance of repeating group encoding/decoding with varying group sizes (10, 50, 100 items).
- `EncodeWithGroups` - Encoding messages with repeating groups
- `DecodeWithGroups` - Decoding messages with repeating groups

### GroupForeachVsCallbackBenchmarks
Compares the v1.5.0 foreach-style group enumerator (#156) against the original `ReadGroups` callback API.
- `Decode_Callback` - Baseline: `ReadGroups` with capturing lambdas (closure allocation per call)
- `Decode_Foreach` - Zero-alloc ref struct enumerator
- `Decode_Foreach_EarlyBreak` - Demonstrates break-out savings vs callbacks (which always run to completion)

Reference results (AMD EPYC 7763, .NET 9, GroupSize=100):

| Method            | Mean      | Allocated |
|-------------------|----------:|----------:|
| Callback          | 999 ns    | 152 B     |
| Foreach           | 184 ns    | 0 B       |
| Foreach + break   |   3.3 ns  | 0 B       |

Foreach is ~5× faster on full iteration and eliminates the 152 B closure allocation. Early break is essentially free because each group property is an O(1) skip.

### ComplexMessageBenchmarks
Tests performance of complex messages with multiple groups (bids, asks, trades).
- `EncodeComplexMessage` - Encoding complex multi-group messages
- `DecodeComplexMessage` - Decoding complex multi-group messages
- `RoundTripComplexMessage` - Full cycle with verification

### SpanReaderBenchmark
Compares performance of different parsing approaches.
- `ParseWithOffsetManual` - Manual offset management (baseline)
- `ParseWithSpanReader` - Using SpanReader with error checking
- `ParseWithSpanReaderNoErrorCheck` - SpanReader without error checks

## Benchmark Schema

The `benchmark-schema.xml` defines various message types for performance testing:
- **SimpleOrder** - Basic message with primitive fields
- **OrderWithOptionals** - Message with optional fields
- **MarketData** - Message with two repeating groups (bids/asks)
- **ComplexMarketData** - Complex message with three repeating groups

## Performance Targets

Based on Phase 4 goals:

- **Simple Message Encoding**: < 100ns per message
- **Simple Message Decoding**: < 100ns per message
- **Zero Allocations**: All encoding/decoding operations should avoid heap allocations
- **Throughput**: Support 1M+ messages/second

## Interpreting Results

BenchmarkDotNet provides:
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation of all measurements
- **Gen0/Gen1**: Garbage collection metrics
- **Allocated**: Total memory allocated per operation

Target: 0 allocations (Gen0/Gen1 = "-", Allocated = "-")

## Continuous Benchmarking

Benchmark results should be:
1. Tracked over time to catch performance regressions
3. Compared against previous versions
4. Used to validate optimization efforts

## Contributing

When adding new benchmarks:
1. Create focused benchmark classes testing specific scenarios
2. Use `[MemoryDiagnoser]` to track allocations
3. Use appropriate `[Params]` for parameterized tests
4. Document expected performance characteristics
5. Update this README with new benchmark descriptions
