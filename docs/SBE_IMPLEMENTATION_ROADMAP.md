# SBE Code Generator - Implementation Roadmap

This document outlines the roadmap for achieving complete SBE 1.0 specification compliance in the code generator.

## Vision

To create a fully-featured, production-ready SBE code generator that:
- ✅ Supports 100% of SBE 1.0 specification features
- ✅ Generates efficient, type-safe C# code
- ✅ Provides excellent developer experience with clear diagnostics
- ✅ Maintains backward compatibility
- ✅ Includes comprehensive test coverage

## Current Status (as of 2024-10-06)

**Overall Completeness**: ~70-75% of SBE 1.0 specification

See [SBE_FEATURE_COMPLETENESS.md](./SBE_FEATURE_COMPLETENESS.md) for detailed feature assessment.

---

## Type System Enhancements (Parallel Track)

This section tracks the progressive enhancement of generated types based on the feasibility study from PR #35.

### Phase 1: TypeDefinition Enhancements ✅ **COMPLETED**

**Goal**: Add constructors, readonly modifiers, and conversions to TypeDefinition.

**Status**: ✅ Completed (PR #38)

**Delivered Features**:
- [x] Readonly structs for `TypeDefinition`
- [x] Automatic constructors for `TypeDefinition`
- [x] Implicit conversion (primitive → wrapper)
- [x] Explicit conversion (wrapper → primitive)
- [x] Comprehensive test coverage
- [x] Migration guide and documentation

**Impact**:
- ~40% code reduction in typical usage
- Zero-cost abstractions (conversions are inlined)
- Breaking change with clear migration path
- 35 unit tests + 40 integration tests passing

**Documentation**:
- [PHASE1_IMPLEMENTATION.md](./PHASE1_IMPLEMENTATION.md) - Technical details
- [PHASE1_SUMMARY.md](./PHASE1_SUMMARY.md) - Executive summary
- [MIGRATION_GUIDE_PHASE1.md](./MIGRATION_GUIDE_PHASE1.md) - Migration guide

---

### Phase 2: Review and Planning ✅ **COMPLETED**

**Goal**: Review Phase 1 outcomes, document lessons learned, and plan future enhancements.

**Status**: ✅ Completed

**Deliverables**:
- [x] Comprehensive review of Phase 1 results
- [x] Feasibility analysis for future enhancements
- [x] Risk assessment for each potential enhancement
- [x] Recommendations for ref structs, OptionalTypeDefinition, and semantic types
- [x] Updated documentation and roadmap

**Key Findings**:
- ✅ Phase 1 approach was successful (low risk, high value)
- ✅ Ref struct enhancements are recommended (readonly + constructors)
- ⚠️ OptionalTypeDefinition requires careful design
- ⚠️ Semantic types need per-type evaluation
- ❌ Readonly for blittable types not feasible (MemoryMarshal incompatibility)

**Documentation**:
- [PHASE2_IMPLEMENTATION.md](./PHASE2_IMPLEMENTATION.md) - Technical analysis
- [PHASE2_SUMMARY.md](./PHASE2_SUMMARY.md) - Executive summary
- [FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md](./FEASIBILITY_STUDY_AUTO_CONSTRUCTORS_READONLY_CONVERSIONS.md) - Original study

---

### Phase 3: Ref Struct Enhancements ✅ **COMPLETED**

**Goal**: Add readonly modifiers and constructors to ref structs.

**Status**: ✅ Completed (Option 1 implemented)

**Delivered Features**:
- [x] Readonly modifier for ref structs (VarString8, etc.)
- [x] Readonly fields in ref structs
- [x] Constructors for ref structs
- [x] Updated Create() factory methods to use constructors
- [x] Comprehensive test coverage (4 new unit tests)
- [x] Migration guide and documentation

**Impact**:
- Enhanced type safety and immutability
- No performance regression (potential improvements)
- Breaking change with clear migration path
- 39 unit tests + 40 integration tests passing

**Documentation**:
- [PHASE3_IMPLEMENTATION.md](./PHASE3_IMPLEMENTATION.md) - Technical details
- [PHASE3_SUMMARY.md](./PHASE3_SUMMARY.md) - Executive summary
- [MIGRATION_GUIDE_PHASE3.md](./MIGRATION_GUIDE_PHASE3.md) - Migration guide

---

### Phase 4: Future Enhancements ❓ **UNDER EVALUATION**

**Goal**: Based on stakeholder feedback, potentially implement:

**Option 2: OptionalTypeDefinition Enhancements** (Medium Risk, Medium Value)
- [ ] Design review (private vs public field approach)
- [ ] Constructors for optional types
- [ ] Nullable conversions
- [ ] Migration guide

**Estimated Effort**: 2-3 sprints  
**Risk**: Medium  
**Breaking Changes**: Yes (field visibility, initialization pattern)

**Option 3: Semantic Type Conversions** (Per-Type Evaluation)
- [ ] LocalMktDate ↔ DateOnly conversions
- [ ] Decimal conversions with precision handling
- [ ] UTCTimestamp ↔ DateTime/DateTimeOffset conversions
- [ ] Document precision considerations

**Estimated Effort**: 2-4 sprints (depends on scope)  
**Risk**: Medium-High  
**Breaking Changes**: No (additive)

**Decision Point**: Awaiting stakeholder feedback and community input.

---

## Phase 1: Core Completeness (High Priority)

### 1.1 Variable-Length Data Support ❌

**Goal**: Support `<data>` elements for variable-length strings and binary blobs.

**Estimated Effort**: Medium (2-3 weeks)

**Tasks**:
- [ ] Create `SchemaDataDto` for parsing `<data>` elements
- [ ] Update `SchemaParser.ParseMessage()` to extract data fields
- [ ] Implement `VariableLengthDataFieldDefinition` generator
- [ ] Generate length-prefixed field accessors
- [ ] Handle UTF-8 string encoding/decoding
- [ ] Support binary blob data
- [ ] Add buffer bounds checking
- [ ] Write comprehensive tests for varData

**Acceptance Criteria**:
```xml
<!-- Should support this schema pattern -->
<message name="TextMessage" id="10">
    <field name="msgId" id="1" type="uint32"/>
    <data name="text" id="2" type="varDataEncoding"/>
</message>
```

**Implementation Notes**:
- Variable data always comes at the end of messages
- Format: `uint16 length` + `byte[] data`
- Need to track current buffer position
- Consider streaming vs materialized approaches

**Files to Create/Modify**:
- `SbeCodeGenerator/Schema/SchemaDataDto.cs` (new)
- `SbeCodeGenerator/Generators/Fields/VariableLengthDataFieldDefinition.cs` (new)
- `SbeCodeGenerator/Generators/MessagesCodeGenerator.cs` (modify)
- `SbeCodeGenerator.Tests/VariableLengthDataTests.cs` (new)

---

### 1.2 Schema Versioning & Evolution ✅

**Goal**: Support schema evolution with `sinceVersion` attribute and version-aware encoding/decoding.

**Status**: ✅ **COMPLETED**

**Completed Tasks**:
- [x] Parse `sinceVersion` attribute on fields
- [x] Store version info in field DTOs
- [x] Generate version documentation in field comments
- [x] Support field skipping via block length extension
- [x] Implement block length extension for schema evolution
- [x] Document versioning best practices
- [x] Create comprehensive integration tests (8 tests)
- [x] Create SCHEMA_VERSIONING.md documentation

**Implementation**:
- Fields with `sinceVersion` are documented with "Since version N" in XML comments
- TryParse methods accept `blockLength` parameter for version-aware parsing
- Decoders handle messages from any schema version via block length
- Fields from newer versions have default values when reading older messages

**Acceptance Criteria**: ✅ All Met
```xml
<!-- Schema v1 -->
<message name="Order" id="1" version="0">
    <field name="orderId" id="1" type="uint64"/>
    <field name="price" id="2" type="int64"/>
</message>

<!-- Schema v2 - adds optional field -->
<message name="Order" id="1" version="1">
    <field name="orderId" id="1" type="uint64"/>
    <field name="price" id="2" type="int64"/>
    <field name="quantity" id="3" type="int64" sinceVersion="1"/>
</message>
```

**Files Created/Modified**:
- ✅ `SbeCodeGenerator/Schema/SchemaFieldDto.cs` (SinceVersion property exists)
- ✅ `SbeCodeGenerator/Generators/Fields/MessageFieldDefinition.cs` (modified)
- ✅ `SbeCodeGenerator/Generators/Fields/OptionalMessageFieldDefinition.cs` (modified)
- ✅ `SbeCodeGenerator/Generators/MessagesCodeGenerator.cs` (modified)
- ✅ `SbeCodeGenerator.IntegrationTests/VersioningIntegrationTests.cs` (new - 8 tests)
- ✅ `docs/SCHEMA_VERSIONING.md` (new - comprehensive guide)
- ✅ `TestSchemas/versioning-test-schema.xml` (new - test schema with v0, v1, v2 fields)

---

### 1.3 Deprecated Field Handling ✅ **COMPLETED**

**Goal**: Properly mark and document deprecated fields in generated code.

**Estimated Effort**: Small (1 week)

**Tasks**:
- [x] Add `[Obsolete]` attribute to deprecated fields
- [x] Include deprecation message in attribute
- [x] Update documentation generation for deprecated items
- [x] Add compiler warnings for deprecated field usage
- [x] Test that deprecated fields still work correctly
- [x] Document migration path from deprecated fields

**Acceptance Criteria**: ✅ All Met
```csharp
// Generated code includes:
[Obsolete("This field is deprecated")]
[FieldOffset(8)]
public Price OldPrice;

// With sinceVersion, includes version info:
[Obsolete("This field is deprecated since version 1")]
[FieldOffset(24)]
public long LegacyQuantity;
```

**Files Modified**:
- ✅ `SbeCodeGenerator/Schema/SchemaFieldDto.cs` (added Deprecated property)
- ✅ `SbeCodeGenerator/Schema/SchemaParser.cs` (parse deprecated attribute)
- ✅ `SbeCodeGenerator/Generators/Fields/MessageFieldDefinition.cs` (generate [Obsolete] attribute)
- ✅ `SbeCodeGenerator/Generators/Fields/OptionalMessageFieldDefinition.cs` (generate [Obsolete] attribute)
- ✅ `SbeCodeGenerator/Generators/MessagesCodeGenerator.cs` (pass deprecated to field generators)
- ✅ `tests/SbeCodeGenerator.Tests/MessagesCodeGeneratorTests.cs` (unit tests added)
- ✅ `tests/SbeCodeGenerator.IntegrationTests/DeprecatedFieldsIntegrationTests.cs` (integration tests added)
- ✅ `tests/SbeCodeGenerator.IntegrationTests/TestSchemas/deprecated-test-schema.xml` (test schema created)

---

## Phase 2: Validation & Quality (Medium Priority)

### 2.1 Validation Constraints ✅ **COMPLETED**

**Goal**: Enforce schema validation constraints in generated code.

**Estimated Effort**: Medium (2-3 weeks)

**Status**: ✅ **Completed**

**Tasks**:
- [x] Parse min/max value attributes from schema
- [x] Generate validation methods for messages
- [x] Add range checking for numeric types
- [ ] Validate enum values against valid values (future enhancement)
- [ ] Support character set validation (future enhancement)
- [x] Generate helpful error messages for validation failures
- [x] Make validation optional (performance vs safety trade-off)
- [x] Add validation tests

**Acceptance Criteria**:
```xml
<type name="Price" primitiveType="int64" minValue="0" maxValue="999999999"/>
```

```csharp
// Generated code includes:
public static void Validate(this Price value)
{
    if (value.Value < 0 || value.Value > 999999999)
        throw new ArgumentOutOfRangeException(nameof(value), value.Value, 
            "Price must be between 0 and 999999999");
}
```

✅ **Implementation Complete**

**Files Created/Modified**:
- ✅ `SbeCodeGenerator/Schema/SchemaTypeDto.cs` (added MinValue, MaxValue)
- ✅ `SbeCodeGenerator/Schema/SchemaFieldDto.cs` (added MinValue, MaxValue)
- ✅ `SbeCodeGenerator/Schema/SchemaParser.cs` (parse min/max attributes)
- ✅ `SbeCodeGenerator/Generators/ValidationGenerator.cs` (new)
- ✅ `SbeCodeGenerator/SBESourceGenerator.cs` (register ValidationGenerator)
- ✅ `SbeCodeGenerator.Tests/ValidationGeneratorTests.cs` (new)
- ✅ `SbeCodeGenerator.IntegrationTests/GeneratorIntegrationTests.cs` (added validation tests)

**Documentation**:
- ✅ [VALIDATION_CONSTRAINTS.md](./VALIDATION_CONSTRAINTS.md)

---

### 2.2 Byte Order (Endianness) Handling ✅ **COMPLETED**

**Goal**: Properly handle byte order for cross-platform compatibility.

**Estimated Effort**: Medium (2 weeks)

**Tasks**:
- [x] Parse `byteOrder` attribute from schema
- [x] Detect platform endianness at runtime
- [x] Generate byte-swapping code when needed
- [x] Add helper methods for endian conversion
- [x] Test on both little-endian and big-endian platforms
- [x] Document byte order requirements
- [x] Benchmark performance impact of byte swapping

**Acceptance Criteria**:
```xml
<messageSchema byteOrder="bigEndian">
```

Generated code automatically provides proper methods for byte swapping when needed.

**Implementation Notes**:
- Most platforms are little-endian (x86, x64, ARM)
- Big-endian mainly for network protocols
- Use `BitConverter.IsLittleEndian` for runtime detection
- Consider using `BinaryPrimitives` class for swapping

**Completed Implementation**:
- `SchemaContext.ByteOrder` stores the schema's byte order setting
- `EndianHelpers` class provides Read*/Write* methods for both byte orders
- Default byte order is "littleEndian" if not specified
- Comprehensive unit and integration tests validate functionality

**Files Created/Modified**:
- `SbeCodeGenerator/SchemaContext.cs` (modified - added ByteOrder property)
- `SbeCodeGenerator/SBESourceGenerator.cs` (modified - parse byteOrder attribute)
- `SbeCodeGenerator/Generators/EndianHelpers.cs` (modified - added Write methods)
- `SbeCodeGenerator.Tests/EndianTests.cs` (new - unit tests)
- `SbeCodeGenerator.IntegrationTests/EndianIntegrationTests.cs` (new - integration tests)
- `docs/SBE_FEATURE_COMPLETENESS.md` (updated)
- `docs/SBE_CHECKLIST.md` (updated)
- `docs/SBE_IMPLEMENTATION_ROADMAP.md` (updated)

---

### 2.3 Enhanced Diagnostics ⚠️

**Goal**: Improve diagnostic messages and error reporting.

**Estimated Effort**: Small (1 week)

**Tasks**:
- [ ] Add more specific diagnostic codes
- [ ] Include XML line numbers in diagnostics (if available)
- [ ] Suggest fixes for common errors
- [ ] Create diagnostic documentation
- [ ] Add warnings for non-optimal schema patterns
- [ ] Implement diagnostic suppression mechanism

**New Diagnostics**:
- **SBE007**: Variable data must be last in message
- **SBE008**: Invalid version number
- **SBE009**: Missing required composite type
- **SBE010**: Circular type reference
- **SBE011**: Block length mismatch

**Files to Modify**:
- `SbeCodeGenerator/Diagnostics/SbeDiagnostics.cs`
- `SbeCodeGenerator/Diagnostics/README.md`

---

## Phase 3: Advanced Features (Lower Priority)

### 3.1 Custom Encoding/Decoding Hooks ❌

**Goal**: Allow custom serialization logic for special cases.

**Estimated Effort**: Medium (2-3 weeks)

**Tasks**:
- [ ] Design extensibility API
- [ ] Add partial classes for user customization
- [ ] Support custom type converters
- [ ] Allow pre/post processing hooks
- [ ] Document extension points
- [ ] Create examples of custom encoders

**Example Use Cases**:
- Custom compression for large fields
- Encryption/decryption of sensitive data
- Custom date/time formats
- Application-specific transformations

---

### 3.2 Multi-Schema Support ⚠️

**Goal**: Better support for projects with multiple schemas.

**Estimated Effort**: Small-Medium (1-2 weeks)

**Tasks**:
- [ ] Test cross-schema type references
- [ ] Improve namespace handling for multiple schemas
- [ ] Support schema imports/includes
- [ ] Document multi-schema best practices
- [ ] Add integration tests with multiple schemas

---

### 3.3 Performance Optimizations 🔧

**Goal**: Optimize generated code for high-performance scenarios.

**Estimated Effort**: Medium (2-3 weeks)

**Tasks**:
- [ ] Profile generated code performance
- [ ] Optimize memory layout for cache efficiency
- [ ] Use Span<T> and Memory<T> for zero-copy operations
- [ ] Implement buffer pooling for allocations
- [ ] Add benchmark tests
- [ ] Create performance tuning guide

**Target Metrics**:
- < 100ns per message encode/decode (simple messages)
- Zero heap allocations for encoding/decoding
- Support for millions of messages per second

---

### 3.4 Enhanced Code Generation Options 🔧

**Goal**: Provide options to customize generated code.

**Estimated Effort**: Medium (2 weeks)

**Tasks**:
- [ ] Add configuration file support (`.sbegen` config)
- [ ] Allow customization of namespace generation
- [ ] Support different code styles (properties vs fields)
- [ ] Option to generate interfaces
- [ ] Option to generate XML documentation
- [ ] Option for nullable reference types
- [ ] Generate JSON converters (optional)

**Configuration Example**:
```json
{
  "sbeCodeGenerator": {
    "namespace": "MyApp.Messages",
    "useNullableReferenceTypes": true,
    "generateInterfaces": true,
    "generateXmlDocs": true,
    "codeStyle": "properties"
  }
}
```

---

## Phase 4: Developer Experience

### 4.1 Documentation & Examples 📚

**Goal**: Comprehensive documentation for users.

**Estimated Effort**: Ongoing

**Tasks**:
- [ ] Create getting started guide
- [ ] Write API reference documentation
- [ ] Add schema authoring best practices
- [ ] Create example projects
- [ ] Add tutorial videos
- [ ] Document common pitfalls and solutions
- [ ] Create troubleshooting guide

**Documentation Structure**:
```
docs/
├── getting-started.md
├── schema-reference.md
├── api-reference.md
├── best-practices.md
├── troubleshooting.md
├── examples/
│   ├── simple-messaging/
│   ├── market-data/
│   └── schema-evolution/
└── videos/
```

---

### 4.2 Tooling Improvements 🔧

**Goal**: Better tooling support for schema development.

**Estimated Effort**: Large (4-6 weeks)

**Tasks**:
- [ ] Create Visual Studio extension
- [ ] Add XML schema validation
- [ ] Provide IntelliSense for SBE schemas
- [ ] Create schema visualization tool
- [ ] Add code snippets for common patterns
- [ ] Build schema migration tool
- [ ] Create diff tool for schema versions

---

### 4.3 Testing Infrastructure 🧪

**Goal**: Achieve comprehensive test coverage.

**Estimated Effort**: Ongoing

**Tasks**:
- [ ] Increase unit test coverage to 90%+
- [ ] Add property-based tests (FsCheck)
- [ ] Create performance benchmarks
- [ ] Add compatibility tests against reference implementation
- [ ] Implement fuzzing tests
- [ ] Add round-trip encoding/decoding tests
- [ ] Create test schema library

**Current Coverage**: ~70% (estimated)  
**Target Coverage**: 95%+

---

## Phase 5: Production Readiness

### 5.1 Stability & Reliability ✅

**Tasks**:
- [ ] Fix all known bugs
- [ ] Add comprehensive error handling
- [ ] Implement graceful degradation
- [ ] Add telemetry/logging
- [ ] Conduct security review
- [ ] Perform memory leak testing

---

### 5.2 Performance Validation ⚡

**Tasks**:
- [ ] Benchmark against reference implementations
- [ ] Profile memory usage
- [ ] Test with large schemas (1000+ messages)
- [ ] Optimize hot paths
- [ ] Validate zero-allocation scenarios

---

### 5.3 Release Process 📦

**Tasks**:
- [ ] Create NuGet package
- [ ] Set up CI/CD pipeline
- [ ] Implement semantic versioning
- [ ] Create release notes automation
- [ ] Add changelog
- [ ] Set up automated releases

---

## Success Metrics

### Feature Completeness
- ✅ 100% of SBE 1.0 spec implemented
- ✅ All examples from spec work correctly
- ✅ Compatible with other SBE implementations

### Quality
- ✅ 95%+ code coverage
- ✅ Zero critical bugs
- ✅ All diagnostic codes documented
- ✅ Complete API documentation

### Performance
- ✅ < 100ns encode/decode for simple messages
- ✅ Zero allocations for hot paths
- ✅ Support 1M+ messages/second

### Developer Experience
- ✅ Clear, helpful error messages
- ✅ Comprehensive examples
- ✅ Quick start guide < 5 minutes
- ✅ Active community support

---

## Dependencies & Prerequisites

### Required Tools
- .NET SDK 9.0+
- C# 12.0+
- Roslyn Source Generators

### Optional Tools
- BenchmarkDotNet (performance testing)
- FsCheck (property-based testing)
- Visual Studio 2022+ (IDE support)

---

## Contributing

We welcome contributions! Priority areas:

**High Impact, Low Effort**:
1. Add more diagnostic messages
2. Improve documentation
3. Add example schemas
4. Write tests for edge cases

**High Impact, Medium Effort**:
1. Implement variable-length data support
2. Add validation constraints
3. Improve byte order handling

**High Impact, High Effort**:
1. Schema versioning implementation
2. Performance optimizations
3. Tooling development

See [CONTRIBUTING.md](./CONTRIBUTING.md) for guidelines.

---

## Timeline

### Q4 2024
- ✅ Feature completeness assessment (done)
- ✅ Type System Phase 1: TypeDefinition enhancements (done - PR #38)
- ✅ Type System Phase 2: Review and planning (done)
- Phase 1.1: Variable-length data
- Phase 1.3: Deprecated fields
- Phase 2.3: Enhanced diagnostics

### Q1 2025
- Type System Phase 3: Ref struct or OptionalTypeDefinition enhancements (pending decision)
- Phase 1.2: Schema versioning
- ✅ Phase 2.1: Validation constraints (completed)
- Phase 2.2: Byte order handling

### Q2 2025
- Phase 3.1: Custom encoding hooks
- Phase 3.2: Multi-schema support
- Phase 4.1: Documentation improvements

### Q3 2025
- Phase 3.3: Performance optimizations
- Phase 4.2: Tooling improvements
- Phase 5: Production readiness

### Q4 2025
- Public beta release
- Performance validation
- Final documentation
- Version 1.0 release 🎉

---

## Risk Assessment

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Schema spec changes | High | Low | Track spec updates closely |
| Performance issues | Medium | Medium | Early benchmarking |
| Breaking changes needed | Medium | Medium | Semantic versioning |
| Limited resources | High | Medium | Prioritize ruthlessly |

---

## Questions & Feedback

For questions about the roadmap or to suggest priorities:
- Open an issue on GitHub
- Join discussions in Issues
- Contact maintainers

---

**Last Updated**: 2024-10-06  
**Roadmap Version**: 1.0
