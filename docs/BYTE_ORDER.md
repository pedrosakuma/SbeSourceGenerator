# Byte Order (Endianness) Support

## Overview

The SBE Code Generator supports both `littleEndian` and `bigEndian` byte order via the `byteOrder` attribute in SBE schemas. For big-endian schemas, multi-byte fields are generated with private backing fields and public properties that perform byte reversal on access — zero overhead for little-endian schemas.

## Schema Configuration

```xml
<!-- Little-endian (default) — no conversion overhead -->
<sbe:messageSchema byteOrder="littleEndian" ...>
</sbe:messageSchema>

<!-- Big-endian (network protocols) — properties handle conversion -->
<sbe:messageSchema byteOrder="bigEndian" ...>
</sbe:messageSchema>
```

If `byteOrder` is omitted, the generator defaults to `littleEndian`.

## Generated Code

### Little-endian schema (default)

Fields are generated as direct public fields — identical to the existing behavior:

```csharp
[FieldOffset(0)]
public uint Price;
[FieldOffset(4)]
public ulong Quantity;
```

### Big-endian schema

Multi-byte fields use private backing fields with public properties:

```csharp
[FieldOffset(0)]
private uint price;
public uint Price { get => BinaryPrimitives.ReverseEndianness(price); set => price = BinaryPrimitives.ReverseEndianness(value); }

[FieldOffset(4)]
private ulong quantity;
public ulong Quantity { get => BinaryPrimitives.ReverseEndianness(quantity); set => quantity = BinaryPrimitives.ReverseEndianness(value); }
```

Single-byte types (`byte`, `sbyte`, `char`) are always direct public fields — no conversion needed.

### Enum fields with multi-byte underlying types

Enum fields use a cast+reverse+cast pattern:

```csharp
[FieldOffset(0)]
private Side side;
public Side Side { get => (Side)BinaryPrimitives.ReverseEndianness((ushort)side); set => side = (Side)BinaryPrimitives.ReverseEndianness((ushort)value); }
```

### Float/double fields

Float and double fields use `BitConverter` for the intermediate int conversion:

```csharp
get => BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(price)))
```

## SbeAssumeHostEndianness Hint

By default, big-endian schemas generate a runtime `BitConverter.IsLittleEndian` check. You can eliminate this branch by specifying the target host endianness:

```xml
<PropertyGroup>
  <SbeAssumeHostEndianness>LittleEndian</SbeAssumeHostEndianness>
</PropertyGroup>
```

### Conversion modes

| Schema | Hint | Mode | Generated Code |
|--------|------|------|----------------|
| `littleEndian` | (none) | None | `public uint Price;` |
| `littleEndian` | `LittleEndian` | None | `public uint Price;` |
| `littleEndian` | `BigEndian` | AlwaysReverse | Direct `ReverseEndianness()` |
| `bigEndian` | (none) | Conditional | `BitConverter.IsLittleEndian ? Reverse() : field` |
| `bigEndian` | `LittleEndian` | AlwaysReverse | Direct `ReverseEndianness()` |
| `bigEndian` | `BigEndian` | None | `public uint Price;` |

## Performance

Benchmark results (Intel, .NET 9):

| Approach | Decode overhead | Code Size |
|----------|----------------|-----------|
| Direct field (LE schema) | 0% baseline | 87B |
| Property passthrough (hint match) | 0% | 87B |
| AlwaysReverse (hint mismatch) | ~30% (~0.3ns/field) | 118B |
| Conditional (no hint, BE schema) | ~33% (~0.3ns/field) | 118B |

> **Recommendation**: For big-endian schemas on known little-endian hosts, set `<SbeAssumeHostEndianness>LittleEndian</SbeAssumeHostEndianness>` to eliminate the runtime branch.

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| SBE007 | Info | Big-endian schema without hint — conditional check is used. Set `SbeAssumeHostEndianness` to optimize. |
| SBE012 | Warning | Invalid `SbeAssumeHostEndianness` value. Expected `LittleEndian` or `BigEndian`. |

## Architecture

The struct memory layout always matches wire format (via `MemoryMarshal.AsRef<T>()`). Properties convert on access — no change to `TryParse`/`TryEncode` mechanics. This means:

- Encoding: assign host-order value to property → setter converts to wire order → struct bytes are wire-ready
- Decoding: `MemoryMarshal.AsRef<T>()` overlays wire bytes → getter converts to host order on read
