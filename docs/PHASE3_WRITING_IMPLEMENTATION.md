# Phase 3 Implementation: Groups and VarData Encoding Support

## Overview

Phase 3 implements encoding support for **repeating groups** and **variable-length data (varData)** fields in SBE messages. This completes the encoding functionality for complex SBE features, enabling full round-trip encoding and decoding of sophisticated message structures.

## Implemented Features

### 1. BeginEncoding Method

For messages containing groups or varData fields, a `BeginEncoding` method is generated that:
- Encodes the fixed message fields
- Returns a `SpanWriter` positioned after the message header
- Enables sequential encoding of variable-length segments

**Generated Code:**
```csharp
public bool BeginEncoding(Span<byte> buffer, out SpanWriter writer)
{
    if (buffer.Length < MESSAGE_SIZE)
    {
        writer = default;
        return false;
    }
    
    writer = new SpanWriter(buffer);
    writer.Write(this);
    return true;
}
```

### 2. Group Encoding Methods

For each repeating group in a message, a static `TryEncode{GroupName}` method is generated:

**Generated Code:**
```csharp
public static bool TryEncodeBids(ref SpanWriter writer, ReadOnlySpan<BidsData> entries)
{
    // Write group header (block length + number of entries)
    var header = new GroupSizeEncoding
    {
        BlockLength = (ushort)BidsData.MESSAGE_SIZE,
        NumInGroup = (uint)entries.Length
    };
    
    if (!writer.TryWrite(header))
        return false;
    
    // Write each entry
    for (int i = 0; i < entries.Length; i++)
    {
        if (!writer.TryWrite(entries[i]))
            return false;
    }
    
    return true;
}
```

### 3. VarData Encoding Methods

For each varData field in a message, a static `TryEncode{FieldName}` method is generated:

**Generated Code:**
```csharp
public static bool TryEncodeSymbol(ref SpanWriter writer, ReadOnlySpan<byte> data)
{
    // Write length prefix (uint8 for VarString8)
    if (data.Length > 255)
        return false;
    
    if (!writer.TryWrite((byte)data.Length))
        return false;
    
    // Write data bytes
    if (!writer.TryWriteBytes(data))
        return false;
    
    return true;
}
```

## Usage Examples

### Encoding Messages with Repeating Groups

```csharp
// Create message and groups
var orderBook = new OrderBookData { InstrumentId = 42 };

var bids = new OrderBookData.BidsData[]
{
    new OrderBookData.BidsData { Price = 1000, Quantity = 100 },
    new OrderBookData.BidsData { Price = 1010, Quantity = 101 },
    new OrderBookData.BidsData { Price = 1020, Quantity = 102 }
};

var asks = new OrderBookData.AsksData[]
{
    new OrderBookData.AsksData { Price = 2000, Quantity = 200 },
    new OrderBookData.AsksData { Price = 2010, Quantity = 201 }
};

// Encode
Span<byte> buffer = stackalloc byte[1024];
orderBook.BeginEncoding(buffer, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);
OrderBookData.TryEncodeAsks(ref writer, asks);

int totalBytes = writer.BytesWritten;
// Send buffer[0..totalBytes] over network
```

### Encoding Messages with VarData

```csharp
// Create message
var order = new NewOrderData
{
    OrderId = 123,
    Price = 9950,
    Quantity = 100,
    Side = OrderSide.Buy,
    OrderType = OrderType.Limit
};

// Prepare varData
string symbol = "AAPL";
var symbolBytes = Encoding.UTF8.GetBytes(symbol);

// Encode
Span<byte> buffer = stackalloc byte[1024];
order.BeginEncoding(buffer, out var writer);
NewOrderData.TryEncodeSymbol(ref writer, symbolBytes);

int totalBytes = writer.BytesWritten;
```

### Round-Trip: Encode and Decode

```csharp
// Encode
var original = new OrderBookData { InstrumentId = 42 };
var bids = new[] { new BidsData { Price = 1000, Quantity = 100 } };

Span<byte> buffer = stackalloc byte[1024];
original.BeginEncoding(buffer, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);

// Decode fixed part
OrderBookData.TryParse(buffer, out var decoded, out var variableData);
Assert.Equal(42, decoded.InstrumentId);

// Decode groups
var decodedBids = new List<BidsData>();
decoded.ConsumeVariableLengthSegments(
    variableData,
    bid => decodedBids.Add(bid),
    ask => { /* process asks */ }
);

Assert.Equal(1000, decodedBids[0].Price.Value);
```

## Test Coverage

### New Integration Tests (11 tests)

**Group Encoding Tests:**
1. `GroupEncoding_TryEncodeBids_EncodesCorrectly` - Basic group encoding
2. `GroupEncoding_EmptyGroup_EncodesCorrectly` - Empty group handling
3. `GroupEncoding_TwoGroups_EncodesCorrectly` - Multiple groups
4. `GroupEncoding_InsufficientBuffer_ReturnsFalse` - Error handling
5. `CompleteMessageWithGroups_RoundTrip_PreservesData` - Full round-trip

**VarData Encoding Tests:**
6. `VarDataEncoding_TryEncodeSymbol_EncodesCorrectly` - Basic varData encoding
7. `VarDataEncoding_EmptyData_EncodesCorrectly` - Empty varData
8. `VarDataEncoding_TooLong_ReturnsFalse` - Length validation (max 255)
9. `VarDataEncoding_InsufficientBuffer_ReturnsFalse` - Error handling
10. `CompleteMessageWithVarData_RoundTrip_PreservesData` - Full round-trip

**API Tests:**
11. `BeginEncoding_InsufficientBuffer_ReturnsFalse` - BeginEncoding validation

### Test Results

```
Total: 214 tests
  Unit Tests: 105 ✅
  Integration Tests: 109 ✅ (up from 98)
  Failed: 0
```

## Implementation Details

### Files Modified

1. **`src/SbeCodeGenerator/Generators/Types/MessageDefinition.cs`**
   - Added `AppendVariableDataEncoding` method
   - Generates `BeginEncoding` for messages with groups/varData
   - Generates `TryEncode{GroupName}` for each group
   - Generates `TryEncode{FieldName}` for each varData field

### Files Added

1. **`tests/SbeCodeGenerator.IntegrationTests/GroupAndVarDataEncodingTests.cs`**
   - 11 comprehensive integration tests
   - Tests for groups, varData, and combined scenarios
   - Round-trip validation tests

## Design Decisions

### 1. Static Helper Methods

Group and varData encoding methods are generated as `static` methods rather than instance methods because:
- They operate on spans of data, not the message instance
- Allows encoding multiple groups/varData fields sequentially
- Consistent with SBE specification for variable-length data

### 2. BeginEncoding Pattern

The `BeginEncoding` method returns a `SpanWriter` to:
- Enable sequential encoding of multiple variable segments
- Maintain buffer position across multiple encoding operations
- Provide consistent API with `TryEncodeWithWriter`

### 3. TryEncode Pattern

All encoding methods follow the `Try` pattern (return `bool`) to:
- Avoid exceptions in hot paths
- Enable graceful error handling
- Match existing `TryParse` patterns

### 4. Length Validation for VarData

VarData encoding validates data length (max 255 for `VarString8`) to:
- Prevent buffer overruns
- Match SBE specification constraints
- Provide early error detection

## SBE Specification Compliance

Phase 3 implementation complies with SBE specification for:

✅ **Repeating Groups:**
- Group dimension encoding (blockLength + numInGroup)
- Sequential group entry encoding
- Support for empty groups
- Multiple groups per message

✅ **Variable-Length Data:**
- Length prefix encoding (uint8 for VarString8)
- Data bytes encoding
- Multiple varData fields per message

## Performance Characteristics

- **Zero allocations:** All operations use stack-based `Span<T>`
- **Single-pass encoding:** Groups and varData encoded sequentially
- **Minimal overhead:** Direct memory writes via `SpanWriter`
- **Buffer-safe:** All operations validate buffer capacity

## Limitations

### Current Scope

Phase 3 supports:
- ✅ Repeating groups with fixed-size entries
- ✅ VarString8 (uint8 length prefix)
- ✅ Multiple groups per message
- ✅ Multiple varData fields per message

### Not Included (Future Enhancements)

Phase 3 does NOT include:
- ❌ Nested groups (groups within groups)
- ❌ VarString16/VarString32 (uint16/uint32 length prefix)
- ❌ VarData fields within groups
- ❌ Custom group dimension types beyond `GroupSizeEncoding`

These features may be added in future phases based on requirements.

## Breaking Changes

**None.** All changes are additive:
- Existing `TryEncode`, `Encode`, and `TryEncodeWithWriter` methods unchanged
- New methods only generated for messages with groups or varData
- No changes to parsing/decoding behavior

## Migration Guide

No migration required. New encoding methods are optional and only available for messages that have groups or varData fields.

### Before Phase 3

Messages with groups/varData could only be decoded:

```csharp
// Decoding only
OrderBookData.TryParse(buffer, out var message, out var variableData);
message.ConsumeVariableLengthSegments(variableData, bid => { }, ask => { });
```

### After Phase 3

Messages with groups/varData can now be both encoded and decoded:

```csharp
// Encoding (NEW!)
var message = new OrderBookData { InstrumentId = 42 };
message.BeginEncoding(buffer, out var writer);
OrderBookData.TryEncodeBids(ref writer, bids);
OrderBookData.TryEncodeAsks(ref writer, asks);

// Decoding (unchanged)
OrderBookData.TryParse(buffer, out var decoded, out var variableData);
decoded.ConsumeVariableLengthSegments(variableData, bid => { }, ask => { });
```

## Acceptance Criteria

All acceptance criteria from issue #62 met:

✅ **Grupos repetidos codificados corretamente**
- Groups encode with proper dimension headers
- Multiple groups per message supported
- Round-trip encoding/decoding validated

✅ **varData suportado na escrita**
- VarData fields encode with length prefix
- Data bytes written correctly
- Round-trip encoding/decoding validated

✅ **Testes avançados validados**
- 11 comprehensive integration tests
- All 214 tests passing (105 unit + 109 integration)
- Zero test failures

## Next Steps

Phase 3 is complete. Future enhancements could include:

1. **Nested Groups** - Groups within groups
2. **Extended VarData** - VarString16/VarString32 support
3. **Performance Benchmarks** - Measure encoding performance
4. **Documentation Examples** - Additional real-world examples

## References

- [SBE Specification](https://github.com/FIXTradingCommunity/fix-simple-binary-encoding) - Repeating groups and varData
- [Issue #62](https://github.com/pedrosakuma/SbeSourceGenerator/issues/62) - Phase 3 requirements
- [Phase 1 Implementation](./PHASE1_WRITING_IMPLEMENTATION.md) - Basic message encoding
- [SpanWriter Reference](../src/SbeCodeGenerator/Runtime/SpanWriter.cs) - Runtime support

## Conclusion

Phase 3 successfully delivers complete encoding support for SBE's complex features:

✅ **Feature Complete** - Groups and varData encoding implemented  
✅ **Well Tested** - 11 new tests, all passing  
✅ **Backward Compatible** - No breaking changes  
✅ **Production Ready** - Zero allocations, safe, performant  

The SbeSourceGenerator now supports **full round-trip encoding and decoding** for all common SBE message patterns, including repeating groups and variable-length data.
