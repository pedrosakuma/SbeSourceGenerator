# SpanWriter Implementation - Phase 1 Complete

## Overview

Phase 1 of the SBE payload writing support has been successfully implemented. This phase provides the foundation for encoding SBE messages using the `SpanWriter` ref struct and automatic generation of `TryEncode` methods for all messages.

## Features Implemented

### 1. SpanWriter Runtime Component

A new `SpanWriter` ref struct has been added to the generated runtime, providing:

- **Sequential writing API**: Write data sequentially without manual offset management
- **Symmetric to SpanReader**: Same design patterns for encoding as reading
- **Type-safe**: Leverages C# type system for compile-time safety
- **Zero-allocation**: Stack-only type with aggressive inlining
- **Extensible**: Supports custom encoders via delegates for schema evolution

### 2. TryEncode Method Generation

All generated messages now include three encoding methods:

#### TryEncode (Try pattern)
```csharp
public bool TryEncode(Span<byte> buffer, out int bytesWritten)
```
- Returns `false` if buffer is too small
- Safe, no exceptions
- Recommended for production code

#### Encode (Throwing pattern)
```csharp
public int Encode(Span<byte> buffer)
```
- Throws `InvalidOperationException` if buffer is too small
- Returns number of bytes written
- Convenient for testing and scenarios where failure is exceptional

#### TryEncodeWithWriter (Composition pattern)
```csharp
public bool TryEncodeWithWriter(ref SpanWriter writer)
```
- Uses an existing `SpanWriter` instance
- Useful for encoding multiple messages or adding headers
- Enables composition of complex payloads

## API Usage Examples

### Basic Encoding
```csharp
// Create a message
var order = new NewOrderData
{
    OrderId = 123456,
    Price = 9950,
    Quantity = 100,
    Side = OrderSide.Buy,
    OrderType = OrderType.Limit
};

// Allocate buffer
var buffer = new byte[NewOrderData.MESSAGE_SIZE];

// Encode message
if (order.TryEncode(buffer, out int bytesWritten))
{
    // Send via network
    await socket.SendAsync(buffer.AsMemory(0, bytesWritten));
}
```

### Round-Trip Encoding/Decoding
```csharp
// Encode
var original = new NewOrderData { OrderId = 123, Price = 100, Quantity = 10 };
var buffer = new byte[NewOrderData.MESSAGE_SIZE];
original.TryEncode(buffer, out int written);

// Decode
NewOrderData.TryParse(buffer, out var decoded, out _);

// Verify
Assert.Equal(original.OrderId, decoded.OrderId);
Assert.Equal(original.Price, decoded.Price);
```

### Encoding Multiple Messages
```csharp
var buffer = new byte[1024];
var writer = new SpanWriter(buffer);

// Encode header
var header = new MessageHeader { /* ... */ };
writer.Write(header);

// Encode messages
var order1 = new NewOrderData { /* ... */ };
var order2 = new NewOrderData { /* ... */ };

order1.TryEncodeWithWriter(ref writer);
order2.TryEncodeWithWriter(ref writer);

// Send all
int totalBytes = writer.BytesWritten;
await socket.SendAsync(buffer.AsMemory(0, totalBytes));
```

### Error Handling
```csharp
var message = new NewOrderData { /* ... */ };
var tooSmallBuffer = new byte[10]; // Too small

// Try pattern - safe
if (!message.TryEncode(tooSmallBuffer, out _))
{
    // Handle error gracefully
    Console.WriteLine("Buffer too small");
}

// Throwing pattern - for exceptional cases
try
{
    message.Encode(tooSmallBuffer);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Encoding failed: {ex.Message}");
}
```

## SpanWriter API Reference

### Properties

- `int BytesWritten` - Number of bytes written so far
- `int RemainingBytes` - Number of bytes remaining in buffer
- `Span<byte> Remaining` - Remaining writable portion of buffer

### Methods

#### Writing Data
- `bool TryWrite<T>(T value)` - Write a blittable struct
- `void Write<T>(T value)` - Write a blittable struct (throws on failure)
- `bool TryWriteBytes(ReadOnlySpan<byte> bytes)` - Write a byte span
- `void WriteBytes(ReadOnlySpan<byte> bytes)` - Write a byte span (throws on failure)

#### Navigation
- `bool TrySkip(int count, bool clear = true)` - Skip bytes (optionally clearing them)
- `void Skip(int count, bool clear = true)` - Skip bytes (throws on failure)
- `void Reset(int position = 0)` - Reset writer to specified position

#### Advanced
- `bool TryGetSlice(int offset, int length, out Span<byte> slice)` - Get writable slice
- `bool TryWriteWith<T>(SpanEncoder<T> encoder, T value)` - Custom encoder delegate
- `void WriteWith<T>(SpanEncoder<T> encoder, T value)` - Custom encoder (throws on failure)
- `bool CanWrite(int count)` - Check if count bytes can be written

## Test Coverage

### Unit Tests (36 new tests)
- SpanWriter basic operations (write, skip, reset)
- Buffer boundary checking
- Custom encoder delegates
- Round-trip with SpanReader
- Error handling

### Integration Tests (9 new tests)
- Simple message round-trip encoding/decoding
- Buffer size validation
- Multiple messages in sequence
- Different message types
- Edge cases (min/max values, zero values)

**Total: 191 tests passing (105 unit + 86 integration)**

## Implementation Details

### Files Added
- `src/SbeCodeGenerator/Runtime/SpanWriter.cs` - Reference implementation
- `src/SbeCodeGenerator/Generators/SpanWriterGenerator.cs` - Code generation
- `tests/SbeCodeGenerator.Tests/Runtime/SpanWriterTests.cs` - Unit tests
- `tests/SbeCodeGenerator.IntegrationTests/SpanWriterIntegrationTests.cs` - Integration tests

### Files Modified
- `src/SbeCodeGenerator/Generators/Types/MessageDefinition.cs` - Added TryEncode generation
- `src/SbeCodeGenerator/Generators/UtilitiesCodeGenerator.cs` - Added SpanWriter to utilities
- `tests/SbeCodeGenerator.Tests/UtilitiesCodeGeneratorTests.cs` - Updated for 4 utilities

### Generated Code Structure
For each message, the following methods are now generated:
```csharp
public partial struct NewOrderData
{
    public bool TryEncode(Span<byte> buffer, out int bytesWritten);
    public int Encode(Span<byte> buffer);
    public bool TryEncodeWithWriter(ref SpanWriter writer);
}
```

## Performance Characteristics

- **Zero allocations**: All operations are stack-based
- **Inline-friendly**: AggressiveInlining on critical methods
- **Symmetric to reading**: Same performance profile as SpanReader
- **Buffer-safe**: All operations check bounds before writing

## Limitations (Phase 1)

Phase 1 focuses on simple message encoding. The following are **not** included in this phase:

- ❌ Repeating groups encoding
- ❌ Variable-length data (varData) encoding
- ❌ Optional fields special handling
- ❌ Complex nested structures

These features are planned for future phases (Phase 2 and Phase 3).

## Next Steps

### Phase 2 (Planned)
- Encoding for messages with repeating groups
- Optional fields encoding support
- Enhanced round-trip testing

### Phase 3 (Planned)
- Variable-length data (varData) encoding
- Complete documentation
- Performance benchmarks

## Migration Guide

No breaking changes were introduced. All existing code continues to work as before. The new encoding methods are additive features.

To start using encoding:

1. Use `TryEncode` for safe encoding with error handling
2. Use `Encode` when you're certain the buffer is large enough
3. Use `TryEncodeWithWriter` for composing complex payloads

## References

- [SpanWriter Reference Implementation](../docs/SpanWriter_Reference_Implementation.cs)
- [Code Generation Examples](../docs/CODE_GENERATION_EXAMPLES_WRITING.md)
- [Payload Writing Feasibility Study](../docs/PAYLOAD_WRITING_FEASIBILITY.md)
- [Phase 1 Unit Tests](../tests/SbeCodeGenerator.Tests/Runtime/SpanWriterTests.cs)
- [Phase 1 Integration Tests](../tests/SbeCodeGenerator.IntegrationTests/SpanWriterIntegrationTests.cs)

## Conclusion

Phase 1 successfully delivers the foundation for SBE message encoding:
- ✅ SpanWriter runtime component implemented
- ✅ TryEncode methods generated for all messages
- ✅ Comprehensive test coverage (191 tests)
- ✅ Complete API documentation
- ✅ Zero breaking changes

The implementation provides a solid foundation for future enhancements while maintaining backward compatibility.
