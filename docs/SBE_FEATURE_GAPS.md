# SBE Feature Gaps - Quick Reference

**Last Updated**: 2025-10-16  
**Purpose**: Quick reference for feature gaps and implementation priorities

## Critical Gaps (Must Fix for v1.0)

### 1. Variable-Length Data (varData) 🔴

**Status**: ❌ Not Implemented  
**Priority**: P0 - Critical  
**Effort**: 2-3 weeks  
**Blocking**: v1.0 release

**What's Missing**:
- `<data>` element support in schema
- VarString8, VarString16, VarString32 types
- VarData for binary blobs
- Length-prefixed encoding/decoding
- UTF-8 string support

**Use Cases Blocked**:
- Variable-length strings (symbols, names, descriptions)
- Binary data (certificates, signatures, images)
- Text messages
- Dynamic-size fields

**Implementation Plan**:
1. Create `SchemaDataDto` for parsing `<data>` elements
2. Update `SchemaParser.ParseMessage()` to extract data fields
3. Implement `VariableLengthDataFieldDefinition` generator
4. Generate length-prefixed field accessors
5. Handle UTF-8 string encoding/decoding
6. Support binary blob data
7. Add buffer bounds checking
8. Write comprehensive tests

**Files to Create/Modify**:
- `Schema/SchemaDataDto.cs` (new)
- `Generators/Fields/VariableLengthDataFieldDefinition.cs` (new)
- `Generators/MessagesCodeGenerator.cs` (modify)
- `Tests/VariableLengthDataTests.cs` (new)

**References**:
- [SBE Spec - Variable-Length Data](https://github.com/real-logic/simple-binary-encoding/wiki/FIX-SBE-XML-Primer#variable-length-data)
- Real Logic implementation: `sbe-tool/src/main/java/uk/co/real_logic/sbe/generation/java/JavaGenerator.java`

---

## Important Gaps (Should Fix for Competitiveness)

### 2. Nested Groups 🟡

**Status**: ❌ Not Implemented  
**Priority**: P1 - Important  
**Effort**: 2-3 weeks  
**Blocking**: Feature parity with Real Logic

**What's Missing**:
- Groups within groups (multi-level nesting)
- Recursive group handling
- Per-level dimension encoding

**Use Cases Blocked**:
- Complex order books (bids → orders → fills)
- Portfolio structures (portfolios → positions → trades)
- Multi-level market data

**Implementation Plan**:
1. Design recursive group structure
2. Update `SchemaGroupDto` to support nested groups
3. Modify `GroupDefinition.cs` for recursive generation
4. Implement proper offset calculation for nested structures
5. Generate encoder/decoder methods for each level
6. Add comprehensive tests for multi-level scenarios

**Files to Modify**:
- `Schema/SchemaGroupDto.cs`
- `Generators/Types/GroupDefinition.cs`
- `Generators/MessagesCodeGenerator.cs`
- `Tests/NestedGroupsTests.cs` (new)

**Example Schema**:
```xml
<message name="OrderBook" id="20">
    <field name="instrumentId" id="1" type="uint64"/>
    <group name="Bids" id="2">
        <field name="price" id="10" type="int64"/>
        <!-- Nested group -->
        <group name="Orders" id="11">
            <field name="orderId" id="20" type="uint64"/>
            <field name="quantity" id="21" type="int64"/>
        </group>
    </group>
</message>
```

---

### 3. Performance Benchmarks 🟡

**Status**: ⚠️ Not Published  
**Priority**: P1 - Important  
**Effort**: 1 week  
**Blocking**: Performance validation claims

**What's Missing**:
- Formal performance benchmarks (BenchmarkDotNet)
- Comparative benchmarks vs Real Logic C# implementation
- Published results and analysis
- Performance optimization based on benchmarks

**Metrics to Measure**:
- Encode/decode latency (nanoseconds)
- Throughput (messages/second)
- Memory allocation (zero allocation validation)
- Cache efficiency
- Comparison with Real Logic SBE

**Implementation Plan**:
1. Set up BenchmarkDotNet infrastructure (✅ Done in benchmarks/)
2. Create benchmark suite for common scenarios
3. Implement comparative benchmarks (this project vs Real Logic)
4. Run on representative hardware
5. Analyze results and optimize hot paths
6. Document and publish results

**Files to Create/Modify**:
- `benchmarks/SbeCodeGenerator.Benchmarks/` (exists, needs scenarios)
- `docs/BENCHMARK_RESULTS.md` (exists, needs data)
- `docs/PERFORMANCE_TUNING_GUIDE.md` (exists, needs validation)

---

## Enhancement Opportunities (Nice to Have)

### 4. FlyWeight Pattern Support 🟢

**Status**: ⚠️ Partial  
**Priority**: P3 - Low  
**Effort**: 1 week

**What It Is**:
- Reusable decoder objects that wrap buffers
- Avoid allocation of message structs
- Direct buffer reading without copying

**Benefits**:
- Reduced GC pressure
- Lower latency
- Better for high-frequency scenarios

**Decision**: Evaluate if needed - current approach with Span<T> already provides zero-copy

---

### 5. Extended varData Types 🟢

**Status**: ❌ Not Implemented (depends on #1)  
**Priority**: P2 - Medium  
**Effort**: 1 week (after varData base)

**What's Missing**:
- VarString16 (uint16 length prefix, up to 64KB)
- VarString32 (uint32 length prefix, up to 4GB)
- VarAsciiString variants

**Use Cases**:
- Large text fields (long descriptions, notes)
- Binary blobs > 255 bytes
- Legacy system integration

**Implementation**: Extend varData base implementation with different length prefixes

---

### 6. JSON Converters 🟢

**Status**: ❌ Not Implemented  
**Priority**: P2 - Medium  
**Effort**: 1-2 weeks

**What to Add**:
- System.Text.Json converters for generated types
- JsonConverter<T> for messages
- Pretty-printing for debugging
- JSON schema generation from SBE schema

**Benefits**:
- Easier debugging and logging
- REST API integration
- Human-readable message inspection
- Testing and development productivity

**Example**:
```csharp
var message = new TradeMessage { ... };
var json = JsonSerializer.Serialize(message); // Auto-converts
```

---

### 7. Custom Encoding/Decoding Hooks 🟢

**Status**: ❌ Not Implemented  
**Priority**: P3 - Low  
**Effort**: 2 weeks

**What to Add**:
- Partial classes for user customization
- Pre/post encoding hooks
- Custom field transformations
- Pluggable serialization strategies

**Use Cases**:
- Custom compression
- Encryption/decryption
- Application-specific transformations
- Legacy format compatibility

---

## Comparison Matrix

| Feature | Status | Priority | Effort | v1.0? |
|---------|--------|----------|--------|-------|
| **Variable-Length Data** | ❌ | 🔴 P0 | 2-3 weeks | ✅ Yes |
| **Nested Groups** | ❌ | 🟡 P1 | 2-3 weeks | ⚠️ Maybe |
| **Performance Benchmarks** | ⚠️ | 🟡 P1 | 1 week | ✅ Yes |
| **FlyWeight Pattern** | ⚠️ | 🟢 P3 | 1 week | ❌ No |
| **Extended varData** | ❌ | 🟢 P2 | 1 week | ❌ No |
| **JSON Converters** | ❌ | 🟢 P2 | 1-2 weeks | ❌ No |
| **Custom Hooks** | ❌ | 🟢 P3 | 2 weeks | ❌ No |

**Total Effort for v1.0**: 3-4 weeks (varData + benchmarks)  
**Total Effort for v1.1**: 5-7 weeks (add nested groups + extended varData)

---

## Action Plan

### Phase 1: Critical Features (Target: v0.9.0 - 3 weeks)

**Week 1-2**: Variable-Length Data (VarString8 + VarData)
- [ ] Schema parsing for `<data>` elements
- [ ] VarString8 code generation
- [ ] VarData code generation
- [ ] UTF-8 encoding support
- [ ] Unit tests
- [ ] Integration tests

**Week 3**: Performance Benchmarks
- [ ] Benchmark scenarios
- [ ] Comparative benchmarks
- [ ] Performance analysis
- [ ] Document results
- [ ] Optimization pass

### Phase 2: Feature Parity (Target: v1.0.0 - 3 weeks)

**Week 4-5**: Nested Groups
- [ ] Schema support for nested groups
- [ ] Recursive code generation
- [ ] Offset calculation
- [ ] Encoder/decoder methods
- [ ] Tests

**Week 6**: Polish & Release
- [ ] Bug fixes
- [ ] Documentation updates
- [ ] Release notes
- [ ] v1.0.0 release

### Phase 3: Enhancements (Target: v1.1+ - Ongoing)

- Extended varData types (VarString16/32)
- JSON converters
- Custom encoding hooks
- Additional tooling

---

## Acceptance Criteria

### For v1.0 Release

**Must Have**:
- ✅ Variable-length data (varData) fully implemented and tested
- ✅ Performance benchmarks published showing competitive performance
- ✅ 95%+ test coverage maintained
- ✅ All existing tests passing
- ✅ Documentation updated
- ✅ Real-world example using varData

**Should Have**:
- ⚠️ Nested groups implemented (if time permits)
- ⚠️ Comparative benchmarks vs Real Logic

**Nice to Have**:
- Extended varData types
- JSON converters
- Additional examples

---

## Success Metrics

### Feature Completeness
- **Target**: 95%+ of SBE 1.0 spec
- **Current**: ~85-90%
- **After varData**: ~90-92%
- **After nested groups**: ~95-98%

### Performance
- **Target**: Within 10% of Real Logic C# implementation
- **Current**: Not benchmarked
- **Required**: Formal benchmarks showing competitive performance

### Quality
- **Target**: 95%+ code coverage, <1 critical bug per release
- **Current**: ~90% coverage, 216 tests passing

---

## Resources

### Specification
- [SBE 1.0 Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding)
- [FIX SBE XML Primer](https://github.com/real-logic/simple-binary-encoding/wiki/FIX-SBE-XML-Primer)

### Reference Implementation
- [Real Logic SBE Tool](https://github.com/aeron-io/simple-binary-encoding)
- [Java Generator Source](https://github.com/real-logic/simple-binary-encoding/tree/master/sbe-tool/src/main/java/uk/co/real_logic/sbe/generation)

### Related Documents
- [SBE Feature Completeness](./SBE_FEATURE_COMPLETENESS.md)
- [SBE Generators Comparison](./SBE_GENERATORS_COMPARISON.md)
- [Implementation Roadmap](./SBE_IMPLEMENTATION_ROADMAP.md)

---

**Document Owner**: SbeSourceGenerator Team  
**Last Review**: 2025-10-16  
**Next Review**: When feature is implemented or quarterly
