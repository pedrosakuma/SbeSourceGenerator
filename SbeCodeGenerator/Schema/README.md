# Schema DTOs and Parsing

This directory contains Data Transfer Objects (DTOs) and parsing utilities for XML schema processing.

## Purpose

These DTOs and helpers were introduced to:
- Eliminate direct `XmlElement.GetAttribute()` calls from generator builders
- Centralize XML parsing logic in one place
- Improve code maintainability and testability
- Provide type-safe representations of schema constructs

## Files

### DTOs (Data Transfer Objects)
- **SchemaFieldDto.cs**: Represents a field element from XML schema
- **SchemaCompositeDto.cs**: Represents a composite type definition
- **SchemaEnumDto.cs**: Represents an enum or set type definition
- **SchemaTypeDto.cs**: Represents a simple type definition
- **SchemaGroupDto.cs**: Represents a group element
- **SchemaDataDto.cs**: Represents a data element
- **SchemaMessageDto.cs**: Represents a message definition

### Utilities
- **SchemaParser.cs**: Static utility class with methods to parse XML elements into DTOs

## Usage Example

### Before (Direct XML Access)
```csharp
var generator = new EnumFlagsDefinition(
    ns,
    typeNode.GetAttribute("name").FirstCharToUpper(),
    typeNode.GetAttribute("description"),
    ToNativeType(typeNode.GetAttribute("encodingType")),
    // ...
);
```

### After (Using DTOs)
```csharp
var enumDto = SchemaParser.ParseEnum(typeNode);
var generator = new EnumFlagsDefinition(
    ns,
    enumDto.Name.FirstCharToUpper(),
    enumDto.Description,
    ToNativeType(enumDto.EncodingType),
    // ...
);
```

## Benefits

1. **Single Source of Truth**: All XML parsing happens in SchemaParser
2. **Type Safety**: DTOs are strongly typed and validated
3. **Testability**: DTOs can be easily created for unit tests
4. **Maintainability**: Changes to XML schema structure only affect SchemaParser
5. **Readability**: Generator methods work with domain objects, not XML infrastructure

## Design Principles

- **Immutability**: All DTOs are C# records and immutable
- **Completeness**: DTOs capture all relevant attributes from XML elements
- **Consistency**: All parsing follows the same pattern
- **Null Safety**: Helper methods in XmlParsingHelpers ensure safe attribute access
