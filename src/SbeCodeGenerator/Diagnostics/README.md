# SBE Source Generator Diagnostics

This directory contains diagnostic descriptors for the SBE source generator.

## Purpose

Provides compile-time diagnostics for:
- Malformed XML schemas
- Invalid attribute values
- Missing required attributes
- Unsupported constructs
- Unresolved type references
- Non-native byte order
- Invalid numeric constraints
- Unknown primitive type fallbacks

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

### SBE007: Non-Native Byte Order
**Severity**: Warning  
**Triggered when**: The schema specifies a `byteOrder` that differs from the platform's native endianness (little-endian)  
**Example**: `<messageSchema byteOrder="bigEndian" .../>`  
**Resolution**: The generator does not emit byte-swap logic. Multi-byte fields may be read/written incorrectly on this platform. Consider using little-endian byte order or handling endianness manually.

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
