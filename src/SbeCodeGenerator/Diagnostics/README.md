# SBE Source Generator Diagnostics

This directory contains diagnostic descriptors for the SBE source generator.

## Purpose

Provides compile-time diagnostics for:
- Malformed XML schemas
- Invalid attribute values
- Missing required attributes
- Unsupported constructs
- Unresolved type references
- Big-endian byte order configuration
- Invalid numeric constraints
- Unknown primitive type fallbacks
- Invalid MSBuild property values

## Diagnostic Codes

### SBE001: Invalid Integer Attribute Value
**Severity**: Error  
**Triggered when**: An XML attribute expected to contain an integer has an invalid value  
**Example**: `<type length="NotANumber" .../>`  
**Resolution**: Ensure the attribute contains a valid integer value

### SBE002: Missing Required Attribute
**Severity**: Error  
**Triggered when**: A required XML attribute is missing or empty  
**Example**: `<field type="" .../>`  
**Resolution**: Provide a value for the required attribute

### SBE003: Invalid Enum Flag Value
**Severity**: Error  
**Triggered when**: An enum flag value cannot be parsed as an integer for bit-shift operations  
**Example**: `<choice name="Flag1">NotANumber</choice>`  
**Resolution**: Ensure the choice value is a valid integer (0-31 for typical flag enums)

### SBE004: Malformed Schema
**Severity**: Error  
**Triggered when**: The XML schema file cannot be loaded or parsed  
**Example**: Malformed XML, invalid encoding, etc.  
**Resolution**: Fix the XML syntax or encoding issues

### SBE005: Unsupported Construct
**Severity**: Warning  
**Triggered when**: The schema contains a valid but unsupported construct  
**Example**: Future schema features not yet implemented  
**Resolution**: Remove the unsupported feature or wait for generator support

### SBE006: Invalid Type Length
**Severity**: Error  
**Triggered when**: A type definition has an invalid length attribute  
**Example**: `<type name="MyType" primitiveType="char" length="abc"/>`  
**Resolution**: Provide a valid integer for the length attribute

### SBE007: Big-Endian Schema with Conditional Byte Swap
**Severity**: Info  
**Triggered when**: The schema specifies `byteOrder="bigEndian"` and no `SbeAssumeHostEndianness` MSBuild property is set  
**Example**: `<messageSchema byteOrder="bigEndian" .../>`  
**Resolution**: Generated multi-byte field properties include a `BitConverter.IsLittleEndian` runtime check. Set `<SbeAssumeHostEndianness>LittleEndian</SbeAssumeHostEndianness>` in your project file to eliminate the branch.

### SBE008: Unresolved Type Reference
**Severity**: Error  
**Triggered when**: A type reference in the schema cannot be resolved to a known primitive or custom type  
**Example**: `<field name="price" type="UnknownType" .../>`  
**Resolution**: Ensure the type is defined in the `<types>` section before it is referenced, or that it maps to a valid SBE primitive type.

### SBE009: Invalid Numeric Constraint
**Severity**: Warning  
**Triggered when**: A `minValue` or `maxValue` attribute contains a non-numeric value  
**Example**: `<field name="qty" type="uint32" minValue="abc" .../>`  
**Resolution**: Provide valid numeric literals for constraint attributes. Non-numeric constraints are ignored during validation code generation.

### SBE010: Unknown Primitive Type Fallback
**Severity**: Warning  
**Triggered when**: A primitive type used in the schema does not have a known mapping in the type catalog for length or null sentinel lookups  
**Example**: A custom or misspelled type name used where a primitive is expected  
**Resolution**: Verify the primitive type name in the schema. The generator uses fallback values (0 for length, `default` for null sentinel) which may produce incorrect code.

### SBE011: Set Choice Bit Position Exceeds Encoding Width
**Severity**: Error  
**Triggered when**: A `<choice>` element in a `<set>` has a bit position that exceeds the maximum for the encoding type  
**Example**: `<choice name="Flag">64</choice>` on a `uint64`-encoded set (max bit 63)  
**Resolution**: Ensure choice bit positions are within range `0` to `(encodingWidth * 8 - 1)`.

### SBE012: Invalid SbeAssumeHostEndianness Value
**Severity**: Warning  
**Triggered when**: The `SbeAssumeHostEndianness` MSBuild property has a value other than `LittleEndian` or `BigEndian`  
**Example**: `<SbeAssumeHostEndianness>Invalid</SbeAssumeHostEndianness>`  
**Resolution**: Set the value to `LittleEndian` or `BigEndian`, or remove the property entirely for automatic detection.

### SBE013: Duplicate Type Name
**Severity**: Warning  
**Triggered when**: Two `<type>`, `<enum>`, `<set>`, or `<composite>` declarations share the same name within a single schema  
**Example**: Two `<enum name="Side">` blocks in the same `<types>` section  
**Resolution**: Rename one of the duplicates so each generated identifier is unique. The generator uses the last definition encountered.

### SBE014: sinceVersion Exceeds Schema Version
**Severity**: Warning  
**Triggered when**: A field's `sinceVersion` attribute is greater than the schema's `version` attribute  
**Example**: `<field sinceVersion="3"/>` in a schema declared as `version="1"`  
**Resolution**: Either bump the schema version, or correct the field's `sinceVersion`. Such fields are unreachable in any generated message version.

### SBE015: Duplicate Generated Source Suppressed
**Severity**: Warning  
**Triggered when**: The generator produces two source files with the same `hintName` (Roslyn requires uniqueness). The first is kept; the duplicate is suppressed and generation continues.  
**Example**: A schema declares two `<enum name="Side">` blocks; the second pass attempts `AddSource("…/Enums/Side.cs", …)` again. Without the suppression, Roslyn would throw `ArgumentException`, abort the generator phase, and produce a cascade of `CS0246` errors against partially-emitted files.  
**Resolution**: Resolve the underlying duplication in the schema (commonly a duplicate type name — see also `SBE013`) or fix the upstream code path that emitted the second source.

## Usage

Diagnostics are automatically reported during source generation. When you build a project that includes an invalid SBE schema as an additional file, you'll see these diagnostics in:

- Build output
- Visual Studio Error List
- VS Code Problems panel
- Command-line build logs

## Implementation Notes

- Diagnostics use `Location.None` as source generators don't have access to the original XML file locations
- The generator gracefully handles errors by using fallback values and continuing generation
- Each generator phase (Types, Messages, Utilities, Validation) runs in isolation — a failure in one phase does not block the others
- Test code uses `default(SourceProductionContext)` which has special handling to skip diagnostic reporting
