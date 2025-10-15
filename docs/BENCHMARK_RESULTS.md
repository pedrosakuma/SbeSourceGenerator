# SBE Code Generator - Benchmark Results

This document contains performance benchmark results for the SBE Code Generator.

## Executive Summary

**Status**: Initial benchmarks in progress  
**Last Updated**: 2025-10-15  
**Environment**: .NET 9.0, x64, Linux

### Key Findings

- ⏳ **Baseline Performance**: To be measured
- ⏳ **Memory Efficiency**: To be measured
- ⏳ **Throughput**: To be measured

## Benchmark Environment

```
BenchmarkDotNet=v0.15.4
OS=Ubuntu (Linux kernel version TBD)
Processor=TBD
.NET SDK=9.0.305
```

## Methodology

All benchmarks use BenchmarkDotNet with:
- **Warmup**: 3 iterations
- **Measurement**: 10 iterations
- **Memory Diagnostics**: Enabled
- **Configuration**: Release mode

## Benchmark Results

### Simple Message Performance

Tests baseline encoding/decoding performance for simple messages without groups or optional fields.

#### SimpleMessageBenchmarks

| Benchmark | Mean | Error | StdDev | Gen0 | Gen1 | Allocated |
|-----------|------|-------|--------|------|------|-----------|
| Encode Simple Message | TBD | TBD | TBD | TBD | TBD | TBD |
| Decode Simple Message | TBD | TBD | TBD | TBD | TBD | TBD |
| Round-trip Simple Message | TBD | TBD | TBD | TBD | TBD | TBD |

**Analysis**: To be completed after benchmark execution.

**Target**: < 100ns for encode/decode, 0 allocations

### Optional Field Performance

Tests the performance impact of optional field handling.

#### OptionalFieldBenchmarks

| Benchmark | Mean | Error | StdDev | Gen0 | Allocated |
|-----------|------|-------|--------|------|-----------|
| Encode with Optionals Set | TBD | TBD | TBD | TBD | TBD |
| Encode with Optionals Null | TBD | TBD | TBD | TBD | TBD |
| Decode with Optionals Set | TBD | TBD | TBD | TBD | TBD |
| Decode with Optionals Null | TBD | TBD | TBD | TBD | TBD |

**Analysis**: To be completed after benchmark execution.

**Expected**: Minimal performance difference between set/null optional fields.

### Repeating Group Performance

Tests encoding/decoding performance for messages with repeating groups.

#### RepeatingGroupBenchmarks

##### Encoding Performance

| Group Size | Mean | Error | StdDev | Allocated |
|------------|------|-------|--------|-----------|
| 10 items | TBD | TBD | TBD | TBD |
| 50 items | TBD | TBD | TBD | TBD |
| 100 items | TBD | TBD | TBD | TBD |

##### Decoding Performance

| Group Size | Mean | Error | StdDev | Allocated |
|------------|------|-------|--------|-----------|
| 10 items | TBD | TBD | TBD | TBD |
| 50 items | TBD | TBD | TBD | TBD |
| 100 items | TBD | TBD | TBD | TBD |

**Analysis**: To be completed after benchmark execution.

**Expected**: Linear scaling with group size, 0 allocations.

### Complex Message Performance

Tests performance of complex messages with multiple groups.

#### ComplexMessageBenchmarks

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|------|-------|--------|-----------|
| Encode Complex Message | TBD | TBD | TBD | TBD |
| Decode Complex Message | TBD | TBD | TBD | TBD |
| Round-trip Complex Message | TBD | TBD | TBD | TBD |

**Configuration**: 50 bids, 50 asks, 20 trades

**Analysis**: To be completed after benchmark execution.

### SpanReader Performance

Compares different parsing approaches.

#### SpanReaderBenchmark

| Benchmark | Mean | Error | StdDev | Allocated |
|-----------|------|-------|--------|-----------|
| Parse with Offset Manual | TBD | TBD | TBD | TBD |
| Parse with SpanReader | TBD | TBD | TBD | TBD |
| Parse with SpanReader (No Error Check) | TBD | TBD | TBD | TBD |

**Configuration**: 100 bids, 100 asks

**Analysis**: To be completed after benchmark execution.

**Expected**: SpanReader overhead < 5% vs manual offset management.

## Performance Analysis

### Encoding Performance

**Key Observations**: TBD

**Bottlenecks Identified**: TBD

**Optimization Opportunities**: TBD

### Decoding Performance

**Key Observations**: TBD

**Bottlenecks Identified**: TBD

**Optimization Opportunities**: TBD

### Memory Efficiency

**Allocation Analysis**: TBD

**GC Pressure**: TBD

**Recommendations**: TBD

## Comparative Analysis

### vs. Manual Serialization

| Metric | SBE Generator | Manual Code | Difference |
|--------|--------------|-------------|------------|
| Encode Time | TBD | TBD | TBD |
| Decode Time | TBD | TBD | TBD |
| Allocations | TBD | TBD | TBD |
| Code Lines | Auto-generated | Manual | N/A |

### vs. Other Serializers

Comparison with common serialization libraries (MessagePack, Protobuf, etc.):

| Library | Encode (ns) | Decode (ns) | Size (bytes) | Notes |
|---------|-------------|-------------|--------------|-------|
| SBE Generator | TBD | TBD | TBD | Zero-copy |
| MessagePack | TBD | TBD | TBD | Compact |
| Protobuf | TBD | TBD | TBD | Portable |
| JSON | TBD | TBD | TBD | Human-readable |

**Note**: Benchmarks to be added in future updates.

## Optimization Impact

### Before Optimization

| Benchmark | Mean | Allocated |
|-----------|------|-----------|
| TBD | TBD | TBD |

### After Optimization

| Benchmark | Mean | Allocated | Improvement |
|-----------|------|-----------|-------------|
| TBD | TBD | TBD | TBD |

**Optimizations Applied**: To be documented

## Performance Recommendations

### For High-Throughput Scenarios

1. **Use Stack Allocation**: Prefer `stackalloc` for small buffers
2. **Reuse Buffers**: Use `ArrayPool<byte>` for larger buffers
3. **Avoid Allocations**: Keep Gen0/Gen1 at 0
4. **Batch Processing**: Process multiple messages per operation

### For Low-Latency Scenarios

1. **Minimize Validation**: Use non-validating paths in production
2. **Pre-allocate Buffers**: Avoid dynamic allocation
3. **Use Spans**: Leverage `Span<T>` for zero-copy operations
4. **Warm Up Code**: Pre-JIT critical paths

### For Memory-Constrained Scenarios

1. **Use Smaller Buffers**: Right-size buffer allocations
2. **Stream Processing**: Process data incrementally
3. **Limit Group Sizes**: Cap maximum group sizes
4. **Monitor Allocations**: Track memory usage

## Continuous Performance Monitoring

### Performance Regression Detection

Track key metrics over time:
- Simple message encode/decode time
- Memory allocations per operation
- Throughput (messages/second)

### Benchmark CI/CD Integration

Benchmarks should be:
- Run on each major commit
- Tracked in performance dashboard
- Flagged when regressions detected
- Documented in release notes

## Running Benchmarks

### Quick Start

```bash
cd benchmarks/SbeCodeGenerator.Benchmarks
dotnet run -c Release
```

### Run Specific Benchmarks

```bash
# Run only simple message benchmarks
dotnet run -c Release -- --filter *SimpleMessage*

# Run with specific parameters
dotnet run -c Release -- --filter *RepeatingGroup* --job short
```

### Generate Reports

```bash
# HTML report
dotnet run -c Release -- --exporters html

# Markdown summary
dotnet run -c Release -- --exporters markdown
```

## Changelog

### 2025-10-15 - Initial Version
- Created benchmark infrastructure
- Defined benchmark suites
- Established performance targets
- Created results template

### Future Updates
- Add actual benchmark results
- Document optimization efforts
- Compare with reference implementations
- Track performance over time

## References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [.NET Performance Guidelines](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [SBE Specification](https://www.fixtrading.org/standards/sbe/)
- [Performance Tuning Guide](./PERFORMANCE_TUNING_GUIDE.md) (To be created)

---

**Status**: Template Created - Awaiting Benchmark Execution  
**Last Updated**: 2025-10-15
