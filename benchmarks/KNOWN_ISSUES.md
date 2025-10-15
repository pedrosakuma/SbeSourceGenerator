# Known Issues - Phase 4

## Benchmark Project Compilation Issues

### Issue
The benchmark project and high-performance example fail to compile due to a code generation bug in the source generator.

### Error
```
error CS0266: Cannot implicitly convert type 'uint' to 'ushort'. 
An explicit conversion exists (are you missing a cast?)
```

### Location
Generated code for repeating groups with `GroupSizeEncoding` composite type.

### Root Cause
The `numInGroup` field in `GroupSizeEncoding` is defined as `uint16` (ushort) but the generated code tries to assign a `uint32` value to it.

### Impact
- Benchmark project cannot compile with schemas that have repeating groups
- High-performance example cannot compile
- This is a pre-existing bug also affecting the Binance example

### Workaround
1. Use schemas without repeating groups for benchmarks
2. Fix the code generator to properly handle `GroupSizeEncoding.numInGroup` field size
3. Ensure generated code uses correct type conversions

### Fix Required
This requires fixing the `MessagesCodeGenerator` or `GroupFieldDefinition` to:
- Correctly identify the type of `numInGroup` field from the schema
- Generate proper type casting when assigning group sizes
- Validate group size encoding composite definitions

### Status
**Not fixed** - This is outside the scope of Phase 4 (benchmarks and documentation).
Should be addressed in a separate bug fix PR.

### Files Disabled Temporarily
- `benchmarks/SbeCodeGenerator.Benchmarks/SimpleMessageBenchmarks.cs.disabled`
- `benchmarks/SbeCodeGenerator.Benchmarks/OptionalFieldBenchmarks.cs.disabled`  
- `benchmarks/SbeCodeGenerator.Benchmarks/RepeatingGroupBenchmarks.cs.disabled`
- `benchmarks/SbeCodeGenerator.Benchmarks/ComplexMessageBenchmarks.cs.disabled`
- `benchmarks/SbeCodeGenerator.Benchmarks/SpanReaderBenchmark.cs.disabled`

These can be re-enabled once the code generation bug is fixed.

### Infrastructure Status
✅ **Complete**: Benchmark infrastructure is in place
✅ **Complete**: Documentation is comprehensive
✅ **Complete**: Examples demonstrate best practices
⏳ **Blocked**: Actual benchmark execution requires bug fix

## Recommendation

1. Create a separate issue/PR to fix the `GroupSizeEncoding` code generation bug
2. Once fixed, rename `.disabled` files back to `.cs`
3. Run full benchmark suite
4. Document actual performance results

---

**Last Updated**: 2025-10-15
