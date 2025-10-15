# Byte Order (Endianness) Support

## Overview

The SBE Code Generator now fully supports byte order (endianness) configuration through the `byteOrder` attribute in SBE schemas. This ensures compatibility with both little-endian and big-endian systems and protocols.

## Schema Configuration

Specify the byte order in your schema's `messageSchema` element:

```xml
<!-- Little-endian (default) -->
<sbe:messageSchema byteOrder="littleEndian" ...>
    ...
</sbe:messageSchema>

<!-- Big-endian (for network protocols) -->
<sbe:messageSchema byteOrder="bigEndian" ...>
    ...
</sbe:messageSchema>
```

If the `byteOrder` attribute is not specified, the generator defaults to `littleEndian`.

## Generated Code

The generator creates an `EndianHelpers` class with methods for reading and writing values with proper byte order:

### Reading Methods

```csharp
// Little-endian reading
short value = EndianHelpers.ReadInt16LittleEndian(buffer);
int value = EndianHelpers.ReadInt32LittleEndian(buffer);
long value = EndianHelpers.ReadInt64LittleEndian(buffer);

// Big-endian reading
short value = EndianHelpers.ReadInt16BigEndian(buffer);
int value = EndianHelpers.ReadInt32BigEndian(buffer);
long value = EndianHelpers.ReadInt64BigEndian(buffer);
```

### Writing Methods

```csharp
// Little-endian writing
EndianHelpers.WriteInt16LittleEndian(buffer, value);
EndianHelpers.WriteInt32LittleEndian(buffer, value);
EndianHelpers.WriteInt64LittleEndian(buffer, value);

// Big-endian writing
EndianHelpers.WriteInt16BigEndian(buffer, value);
EndianHelpers.WriteInt32BigEndian(buffer, value);
EndianHelpers.WriteInt64BigEndian(buffer, value);
```

### Byte Swapping

For converting between byte orders:

```csharp
short swapped = EndianHelpers.ReverseBytes(originalValue);
int swapped = EndianHelpers.ReverseBytes(originalValue);
long swapped = EndianHelpers.ReverseBytes(originalValue);
```

### Platform Detection

Check the current platform's endianness:

```csharp
bool isLittle = EndianHelpers.IsLittleEndian;
```

## Implementation Details

### SchemaContext

The `SchemaContext` class stores the byte order configuration:

```csharp
public class SchemaContext
{
    public string ByteOrder { get; set; } = "littleEndian";
}
```

### Parsing

The `SBESourceGenerator` parses the `byteOrder` attribute during schema loading:

```csharp
var messageSchemaNode = d.DocumentElement;
if (messageSchemaNode != null)
{
    var byteOrderAttr = messageSchemaNode.GetAttribute("byteOrder");
    if (!string.IsNullOrEmpty(byteOrderAttr))
    {
        context.ByteOrder = byteOrderAttr;
    }
}
```

## Performance

All endianness conversion methods use `BinaryPrimitives` from `System.Buffers.Binary` and are marked with `[MethodImpl(MethodImplOptions.AggressiveInlining)]` for optimal performance.

On platforms where the schema byte order matches the platform byte order, the overhead is minimal (a simple read/write). When byte swapping is needed, the `BinaryPrimitives` methods provide highly optimized implementations.

## Testing

The implementation includes comprehensive tests:

### Unit Tests
- `EndianTests.cs`: Validates parsing of `byteOrder` attribute
- Tests for default values, explicit settings, and missing attributes

### Integration Tests
- `EndianIntegrationTests.cs`: Validates encoding/decoding operations
- Tests for all data types (int16, int32, int64, uint16, uint32, uint64)
- Tests for both little-endian and big-endian operations
- Round-trip tests to ensure data integrity

## Example Usage

### Little-Endian Schema

```xml
<sbe:messageSchema byteOrder="littleEndian" ...>
    <sbe:message name="Order" id="1">
        <field name="orderId" id="1" type="uint64"/>
        <field name="price" id="2" type="int64"/>
    </sbe:message>
</sbe:messageSchema>
```

### Big-Endian Schema (Network Protocol)

```xml
<sbe:messageSchema byteOrder="bigEndian" ...>
    <sbe:message name="Order" id="1">
        <field name="orderId" id="1" type="uint64"/>
        <field name="price" id="2" type="int64"/>
    </sbe:message>
</sbe:messageSchema>
```

## Best Practices

1. **Explicitly specify byte order**: Even if using little-endian, specify `byteOrder="littleEndian"` for clarity
2. **Use big-endian for network protocols**: Network protocols traditionally use big-endian (network byte order)
3. **Use little-endian for file formats**: Most modern platforms are little-endian, making this more efficient
4. **Test both byte orders**: If your application supports multiple platforms, test with both configurations

## Compatibility

- **Platforms**: Works on all platforms (.NET Standard 2.0+)
- **SBE Specification**: Fully compliant with SBE byte order specification
- **Performance**: Zero overhead when schema byte order matches platform byte order

## References

- [SBE Specification - Byte Order](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding)
- [BinaryPrimitives Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives)
- [BitConverter.IsLittleEndian](https://learn.microsoft.com/en-us/dotnet/api/system.bitconverter.islittleendian)
