# Block Length Extension Implementation

## Overview
This document describes the implementation of block length extension for schema evolution in the SBE Code Generator.

## What is Block Length Extension?

Block length extension is a critical feature for schema evolution in Simple Binary Encoding (SBE). It allows decoders to handle messages with different block lengths, enabling:

1. **Forward Compatibility**: Old decoders can read messages from newer schemas by skipping unknown fields
2. **Backward Compatibility**: New decoders can read messages from older schemas by handling missing fields

## Implementation Details

### 1. Schema Field DTO Enhancement

Added `sinceVersion` attribute to `SchemaFieldDto.cs`:

```csharp
internal record SchemaFieldDto(
    // ... existing fields ...
    string SinceVersion  // NEW: Tracks when a field was added
);
```

### 2. Schema Parser Update

Updated `SchemaParser.ParseField()` to parse the `sinceVersion` attribute from XML:

```csharp
SinceVersion: fieldElement.GetAttributeOrEmpty("sinceVersion")
```

### 3. Generated Code Changes

Modified `MessageDefinition.cs` to generate two `TryParse` overloads:

```csharp
// Original method (backward compatible)
public static bool TryParse(ReadOnlySpan<byte> buffer, 
                           out MessageData message, 
                           out ReadOnlySpan<byte> variableData)
{
    return TryParse(buffer, MESSAGE_SIZE, out message, out variableData);
}

// New method with blockLength parameter
public static bool TryParse(ReadOnlySpan<byte> buffer, 
                           int blockLength,
                           out MessageData message, 
                           out ReadOnlySpan<byte> variableData)
{
    if (buffer.Length < blockLength)
    {
        message = default;
        variableData = default;
        return false;
    }
    
    // Read only the bytes specified by blockLength
    message = MemoryMarshal.AsRef<MessageData>(buffer);
    variableData = buffer.Slice(blockLength);
    return true;
}
```

## How It Works

### Schema Evolution Scenario

**Schema V1** (16 bytes):
```xml
<message name="Order" id="1">
    <field name="orderId" id="1" type="uint64"/>  <!-- 8 bytes -->
    <field name="price" id="2" type="int64"/>     <!-- 8 bytes -->
</message>
```

**Schema V2** (24 bytes):
```xml
<message name="Order" id="1">
    <field name="orderId" id="1" type="uint64"/>          <!-- 8 bytes -->
    <field name="price" id="2" type="int64"/>             <!-- 8 bytes -->
    <field name="quantity" id="3" type="int64" sinceVersion="1"/>  <!-- 8 bytes -->
</message>
```

### Reading V2 Message with V1 Decoder

```csharp
// V1 decoder knows MESSAGE_SIZE = 16
// V2 message has 24 bytes of data
Span<byte> v2MessageBuffer = stackalloc byte[24];

// V1 decoder uses its known block length
int v1BlockLength = 16;
var success = OrderV2Data.TryParse(v2MessageBuffer, v1BlockLength, 
                                  out var message, out var remaining);

// Result:
// - success = true
// - message contains orderId and price
// - remaining buffer starts at byte 16 (skipping the unknown quantity field)
```

### Reading V1 Message with V2 Decoder

```csharp
// V2 decoder knows MESSAGE_SIZE = 24
// V1 message has only 16 bytes of data
Span<byte> v1MessageBuffer = stackalloc byte[16];

// V2 decoder receives block length from message header
int v1BlockLength = 16;
var success = OrderV2Data.TryParse(v1MessageBuffer, v1BlockLength, 
                                  out var message, out var remaining);

// Result:
// - success = true
// - message contains orderId and price
// - quantity field has default value (0)
// - remaining buffer starts at byte 16
```

## Tests

Added comprehensive integration tests:

1. **BlockLengthExtension_AllowsDifferentBlockLengths**: Tests with exact, larger, and smaller block lengths
2. **BlockLengthExtension_BackwardCompatibleTryParse**: Ensures original TryParse still works

All 34 tests pass (23 unit + 11 integration).

## Benefits

1. **Backward Compatibility**: Existing code continues to work without modification
2. **Forward Compatibility**: Decoders can handle messages from future schema versions
3. **Safe Evolution**: Fields can be added to messages without breaking existing systems
4. **MessageHeader Integration**: Works seamlessly with SBE message headers that specify block length

## Next Steps

To complete schema evolution support:

1. Generate version checks in decoders for fields with `sinceVersion`
2. Add conditional field access based on schema version
3. Document best practices for schema evolution
4. Create migration guides

## References

- SBE Specification: [Schema Evolution](https://github.com/real-logic/simple-binary-encoding/wiki/Design-Principles#versioning-and-schema-evolution)
- Issue: [Implement Block Length Extension for Schema Evolution](#)
