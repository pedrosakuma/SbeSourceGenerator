# SBE Code Generator - Feature Completeness Report

This document tracks the implementation status of features from the [FIX SBE Specification](https://www.fixtrading.org/standards/sbe/) in this code generator.

## Overview

**Last Updated**: 2026-04-09  
**SBE Specification Version**: 1.0  
**Generator Version**: 0.4.0

## Feature Implementation Status

### ✅ Core Features - Implemented

#### 1. Primitive Types Support
**Status**: ✅ **FULLY IMPLEMENTED**

The generator supports all SBE primitive types with proper C# mappings:

| SBE Type | C# Type | Length | Implementation |
|----------|---------|---------|----------------|
| `int8` | `sbyte` | 1 byte | `TypesCatalog.cs` |
| `int16` | `short` | 2 bytes | `TypesCatalog.cs` |
| `int32` | `int` | 4 bytes | `TypesCatalog.cs` |
| `int64` | `long` | 8 bytes | `TypesCatalog.cs` |
| `uint8` | `byte` | 1 byte | `TypesCatalog.cs` |
| `uint16` | `ushort` | 2 bytes | `TypesCatalog.cs` |
| `uint32` | `uint` | 4 bytes | `TypesCatalog.cs` |
| `uint64` | `ulong` | 8 bytes | `TypesCatalog.cs` |
| `char` | `char` or `byte[]` | Variable | `FixedSizeCharTypeDefinition.cs` |

**Test Coverage**: 
- `TypesCodeGeneratorTests.cs` - Tests simple type generation
- `TypesCatalog.cs` - Defines primitive type lengths

**References**:
- `SbeCodeGenerator/TypesCatalog.cs`
- `SbeCodeGenerator/Generators/TypesCodeGenerator.cs` (lines 313-347)

---

#### 2. Message Encoding/Decoding
**Status**: ✅ **IMPLEMENTED**

Messages are generated as C# structs with proper field layout and offsets.

**Features**:
- Message header support (`MessageHeader` composite)
- Field offset calculation (automatic and manual)
- Message ID and template ID support
- Description and semantic type metadata
- Deprecated message marking

**Implementation**:
- `MessagesCodeGenerator.cs` - Generates message definitions
- `MessageDefinition.cs` - Message structure generator
- `MessageFieldDefinition.cs` - Field code generation

**Test Coverage**:
- `MessagesCodeGeneratorTests.cs` - Tests message generation
- `SnapshotTests.cs` - Validates generated message structure

---

#### 3. Optional Fields
**Status**: ✅ **IMPLEMENTED**

Optional fields are supported with null value semantics.

**Features**:
- `presence="optional"` attribute handling
- Null value constants from SBE specification
- C# nullable type mapping where appropriate
- Automatic null value detection per primitive type

**Implementation**:
- `OptionalMessageFieldDefinition.cs` - Optional message fields
- `OptionalTypeDefinition.cs` - Optional type definitions
- `NullableValueFieldDefinition.cs` - Nullable composite fields
- `TypesCatalog.cs` - Null value mappings

**Test Coverage**:
- Tests for optional field generation in `TypesCodeGeneratorTests.cs`

**Example**:
```csharp
// For an optional int32 field with nullValue="2147483647"
private int fieldName;
public int? FieldName => fieldName == int.MaxValue ? null : fieldName;
```

---

#### 4. Composite Types
**Status**: ✅ **IMPLEMENTED**

Composite types are fully supported with nested field definitions.

**Features**:
- Struct-based composite definitions
- Nested field support
- Constant fields in composites
- Optional fields in composites
- Semantic type support for composites (Price, UTCTimestamp, etc.)

**Implementation**:
- `CompositeDefinition.cs` - Main composite generator
- `ValueFieldDefinition.cs` - Regular composite fields
- `ConstantTypeFieldDefinition.cs` - Constant fields
- `NullableValueFieldDefinition.cs` - Optional fields

**Semantic Type Extensions**:
- `DecimalSemanticTypeDefinition.cs` - Price/decimal types
- `UTCTimestampSemanticTypeDefinition.cs` - Timestamp types
- `MonthYearSemanticTypeDefinition.cs` - MaturityMonthYear
- `LocalMktDateSemanticTypeDefinition.cs` - Date types

**Test Coverage**:
- `TypesCodeGeneratorTests.Generate_WithComposite_ProducesCompositeCode`
- Integration tests with real B3 schemas

---

#### 5. Enumerations (Enums)
**Status**: ✅ **IMPLEMENTED**

Enums are generated as C# enums with proper encoding types.

**Features**:
- Standard enum generation
- Nullable enum support (for types with NULL encoding)
- Character-based enums (converted to byte)
- Proper encoding type mapping

**Implementation**:
- `EnumDefinition.cs` - Standard enum generation
- `NullableEnumDefinition.cs` - Nullable enums
- `EnumFieldDefinition.cs` - Enum value definitions

**Test Coverage**:
- `TypesCodeGeneratorTests.Generate_WithSimpleEnum_ProducesEnumCode`

**Example**:
```csharp
public enum Side : byte
{
    Buy = 0,
    Sell = 1,
}
```

---

#### 6. Bit Sets (Choice Sets)
**Status**: ✅ **IMPLEMENTED**

Sets are generated as C# Flag enums with bit-shift operations.

**Features**:
- `[Flags]` attribute generation
- Bit-shift value calculation (1 << N)
- Multiple choice selection support
- Validation of bit values (0-31 for typical flags)

**Implementation**:
- `EnumFlagsDefinition.cs` - Set/flags generator
- Diagnostic validation for invalid bit values (SBE003)

**Test Coverage**:
- `TypesCodeGeneratorTests.Generate_WithSet_ProducesSetCode`
- `DiagnosticsTests.cs` - Invalid flag value tests

**Example**:
```csharp
[Flags]
public enum Flags : byte
{
    Active = 1 << 0,      // 1
    Cancelled = 1 << 1,   // 2
}
```

---

#### 7. Repeating Groups
**Status**: ✅ **IMPLEMENTED**

Repeating groups are supported with dimension encoding.

**Features**:
- Group dimension type support (`GroupSizeEncoding`)
- Nested field definitions in groups
- Block length calculation
- Group message size constants
- Constant fields in groups

**Implementation**:
- `GroupDefinition.cs` - Group structure generator
- Group dimension handling in messages
- Automatic offset calculation for group fields

**Test Coverage**:
- Integration tests with B3 schemas containing groups
- Snapshot tests validate group structure

---

#### 8. Constant Fields
**Status**: ✅ **IMPLEMENTED**

Constant fields are supported in messages, composites, and groups.

**Features**:
- `presence="constant"` attribute handling
- Value reference support (`valueRef` attribute)
- Inline constant values
- Proper typing for constant values

**Implementation**:
- `ConstantTypeDefinition.cs` - Type-level constants
- `ConstantMessageFieldDefinition.cs` - Message-level constants
- `ConstantTypeFieldDefinition.cs` - Composite-level constants

---

#### 9. Field Offsets
**Status**: ✅ **IMPLEMENTED**

Both automatic and manual field offset calculation is supported.

**Features**:
- Automatic offset calculation based on field sizes
- Manual offset specification via `offset` attribute
- C# `[FieldOffset]` attribute generation for explicit layout
- Struct layout with `[StructLayout(LayoutKind.Explicit)]`

**Implementation**:
- Offset calculation in generator methods
- Blittable field interface (`IBlittableMessageField`)
- Field length tracking in `SchemaContext`

---

#### 10. Schema Versioning
**Status**: ✅ **IMPLEMENTED**

Schema versioning with `sinceVersion` attribute is now fully implemented.

**Implemented Features**:
- ✅ Schema version attribute parsing
- ✅ Semantic version attribute
- ✅ Schema ID in generated code
- ✅ Field-level `sinceVersion` attribute support
- ✅ Block length extension for schema evolution
- ✅ Backward compatibility (new decoders read old messages)
- ✅ Forward compatibility (old decoders skip unknown fields)
- ✅ Version documentation in generated code

**Implementation**:
- `SchemaFieldDto` stores `sinceVersion` for each field
- Field generators include version info in XML documentation
- TryParse methods accept `blockLength` parameter for version-aware parsing
- Comprehensive integration tests validate schema evolution scenarios

**Documentation**:
- [Schema Versioning Guide](./SCHEMA_VERSIONING.md)

**Tests**:
- 8 integration tests in `VersioningIntegrationTests.cs`
- Test schema with v0, v1, and v2 fields in `versioning-test-schema.xml`

---

### ⚠️ Features Partially Implemented

#### 11. Variable-Length Data (varData)
**Status**: ✅ **IMPLEMENTED**

Variable-length data fields are fully supported with encoding and decoding.

**Implemented Features**:
- `<data>` element support in schema parsing
- VarString8 and VarData code generation
- Length-prefixed encoding/decoding
- UTF-8 string support
- Buffer bounds checking

**Implementation**:
- Schema parsing for `<data>` elements
- `MessagesCodeGenerator` handles varData fields
- Generated Encode/Decode methods for variable-length data
- Integration tests with real schemas

---

#### 12. Referenced Types Across Schemas
**Status**: ⚠️ **NOT VERIFIED**

Cross-schema type references are not tested.

**Current State**:
- Each schema is processed independently
- No tests for type imports/references between schemas

**Recommendation**: Add tests and documentation for multi-schema projects.

---

### ❌ Missing Features

#### 13. Block Length Extension
**Status**: ✅ **IMPLEMENTED**

The ability to extend message block lengths for schema evolution is now implemented.

**Implementation**:
- TryParse methods now accept an optional `blockLength` parameter
- Decoders can handle messages with different block lengths
- Supports both forward and backward compatibility scenarios

**Required for**:
- Adding optional fields to end of messages
- Maintaining backward compatibility

---

#### 14. Validation Constraints
**Status**: ✅ **IMPLEMENTED**

Schema-level validation constraints are now enforced in generated code.

**Supported Validations**:
- Min/max value ranges
- Range checking for numeric types
- Extension methods for validation

**Implementation**:
- `ValidationGenerator` generates validation extension methods
- Supports both type-level and field-level constraints
- Runtime validation via `.Validate()` method calls

**Required for**:
- Data integrity enforcement
- Schema compliance validation
- Runtime value checking

**See**: [Validation Constraints Documentation](./VALIDATION_CONSTRAINTS.md)

---

#### 15. Byte Order (Endianness)
**Status**: ✅ **IMPLEMENTED**

The generator now explicitly handles byte order.

**Current State**:
- Parses `byteOrder` attribute from messageSchema element
- Stores byte order in SchemaContext
- Generated EndianHelpers class provides methods for both little-endian and big-endian operations
- Supports reading and writing with proper byte order conversion
- Includes comprehensive tests for both endianness scenarios

**Implementation**:
- `SchemaContext.ByteOrder` property stores the schema's byte order
- `EndianHelpers` class provides Read*/Write* methods for both byte orders
- Default byte order is "littleEndian" if not specified in schema

---

#### 16. Custom Encoding/Decoding
**Status**: ❌ **NOT IMPLEMENTED**

No extensibility points for custom encoding logic.

**Missing**:
- Custom encoder/decoder hooks
- Pluggable serialization strategies

---

#### 17. Message Extensions (sinceVersion)
**Status**: ✅ **IMPLEMENTED**

Field-level version tracking for schema evolution is now fully supported.

**Implemented Features**:
- ✅ `sinceVersion` attribute parsing and storage
- ✅ Version information in generated XML documentation
- ✅ Block length-based field presence detection
- ✅ Backward compatibility (new decoders read old messages)
- ✅ Forward compatibility (old decoders skip unknown fields)

**Implementation**:
- Fields with `sinceVersion` include "Since version N" in documentation
- TryParse methods accept `blockLength` parameter for version-aware parsing
- Comprehensive integration tests cover V1/V2/V3 schema evolution scenarios

**Example**:
```xml
<field name="quantity" id="3" type="int64" sinceVersion="1" 
       description="Order quantity - added in version 1"/>
```

Generates:
```csharp
/// <summary>
/// Order quantity - added in version 1
/// 
/// Since version 1
/// (MessageFieldDefinition)
/// </summary>
[FieldOffset(16)]
public long Quantity;
```

**See**: [Schema Versioning Guide](./SCHEMA_VERSIONING.md)

**Tests**: 8 comprehensive integration tests in `VersioningIntegrationTests.cs`

---

---

#### 18. Deprecated Fields Handling
**Status**: ✅ **IMPLEMENTED**

Deprecated fields are properly marked with `[Obsolete]` attribute.

**Current**:
- `deprecated` attribute is parsed ✅
- Stored in DTOs ✅
- Reflected in generated code with `[Obsolete]` attribute ✅
- Compiler warnings generated (CS0618) ✅
- Version information included in deprecation message when `sinceVersion` is present ✅
- Comprehensive unit and integration tests ✅

**Implementation**:
- Fields with `deprecated` attribute generate `[Obsolete("This field is deprecated")]`
- Fields with both `deprecated` and `sinceVersion` generate `[Obsolete("This field is deprecated since version N")]`
- Backward compatibility maintained - deprecated fields still work correctly

---

## Testing Coverage

### Current Test Suite

Comprehensive test suite with unit and integration tests — all passing ✅

**Unit Tests** (`SbeCodeGenerator.Tests`):
- Primitive type generation
- Enum generation  
- Set generation
- Composite generation
- Message generation
- Diagnostics validation
- Validation constraint generation
- Variable-length data generation

**Integration Tests** (`SbeCodeGenerator.IntegrationTests`):
- Real-world B3 schema processing
- Snapshot testing for generated code
- Validation constraint enforcement
- Schema versioning (8 tests in `VersioningIntegrationTests.cs`)
- Variable-length data encoding/decoding

### Test Coverage Gaps

- ✅ **Variable-length data** - Fully tested
- ✅ **Schema versioning** - Fully tested (8 integration tests in `VersioningIntegrationTests.cs`)
- ✅ **Validation constraints** - Fully tested
- ✅ **Byte order handling** - Fully tested (unit + integration tests)
- ❌ No tests for multi-schema references

---

## Code Quality

### Diagnostics System ✅

Comprehensive diagnostic reporting for:
- **SBE001**: Invalid integer attribute values
- **SBE002**: Missing required attributes
- **SBE003**: Invalid enum flag values
- **SBE004**: Malformed schema XML
- **SBE005**: Unsupported constructs (warning)
- **SBE006**: Invalid type lengths

See: `SbeCodeGenerator/Diagnostics/` directory

### Architecture ✅

Well-structured codebase:
- Separated generators for Types, Messages, Utilities
- DTO-based XML parsing (no direct XML access in generators)
- Helper utilities for common operations
- Consistent code generation patterns

See: `ARCHITECTURE_DIAGRAMS.md`, `IMPLEMENTATION_SUMMARY.md`

---

## Compliance Summary

### SBE 1.0 Specification Compliance

| Feature Category | Status | Notes |
|------------------|--------|-------|
| **Primitive Types** (int8–uint64, char, float, double) | ✅ Implemented | All 11 SBE primitives mapped to C# |
| **Type Definitions** (presence, nullValue, length, minValue/maxValue) | ✅ Implemented | `characterEncoding` parsed but not used ([#96](https://github.com/pedrosakuma/SbeSourceGenerator/issues/96)) |
| **Composite Types** | ⚠️ Partial | Flat composites work. Nested composites, `<ref>`, inline enum/set **not supported** ([#88](https://github.com/pedrosakuma/SbeSourceGenerator/issues/88), [#89](https://github.com/pedrosakuma/SbeSourceGenerator/issues/89)) |
| **Enums** | ✅ Implemented | `sinceVersion`/`deprecated` on validValues not applied ([#94](https://github.com/pedrosakuma/SbeSourceGenerator/issues/94)) |
| **Sets (Bitsets)** | ✅ Implemented | Max 64 choices not validated ([#98](https://github.com/pedrosakuma/SbeSourceGenerator/issues/98)) |
| **Messages** | ⚠️ Partial | Explicit `blockLength` not read from schema ([#93](https://github.com/pedrosakuma/SbeSourceGenerator/issues/93)). Custom `headerType` not supported ([#92](https://github.com/pedrosakuma/SbeSourceGenerator/issues/92)) |
| **Fields** | ✅ Implemented | `presence`, `offset`, `valueRef`, `sinceVersion`, `deprecated` all work. `epoch`/`timeUnit` not parsed (deprecated in spec) |
| **Repeating Groups** | ⚠️ Partial | Flat groups work. **Nested groups not supported** ([#90](https://github.com/pedrosakuma/SbeSourceGenerator/issues/90)). Data in groups not supported ([#91](https://github.com/pedrosakuma/SbeSourceGenerator/issues/91)). Explicit `blockLength` on groups not read ([#93](https://github.com/pedrosakuma/SbeSourceGenerator/issues/93)) |
| **Variable Data** | ✅ Implemented | uint8 and uint16 length prefixes. uint32 not supported ([#97](https://github.com/pedrosakuma/SbeSourceGenerator/issues/97)). `sinceVersion` on data not parsed ([#95](https://github.com/pedrosakuma/SbeSourceGenerator/issues/95)) |
| **Optional Fields** | ✅ Implemented | Null sentinel values fully supported |
| **Constant Fields** | ✅ Implemented | In messages, composites, and groups |
| **Schema Versioning** | ✅ Implemented | `sinceVersion`, `deprecated`, block length extension |
| **Validation** | ✅ Implemented | minValue/maxValue range checks |
| **Byte Order** | ✅ Implemented | Little-endian and big-endian with runtime detection |

Open gaps tracked at: [spec-compliance issues](https://github.com/pedrosakuma/SbeSourceGenerator/labels/spec-compliance)

---

## Recommendations

All spec compliance gaps and feature requests are tracked as [GitHub issues](https://github.com/pedrosakuma/SbeSourceGenerator/labels/spec-compliance). Key gaps:

- **High priority**: Composite `<ref>` support ([#88](https://github.com/pedrosakuma/SbeSourceGenerator/issues/88)), nested composites ([#89](https://github.com/pedrosakuma/SbeSourceGenerator/issues/89)), nested groups ([#90](https://github.com/pedrosakuma/SbeSourceGenerator/issues/90))
- **Medium priority**: Data in groups ([#91](https://github.com/pedrosakuma/SbeSourceGenerator/issues/91)), custom headerType ([#92](https://github.com/pedrosakuma/SbeSourceGenerator/issues/92)), explicit blockLength ([#93](https://github.com/pedrosakuma/SbeSourceGenerator/issues/93))
- **Low priority**: Enum versioning ([#94](https://github.com/pedrosakuma/SbeSourceGenerator/issues/94)), data sinceVersion ([#95](https://github.com/pedrosakuma/SbeSourceGenerator/issues/95)), characterEncoding ([#96](https://github.com/pedrosakuma/SbeSourceGenerator/issues/96)), extended varData ([#97](https://github.com/pedrosakuma/SbeSourceGenerator/issues/97)), set validation ([#98](https://github.com/pedrosakuma/SbeSourceGenerator/issues/98))

---

## References

- [FIX SBE Standard](https://www.fixtrading.org/standards/sbe/)
- [SBE 1.0 Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding)
- [Simple Binary Encoding Wiki](https://github.com/real-logic/simple-binary-encoding/wiki)

---

## Change Log

| Date | Version | Changes |
|------|---------|---------|
| 2025-10-16 | 1.2 | Fixed GroupSizeEncoding bug, noted nested groups limitation |
| 2025-10-15 | 1.1 | Updated dates and current status |
| 2024-10-06 | 1.0 | Initial feature completeness assessment |

---

## Contributors

This assessment is based on analysis of the current codebase. For questions or clarifications, please open an issue.
