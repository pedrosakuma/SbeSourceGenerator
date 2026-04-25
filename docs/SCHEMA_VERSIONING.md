# Schema Versioning and Evolution Guide

## Overview

The SBE Code Generator now supports field-level versioning using the `sinceVersion` attribute, enabling safe schema evolution and maintaining backward/forward compatibility between different versions of your message schemas.

## How It Works

### The sinceVersion Attribute

The `sinceVersion` attribute on field elements indicates the schema version in which a field was introduced:

```xml
<message name="Order" id="1" description="Order message">
    <field name="orderId" id="1" type="uint64"/>
    <field name="price" id="2" type="int64"/>
    <field name="quantity" id="3" type="int64" sinceVersion="1"/>
    <field name="side" id="4" type="uint8" sinceVersion="2"/>
</message>
```

In this example:
- `orderId` and `price` are present since version 0 (the initial version)
- `quantity` was added in version 1
- `side` was added in version 2

### Block Length and Version Detection

SBE uses the `blockLength` field in the message header to determine which fields are present in a message:

```csharp
struct MessageHeader {
    ushort blockLength;  // Number of bytes in the message body
    ushort templateId;
    ushort schemaId;
    ushort version;
}
```

The `blockLength` indicates how many bytes of field data follow the header. Decoders use this to:
1. Read only the fields that were written
2. Skip fields that weren't present in older schema versions
3. Avoid reading beyond the available data

## Generated Code

### Field Documentation

Fields with `sinceVersion` include version information in their XML documentation:

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

### TryParse Methods

The generated `TryParse` method returns a zero-copy `MessageDataReader` ref struct:

```csharp
public static bool TryParse(ReadOnlySpan<byte> buffer, int blockLength, 
                           out OrderDataReader reader)
{
    var spanReader = new SpanReader(buffer);
    
    // TryReadBlock advances by blockLength, not sizeof(T)
    // - blockLength < sizeof: partial read, trailing fields zeroed (backward compat)
    // - blockLength > sizeof: full read, skips extra bytes (forward compat)
    if (!spanReader.TryReadBlock<OrderData>(blockLength, out _))
    {
        reader = default;
        return false;
    }
    
    reader = new OrderDataReader(buffer, blockLength);
    return true;
}
```

> **v0.9.0**: `TryReadBlock<T>` replaces the previous manual `TryRead<T>` + skip/slice logic. It handles all schema evolution cases in a single method call, including the edge case where `blockLength == 0`.

## Usage Examples

### Example 1: Reading Messages from Different Schema Versions

```csharp
// V1 message: orderId (8 bytes) + price (8 bytes) = 16 bytes
Span<byte> v1Message = /* ... from wire or storage ... */;
int v1BlockLength = 16;

// V3 decoder can read V1 messages
var success = OrderData.TryParse(v1Message, v1BlockLength, out var order);

// Fields present in V1
Console.WriteLine($"OrderId: {order.Data.OrderId}");  // ✓ Present
Console.WriteLine($"Price: {order.Data.Price}");      // ✓ Present

// Fields added in later versions have default values
Console.WriteLine($"Quantity: {order.Data.Quantity}"); // 0 (not in V1)
Console.WriteLine($"Side: {order.Data.Side}");         // 0 (not in V2)
```

### Example 2: Writing Messages with the Latest Schema

```csharp
// Create a V3 message with all fields
Span<byte> buffer = stackalloc byte[OrderData.MESSAGE_SIZE];
ref OrderData order = ref MemoryMarshal.AsRef<OrderData>(buffer);

order.OrderId = 12345;
order.Price = 9950;
order.Quantity = 100;
order.Side = 1;  // Buy

// Send with V3 block length
SendMessage(buffer, OrderData.MESSAGE_SIZE);
```

### Example 3: Forward Compatibility

Old decoders (V1) can read newer messages (V3) by using the blockLength from the message header:

```csharp
// V3 message with all fields
Span<byte> v3Message = /* ... 25 bytes ... */;

// V1 decoder only knows about blockLength=16
int v1BlockLength = 16;
var success = OrderData.TryParse(v3Message, v1BlockLength, out var order);

// V1 decoder reads its known fields
Console.WriteLine($"OrderId: {order.Data.OrderId}");  // ✓ Read correctly
Console.WriteLine($"Price: {order.Data.Price}");      // ✓ Read correctly

// Unknown fields (quantity, side) are skipped
// They're still in the struct but V1 code shouldn't access them
// reader.BytesConsumed will be 25 bytes (the blockLength)
```

## Schema Evolution Best Practices

### 1. Only Add Fields at the End

New fields should always be added at the end of the message:

```xml
<!-- ✓ Good: New field at the end -->
<message name="Order" id="1">
    <field name="orderId" id="1" type="uint64"/>
    <field name="price" id="2" type="int64"/>
    <field name="quantity" id="3" type="int64" sinceVersion="1"/>  <!-- Added later -->
</message>

<!-- ✗ Bad: Inserting field in the middle would break offsets -->
```

### 2. Never Remove or Reorder Fields

Once a field is added, it should never be removed or reordered:

```xml
<!-- ✗ Bad: Don't do this -->
<message name="Order" id="1">
    <field name="price" id="2" type="int64"/>          <!-- Moved up -->
    <field name="orderId" id="1" type="uint64"/>       <!-- Moved down -->
</message>
```

### 3. Use sinceVersion for New Fields

Always specify `sinceVersion` when adding new fields:

```xml
<!-- ✓ Good: sinceVersion specified -->
<field name="quantity" id="3" type="int64" sinceVersion="1"/>

<!-- ⚠ Warning: Missing sinceVersion implies version 0 -->
<field name="quantity" id="3" type="int64"/>
```

### 4. Deprecate Instead of Remove

Use the `deprecated` attribute to mark fields that should no longer be used:

```xml
<field name="legacyField" id="5" type="int32" 
       sinceVersion="1" 
       deprecated="2"
       description="Use newField instead"/>
```

### 5. Increment Schema Version

Update the schema's `version` attribute when adding new fields:

```xml
<sbe:messageSchema xmlns:sbe="http://fixprotocol.io/2016/sbe"
                   package="myapp"
                   id="1"
                   version="2"          <!-- Increment this -->
                   semanticVersion="2.0">
```

## Block Length Calculation

The block length is calculated automatically based on field offsets and sizes:

```
Schema V1 (version 0):
  orderId (offset 0, size 8)  → 0-7
  price   (offset 8, size 8)  → 8-15
  Block Length = 16 bytes

Schema V2 (version 1):
  orderId  (offset 0, size 8)  → 0-7
  price    (offset 8, size 8)  → 8-15
  quantity (offset 16, size 8) → 16-23  [sinceVersion="1"]
  Block Length = 24 bytes

Schema V3 (version 2):
  orderId  (offset 0, size 8)  → 0-7
  price    (offset 8, size 8)  → 8-15
  quantity (offset 16, size 8) → 16-23  [sinceVersion="1"]
  side     (offset 24, size 1) → 24     [sinceVersion="2"]
  Block Length = 25 bytes
```

### `{Msg}VersionMap` (Issue #146)

For any message that has more than one version, the generator emits a static `{Msg}VersionMap`
class in the canonical V0 namespace. It exposes a small `(int BlockLength, int Version)[]`
array in declaration order plus a `[AggressiveInlining]` `TryGetVersion(blockLength, out version)`
helper. Use it on the receive path to pick the right `{Msg}V{N}Data.TryParse` overload from a
header you just decoded:

```csharp
if (!EvolvingOrderVersionMap.TryGetVersion(header.BlockLength, out int version))
{
    // Unknown blockLength — log / drop / fall back to forward-compat parse.
    return;
}
switch (version)
{
    case 0: EvolvingOrderData.TryParse(payload, header.BlockLength, out var v0); break;
    case 1: V1.EvolvingOrderData.TryParse(payload, header.BlockLength, out var v1); break;
    case 2: V2.EvolvingOrderData.TryParse(payload, header.BlockLength, out var v2); break;
}
```

The lookup is allocation-free and a linear scan over a tiny array, so it is faster than a
`Dictionary<int,int>` for the typical 2–5 version case.

## Wire Format Compatibility

### Message Format

```
┌──────────────────┬──────────────────┬─────────────────┐
│  Message Header  │  Message Body    │  Variable Data  │
│   (8 bytes)      │  (blockLength)   │  (remaining)    │
└──────────────────┴──────────────────┴─────────────────┘
```

### Example: V1 Message Read by V3 Decoder

```
V1 Message on wire:
┌──────────────────┬──────────────────┐
│  Header          │  Body (16 bytes) │
│  blockLength=16  │  orderId, price  │
└──────────────────┴──────────────────┘

V3 Decoder processes (TryReadBlock):
1. Reads header: blockLength = 16
2. TryReadBlock<OrderData>(blockLength=16):
   - sizeof(OrderData) = 25 > blockLength = 16
   - Partial read: copies 16 bytes, zero-pads remaining 9
   - Advances reader by 16 bytes
3. Result: orderId and price populated, quantity and side = 0
```

### Example: V3 Message Read by V1 Decoder

```
V3 Message on wire:
┌──────────────────┬──────────────────┐
│  Header          │  Body (25 bytes) │
│  blockLength=25  │  all fields      │
└──────────────────┴──────────────────┘

V1 Decoder processes (TryReadBlock):
1. Reads header: blockLength = 25
2. TryReadBlock<OrderData>(blockLength=25):
   - sizeof(OrderData) = 16 <= blockLength = 25
   - Full read: reads 16 bytes for struct
   - Skips remaining 9 bytes (25 - 16)
   - Advances reader by 25 bytes
3. Result: orderId and price populated, extra fields skipped
```

## Testing Schema Evolution

The integration tests in `VersioningIntegrationTests.cs` demonstrate various schema evolution scenarios:

1. **V3 decoder reading V1 messages** - Backward compatibility
2. **V3 decoder reading V2 messages** - Backward compatibility
3. **V1 decoder reading V3 messages** - Forward compatibility
4. **Documentation generation** - Verifying "Since version" comments
5. **Variable data handling** - Schema evolution with varData

Run these tests to verify your schema evolution works correctly:

```bash
dotnet test --filter "FullyQualifiedName~VersioningIntegrationTests"
```

## Limitations and Considerations

### 1. Blittable Struct Approach

The current implementation uses blittable structs with fixed layouts. This means:

- **Pros**: Very efficient, zero-copy reading, good performance
- **Cons**: All fields are always in the struct, even if not present in the wire format

Fields from newer versions will have default/zero values when reading older messages.

### 2. No Runtime Version Checking

The generated code doesn't include runtime methods to check if a field was actually present. To determine field presence, you need to:

```csharp
bool IsFieldPresent(int fieldOffset, int blockLength)
{
    return fieldOffset < blockLength;
}

// Usage
if (IsFieldPresent(16, blockLength))  // 16 = offset of quantity field
{
    // quantity was present in the message
    var qty = order.Quantity;
}
```

### 3. Default Values

Fields that weren't present in the wire format will have C# default values:
- Numeric types: `0`
- Enums: `0` (first value)
- Structs: all fields set to their defaults

Ensure your protocol can distinguish between "not present" and "present with value 0" if needed.

## Migration from Version 0 to Version 1

If you have existing messages without versioning:

1. **Current schema becomes version 0**:
   ```xml
   <message name="Order" id="1">
       <field name="orderId" id="1" type="uint64"/>
       <field name="price" id="2" type="int64"/>
   </message>
   ```

2. **Add new fields with sinceVersion="1"**:
   ```xml
   <message name="Order" id="1" version="1">
       <field name="orderId" id="1" type="uint64"/>
       <field name="price" id="2" type="int64"/>
       <field name="quantity" id="3" type="int64" sinceVersion="1"/>
   </message>
   ```

3. **Update encoders** to write the new blockLength
4. **Update decoders** to use the blockLength-aware TryParse

## References

- [SBE Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding)
- [Integration Tests](../tests/SbeCodeGenerator.IntegrationTests/VersioningIntegrationTests.cs)
- [Test Schema](../tests/SbeCodeGenerator.IntegrationTests/TestSchemas/versioning-test-schema.xml)
